using System.Text.Json;
using Core.Api.Models;
using Microsoft.Playwright;

namespace Core.Api;

/// <summary>The raw answer to a request: status, body text and headers.</summary>
public sealed record ApiResponse(int Status, string Body, IReadOnlyDictionary<string, string> Headers)
{
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;
}

/// <summary>
/// Talks to Playground Bank's REST and GraphQL APIs (/api/bank/*).
///
/// In a UI test, give it <c>Context.APIRequest</c>: it shares the browser's
/// cookies, so signing up here also signs the browser in. In an API-only test,
/// give it a context from <c>Playwright.APIRequest.NewContextAsync</c>.
///
/// The typed methods return the parsed reply and throw <see cref="BankApiException"/>
/// on an error answer. <see cref="SendRawAsync"/> returns any answer as it came,
/// for negative and contract tests.
/// </summary>
public sealed class BankApiClient(IAPIRequestContext request)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // --- Session --------------------------------------------------------------

    public Task<Session> RegisterAsync(RegisterRequest body) =>
        SendAsync<Session>("POST", "/api/bank/register", body);

    public Task<Session> LoginAsync(string email, string password) =>
        SendAsync<Session>("POST", "/api/bank/login", new LoginRequest(email, password));

    public Task LogoutAsync() =>
        SendAsync<JsonElement>("POST", "/api/bank/logout", new { });

    public Task<Session> GetMeAsync() =>
        SendAsync<Session>("GET", "/api/bank/me");

    // --- Accounts and money ---------------------------------------------------

    public Task<AccountList> GetAccountsAsync() =>
        SendAsync<AccountList>("GET", "/api/bank/accounts");

    public async Task<BankAccount> GetAccountAsync(string accountId) =>
        (await SendAsync<AccountReply>("GET", $"/api/bank/accounts/{accountId}")).Account;

    public async Task<BankAccount> OpenAccountAsync(OpenAccountRequest body) =>
        (await SendAsync<AccountReply>("POST", "/api/bank/accounts", body)).Account;

    public async Task<BankAccount> DepositAsync(string accountId, long amountCents) =>
        (await SendAsync<AccountReply>("POST", $"/api/bank/accounts/{accountId}/deposits", new DepositRequest(amountCents))).Account;

    public Task<TransactionPage> GetTransactionsAsync(string accountId) =>
        SendAsync<TransactionPage>("GET", $"/api/bank/accounts/{accountId}/transactions");

    public Task<TransferReply> TransferAsync(TransferRequest body, string? idempotencyKey = null) =>
        SendAsync<TransferReply>("POST", "/api/bank/transfers", body,
            idempotencyKey is null ? null : new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey });

    // --- Bill pay ---------------------------------------------------------------

    public async Task<IReadOnlyList<Payee>> GetPayeesAsync() =>
        (await SendAsync<PayeeList>("GET", "/api/bank/payees")).Payees;

    public async Task<Payee> AddPayeeAsync(string name, string reference) =>
        (await SendAsync<PayeeReply>("POST", "/api/bank/payees", new PayeeRequest(name, reference))).Payee;

    /// <summary>The bank wants a JSON body even on this DELETE, so an empty one is sent.</summary>
    public Task DeletePayeeAsync(string payeeId) =>
        SendAsync<JsonElement>("DELETE", $"/api/bank/payees/{payeeId}", new { });

    public Task<BillPaymentReply> PayBillAsync(BillPaymentRequest body) =>
        SendAsync<BillPaymentReply>("POST", "/api/bank/bill-payments", body);

    // --- Cards ------------------------------------------------------------------

    public async Task<IReadOnlyList<Card>> GetCardsAsync() =>
        (await SendAsync<CardList>("GET", "/api/bank/cards")).Cards;

    public async Task<Card> IssueCardAsync(string accountId) =>
        (await SendAsync<CardReply>("POST", "/api/bank/cards", new CardRequest(accountId))).Card;

    // --- Market -----------------------------------------------------------------

    public Task<Market> GetMarketAsync() =>
        SendAsync<Market>("GET", "/api/bank/market");

    // --- Staff (Support and Admin) ---------------------------------------------

    public async Task<IReadOnlyList<BankUser>> GetAllUsersAsync() =>
        (await SendAsync<UserList>("GET", "/api/bank/admin/users")).Users;

    public Task<JsonElement> ChangeUserAsync(string userId, string? role = null, string? status = null) =>
        SendAsync<JsonElement>("PATCH", $"/api/bank/admin/users/{userId}", new ChangeUserRequest(role, status));

    // --- GraphQL ----------------------------------------------------------------

    /// <summary>
    /// Sends a GraphQL query. GraphQL answers 200 even for errors, so the
    /// errors come back in the reply instead of as an exception.
    /// </summary>
    public async Task<GraphqlReply> GraphqlAsync(string query, object? variables = null)
    {
        var response = await SendRawAsync("POST", "/api/bank/graphql", new { query, variables });
        if (response.Status != 200)
        {
            throw new BankApiException("POST", "/api/bank/graphql", response.Status, TryReadError(response.Body));
        }
        return JsonSerializer.Deserialize<GraphqlReply>(response.Body, Json)!;
    }

    // --- Plumbing ---------------------------------------------------------------

    /// <summary>
    /// Sends a request and returns the answer as it came, whatever the status.
    /// The body is sent as JSON unless <paramref name="contentType"/> says otherwise;
    /// a string body is sent as it is.
    /// </summary>
    public async Task<ApiResponse> SendRawAsync(
        string method,
        string path,
        object? body = null,
        string contentType = "application/json",
        IDictionary<string, string>? headers = null)
    {
        var allHeaders = new Dictionary<string, string>(headers ?? new Dictionary<string, string>());
        var options = new APIRequestContextOptions { Method = method };

        if (body is not null)
        {
            options.Data = body as string ?? JsonSerializer.Serialize(body, Json);
            allHeaders["Content-Type"] = contentType;
        }

        options.Headers = allHeaders;

        var response = await request.FetchAsync(path, options);
        return new ApiResponse(response.Status, await response.TextAsync(), response.Headers);
    }

    private async Task<T> SendAsync<T>(string method, string path, object? body = null, IDictionary<string, string>? headers = null)
    {
        var response = await SendRawAsync(method, path, body, headers: headers);

        if (response.Status is < 200 or > 299)
        {
            throw new BankApiException(method, path, response.Status, TryReadError(response.Body));
        }

        return JsonSerializer.Deserialize<T>(response.Body, Json)
            ?? throw new InvalidOperationException($"{method} {path} answered {response.Status} with an empty body.");
    }

    private static ApiError? TryReadError(string body)
    {
        try { return JsonSerializer.Deserialize<ApiError>(body, Json); }
        catch (JsonException) { return null; }
    }
}
