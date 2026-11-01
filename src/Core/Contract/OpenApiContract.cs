using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Api;
using Core.Config;
using Json.Schema;

namespace Core.Contract;

/// <summary>
/// The bank's API contract: its OpenAPI 3.1 document, read from the site under
/// test (/api/openapi.json), so the check always uses the spec that site ships.
///
/// <see cref="Check"/> compares one answer with the spec: the status must be
/// listed for that operation, and the body must match the JSON schema listed
/// for that status. OpenAPI 3.1 schemas are JSON Schema 2020-12, so each check
/// builds one self-contained schema: the shared definitions are copied in
/// under $defs, and "#/components/schemas/X" references become "#/$defs/X".
/// </summary>
public sealed class OpenApiContract
{
    private const string ComponentsPrefix = "#/components/schemas/";
    private static readonly ConcurrentDictionary<string, Task<OpenApiContract>> Loaded = new();
    private static readonly EvaluationOptions Evaluation = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private readonly JsonObject _paths;
    private readonly JsonObject _definitions;

    private OpenApiContract(JsonObject document)
    {
        _paths = document["paths"]!.AsObject();
        _definitions = RewriteReferences(document["components"]!["schemas"]!.DeepClone()).AsObject();
    }

    /// <summary>The spec of the site under test, read once per test run.</summary>
    public static Task<OpenApiContract> LoadAsync(BankApiClient api) =>
        Loaded.GetOrAdd(TestSettings.Current.BaseUrl, async _ =>
        {
            var response = await api.SendRawAsync("GET", "/api/openapi.json");
            if (response.Status != 200)
            {
                throw new InvalidOperationException($"GET /api/openapi.json answered {response.Status}.");
            }
            return new OpenApiContract(JsonNode.Parse(response.Body)!.AsObject());
        });

    /// <summary>What doesn't match the spec in this answer. Empty means it matches.</summary>
    public IReadOnlyList<string> Check(string method, string pathTemplate, ApiResponse response)
    {
        var operation = _paths[pathTemplate]?[method.ToLowerInvariant()];
        if (operation is null)
        {
            return [$"{method} {pathTemplate} is not in the spec."];
        }

        var documented = operation["responses"]?[response.Status.ToString()];
        if (documented is null)
        {
            return [$"{method} {pathTemplate} answered {response.Status}, a status the spec doesn't list."];
        }

        var schemaNode = documented["content"]?["application/json"]?["schema"];
        if (schemaNode is null)
        {
            return []; // Not a JSON answer (e.g. the CSV statement): nothing to compare.
        }

        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$defs"] = _definitions.DeepClone(),
        };
        foreach (var (keyword, value) in RewriteReferences(schemaNode.DeepClone()).AsObject().ToList())
        {
            schema[keyword] = value?.DeepClone();
        }

        var results = JsonSchema
            .FromText(schema.ToJsonString(), baseUri: new Uri($"https://pwcs.test/contract/{Guid.NewGuid():N}"))
            .Evaluate(JsonDocument.Parse(response.Body).RootElement, Evaluation);

        return results.IsValid ? [] : Describe(results);
    }

    private static List<string> Describe(EvaluationResults results)
    {
        var problems = new List<string>();
        foreach (var result in new[] { results }.Concat(results.Details ?? []))
        {
            foreach (var (keyword, message) in result.Errors ?? new Dictionary<string, string>())
            {
                problems.Add($"at '{result.InstanceLocation}' ({keyword}): {message}");
            }
        }
        return problems.Count > 0 ? problems : ["The body doesn't match the schema."];
    }

    private static JsonNode RewriteReferences(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$ref"] is JsonValue reference
                    && reference.TryGetValue<string>(out var target)
                    && target.StartsWith(ComponentsPrefix, StringComparison.Ordinal))
                {
                    obj["$ref"] = "#/$defs/" + target[ComponentsPrefix.Length..];
                }
                foreach (var (_, child) in obj.ToList())
                {
                    if (child is not null) RewriteReferences(child);
                }
                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is not null) RewriteReferences(child);
                }
                break;
        }
        return node;
    }
}
