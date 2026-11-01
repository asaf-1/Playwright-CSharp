using Core.Data;
using Core.Pages;

namespace xUnitTests.Ui;

[Trait("Category", "Ui")]
public class BankPageTests : xUnitBaseTest
{
    [Fact]
    public async Task Fresh_customer_sees_their_accounts_and_total_without_typing_a_login()
    {
        var customer = await SignUpFreshCustomerAsync();
        var bank = new BankPage(Page);

        await bank.GotoAsync();

        foreach (var account in customer.Accounts)
        {
            await Expect(bank.AccountNumber(account.Id)).ToContainTextAsync(account.Number);
            await Expect(bank.AccountBalance(account.Id)).ToHaveTextAsync(Money.Format(account.BalanceCents));
        }
        await Expect(bank.Total).ToHaveTextAsync(Money.Format(customer.Accounts.Sum(account => account.BalanceCents)));
    }
}
