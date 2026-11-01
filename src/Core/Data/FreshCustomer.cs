using Core.Api;
using Core.Api.Models;
using Core.Config;
using Microsoft.Playwright;

namespace Core.Data;

/// <summary>A customer made for one test, with what the test needs to know about it.</summary>
public sealed record Customer(BankUser User, string Password, IReadOnlyList<BankAccount> Accounts)
{
    public string Email => User.Email;
    public BankAccount Checking => Accounts.First(account => account.Kind == "checking");
    public BankAccount Savings => Accounts.First(account => account.Kind == "savings");
}

/// <summary>
/// Signs up a brand-new customer through the API. Every test gets its own, so
/// tests never share money and can run in parallel. Emails start with the
/// TestUserPrefix (pwcs-...) so test users are easy to spot in the database.
/// New customers start with a Checking and a Savings account holding random
/// amounts, so tests read balances instead of expecting fixed numbers.
/// </summary>
public static class FreshCustomer
{
    public static async Task<Customer> CreateAsync(BankApiClient api)
    {
        var password = NewPassword();
        var session = await api.RegisterAsync(new RegisterRequest(
            FullName: $"Pwcs Tester {Guid.NewGuid().ToString("N")[..6]}",
            Email: NewEmail(),
            Password: password));

        var accounts = await api.GetAccountsAsync();
        return new Customer(session.User, password, accounts.Accounts);
    }

    /// <summary>
    /// Signs up a customer on a separate connection, so the test's browser stays
    /// signed out. For tests of the login page itself.
    /// </summary>
    public static async Task<Customer> CreateSignedOutAsync(IPlaywright playwright)
    {
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = TestSettings.Current.BaseUrl,
            Timeout = TestSettings.Current.TimeoutMs,
        });
        try
        {
            return await CreateAsync(new BankApiClient(request));
        }
        finally
        {
            await request.DisposeAsync();
        }
    }

    /// <summary>A unique test-user email: pwcs-&lt;12 hex&gt;@playgroundbank.test.</summary>
    public static string NewEmail() =>
        $"{TestSettings.Current.TestUserPrefix}-{Guid.NewGuid().ToString("N")[..12]}@{TestSettings.Current.TestUserDomain}";

    /// <summary>A password the bank accepts: 8+ characters with a letter and a number.</summary>
    public static string NewPassword() => $"Pwcs1-{Guid.NewGuid():N}"[..20];

    /// <summary>A unique, readable memo, so a test can find its own rows.</summary>
    public static string UniqueMemo(string what) => $"{what} {Guid.NewGuid().ToString("N")[..8]}";
}

/// <summary>A customer signed in in a browser of their own, for tests where two people interact.</summary>
public sealed record BrowserCustomer(Customer Customer, IPage Page, BankApiClient Api);
