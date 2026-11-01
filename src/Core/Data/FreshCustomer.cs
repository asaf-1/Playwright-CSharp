using Core.Api;
using Core.Api.Models;
using Core.Config;

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
        var settings = TestSettings.Current;
        var id = Guid.NewGuid().ToString("N")[..12];
        var password = $"Pwcs1-{Guid.NewGuid():N}"[..20];

        var session = await api.RegisterAsync(new RegisterRequest(
            FullName: $"Pwcs Tester {id[..6]}",
            Email: $"{settings.TestUserPrefix}-{id}@{settings.TestUserDomain}",
            Password: password));

        var accounts = await api.GetAccountsAsync();
        return new Customer(session.User, password, accounts.Accounts);
    }
}
