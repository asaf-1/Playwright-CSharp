using Core.Api;
using Core.Api.Models;
using Core.Data;

namespace Core.Contract;

/// <summary>
/// What a contract case gets to work with: a fresh customer and a client signed
/// in as them, plus a client that is signed in as nobody.
/// </summary>
public sealed record ContractContext(Customer Customer, BankApiClient SignedIn, BankApiClient SignedOut);

/// <summary>
/// One call to compare with the spec. <see cref="Path"/> is the path exactly as
/// the spec writes it ("/api/bank/accounts/{id}"), so the right schema is found.
/// </summary>
public sealed record ContractCase(string Method, string Path, int ExpectedStatus, Func<ContractContext, Task<ApiResponse>> Call)
{
    /// <summary>A readable name without braces, which test runners treat specially.</summary>
    public override string ToString() =>
        $"{Method} {Path.Replace("{", ":").Replace("}", "")} answers {ExpectedStatus}";
}

/// <summary>
/// The calls the contract tests make: the main happy path of each area, and
/// the error answers a client is most likely to meet.
/// </summary>
public static class ContractCases
{
    public static IEnumerable<ContractCase> All()
    {
        // Session
        yield return new("GET", "/api/bank/status", 200, c => c.SignedOut.SendRawAsync("GET", "/api/bank/status"));
        yield return new("POST", "/api/bank/register", 201, c => c.SignedOut.SendRawAsync("POST", "/api/bank/register",
            new RegisterRequest("Pwcs Contract", FreshCustomer.NewEmail(), FreshCustomer.NewPassword())));
        yield return new("POST", "/api/bank/register", 400, c => c.SignedOut.SendRawAsync("POST", "/api/bank/register",
            new RegisterRequest("X", "not-an-email", "short", AcceptTerms: false)));
        yield return new("POST", "/api/bank/register", 409, c => c.SignedOut.SendRawAsync("POST", "/api/bank/register",
            new RegisterRequest("Pwcs Contract", c.Customer.Email, FreshCustomer.NewPassword())));
        yield return new("POST", "/api/bank/login", 401, c => c.SignedOut.SendRawAsync("POST", "/api/bank/login",
            new LoginRequest(c.Customer.Email, "Wrong-password-1")));
        yield return new("GET", "/api/bank/me", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/me"));
        yield return new("GET", "/api/bank/me", 401, c => c.SignedOut.SendRawAsync("GET", "/api/bank/me"));

        // Accounts and money
        yield return new("GET", "/api/bank/accounts", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/accounts"));
        yield return new("GET", "/api/bank/accounts", 401, c => c.SignedOut.SendRawAsync("GET", "/api/bank/accounts"));
        yield return new("POST", "/api/bank/accounts", 201, c => c.SignedIn.SendRawAsync("POST", "/api/bank/accounts",
            new OpenAccountRequest("savings", "Pwcs Contract", 1_000)));
        yield return new("GET", "/api/bank/accounts/{id}", 200, c =>
            c.SignedIn.SendRawAsync("GET", $"/api/bank/accounts/{c.Customer.Checking.Id}"));
        yield return new("GET", "/api/bank/accounts/{id}", 404, c =>
            c.SignedIn.SendRawAsync("GET", $"/api/bank/accounts/{Guid.NewGuid()}"));
        yield return new("POST", "/api/bank/accounts/{id}/deposits", 201, c =>
            c.SignedIn.SendRawAsync("POST", $"/api/bank/accounts/{c.Customer.Checking.Id}/deposits", new DepositRequest(500)));
        yield return new("POST", "/api/bank/accounts/{id}/deposits", 415, c =>
            c.SignedIn.SendRawAsync("POST", $"/api/bank/accounts/{c.Customer.Checking.Id}/deposits", "amountCents=500",
                contentType: "application/x-www-form-urlencoded"));
        yield return new("GET", "/api/bank/accounts/{id}/transactions", 200, c =>
            c.SignedIn.SendRawAsync("GET", $"/api/bank/accounts/{c.Customer.Checking.Id}/transactions"));
        yield return new("POST", "/api/bank/transfers", 201, c => c.SignedIn.SendRawAsync("POST", "/api/bank/transfers",
            new TransferRequest(c.Customer.Checking.Id, c.Customer.Savings.Number, 1_000, "Pwcs contract")));
        yield return new("POST", "/api/bank/transfers", 400, c => c.SignedIn.SendRawAsync("POST", "/api/bank/transfers",
            new TransferRequest(c.Customer.Checking.Id, c.Customer.Savings.Number, 0)));
        yield return new("POST", "/api/bank/transfers", 409, c => c.SignedIn.SendRawAsync("POST", "/api/bank/transfers",
            new TransferRequest(c.Customer.Checking.Id, c.Customer.Savings.Number, c.Customer.Checking.BalanceCents + 1)));

        // Bill pay
        yield return new("GET", "/api/bank/payees", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/payees"));
        yield return new("POST", "/api/bank/payees", 201, c => c.SignedIn.SendRawAsync("POST", "/api/bank/payees",
            new PayeeRequest("Pwcs Contract", "PWCS-1")));
        yield return new("POST", "/api/bank/bill-payments", 201, async c =>
        {
            var payee = await c.SignedIn.AddPayeeAsync("Pwcs Contract", "PWCS-2");
            return await c.SignedIn.SendRawAsync("POST", "/api/bank/bill-payments",
                new BillPaymentRequest(c.Customer.Checking.Id, payee.Id, 500, "Pwcs contract"));
        });

        // Cards
        yield return new("GET", "/api/bank/cards", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/cards"));
        yield return new("POST", "/api/bank/cards", 201, c => c.SignedIn.SendRawAsync("POST", "/api/bank/cards",
            new CardRequest(c.Customer.Checking.Id)));

        // Market and portfolio
        yield return new("GET", "/api/bank/market", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/market"));
        yield return new("GET", "/api/bank/market/{symbol}", 200, async c =>
        {
            var coin = (await c.SignedIn.GetMarketAsync()).Coins[0];
            return await c.SignedIn.SendRawAsync("GET", $"/api/bank/market/{coin.Symbol}");
        });
        yield return new("GET", "/api/bank/portfolio", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/portfolio"));

        // Between customers
        yield return new("GET", "/api/bank/notifications", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/notifications"));
        yield return new("GET", "/api/bank/requests", 200, c => c.SignedIn.SendRawAsync("GET", "/api/bank/requests"));

        // Staff area, as a customer
        yield return new("GET", "/api/bank/admin/users", 403, c => c.SignedIn.SendRawAsync("GET", "/api/bank/admin/users"));

        // GraphQL
        yield return new("POST", "/api/bank/graphql", 200, c => c.SignedIn.SendRawAsync("POST", "/api/bank/graphql",
            new { query = "{ accounts { id number kind balanceCents } }" }));
    }
}
