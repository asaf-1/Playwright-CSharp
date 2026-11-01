using System.Text.Json;
using Core.Api.Models;
using Microsoft.Playwright;

namespace Core.Api;

/// <summary>
/// Talks to Playground Bank's REST API (/api/bank/*).
///
/// In a UI test, give it <c>Context.APIRequest</c>: it shares the browser's
/// cookies, so signing up here also signs the browser in. In an API-only test,
/// give it a context from <c>Playwright.APIRequest.NewContextAsync</c>.
/// </summary>
public sealed class BankApiClient(IAPIRequestContext request)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<Session> RegisterAsync(RegisterRequest body) =>
        SendAsync<Session>("POST", "/api/bank/register", body);

    public Task<Session> LoginAsync(string email, string password) =>
        SendAsync<Session>("POST", "/api/bank/login", new LoginRequest(email, password));

    public Task LogoutAsync() =>
        SendAsync<JsonElement>("POST", "/api/bank/logout", new { });

    public Task<Session> GetMeAsync() =>
        SendAsync<Session>("GET", "/api/bank/me");

    public Task<AccountList> GetAccountsAsync() =>
        SendAsync<AccountList>("GET", "/api/bank/accounts");

    public async Task<BankAccount> GetAccountAsync(string accountId) =>
        (await SendAsync<AccountReply>("GET", $"/api/bank/accounts/{accountId}")).Account;

    public async Task<BankAccount> OpenAccountAsync(OpenAccountRequest body) =>
        (await SendAsync<AccountReply>("POST", "/api/bank/accounts", body)).Account;

    public async Task<BankAccount> DepositAsync(string accountId, long amountCents) =>
        (await SendAsync<AccountReply>("POST", $"/api/bank/accounts/{accountId}/deposits", new DepositRequest(amountCents))).Account;

    public Task<TransferReply> TransferAsync(TransferRequest body, string? idempotencyKey = null) =>
        SendAsync<TransferReply>("POST", "/api/bank/transfers", body,
            idempotencyKey is null ? null : new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey });

    private async Task<T> SendAsync<T>(string method, string path, object? body = null, Dictionary<string, string>? headers = null)
    {
        var allHeaders = new Dictionary<string, string>(headers ?? []);
        var options = new APIRequestContextOptions { Method = method };

        if (body is not null)
        {
            options.Data = JsonSerializer.Serialize(body, Json);
            allHeaders["Content-Type"] = "application/json";
        }

        options.Headers = allHeaders;

        var response = await request.FetchAsync(path, options);
        var text = await response.TextAsync();

        if (!response.Ok)
        {
            ApiError? error = null;
            try { error = JsonSerializer.Deserialize<ApiError>(text, Json); }
            catch (JsonException) { /* not JSON: keep the status alone */ }
            throw new BankApiException(method, path, response.Status, error);
        }

        return JsonSerializer.Deserialize<T>(text, Json)
            ?? throw new InvalidOperationException($"{method} {path} answered {response.Status} with an empty body.");
    }
}
