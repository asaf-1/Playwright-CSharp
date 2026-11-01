using Core.Api.Models;

namespace xUnitTests.Api;

[Trait("Category", "Api")]
public class AccountsApiTests : xUnitApiBaseTest
{
    [Fact]
    public async Task Opening_an_account_adds_it_with_its_opening_amount()
    {
        await SignUpFreshCustomerAsync();

        var opened = await BankApi.OpenAccountAsync(new OpenAccountRequest("savings", "Pwcs Holiday", 12_345));
        var list = await BankApi.GetAccountsAsync();

        Assert.Equal("savings", opened.Kind);
        Assert.Equal("Pwcs Holiday", opened.Name);
        Assert.Equal(12_345, opened.BalanceCents);
        Assert.Contains(list.Accounts, a => a.Id == opened.Id);
    }

    [Fact]
    public async Task Deposit_raises_the_balance_by_exactly_the_amount()
    {
        var customer = await SignUpFreshCustomerAsync();

        var after = await BankApi.DepositAsync(customer.Checking.Id, 50_000);
        var history = (await BankApi.GetTransactionsAsync(customer.Checking.Id)).Transactions;

        Assert.Equal(customer.Checking.BalanceCents + 50_000, after.BalanceCents);
        Assert.Contains(history, t => t.Kind == "deposit" && t.AmountCents == 50_000 && t.BalanceAfterCents == after.BalanceCents);
    }
}
