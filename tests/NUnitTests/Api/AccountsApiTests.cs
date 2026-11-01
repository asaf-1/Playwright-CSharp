using Core.Api;
using Core.Api.Models;

namespace NUnitTests.Api;

[Category("Api")]
public class AccountsApiTests : NUnitApiBaseTest
{
    [Test]
    public async Task New_customer_starts_with_checking_and_savings_and_a_matching_total()
    {
        await SignUpFreshCustomerAsync();

        var list = await BankApi.GetAccountsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(list.Accounts.Select(a => a.Kind), Is.EquivalentTo(new[] { "checking", "savings" }));
            Assert.That(list.Accounts, Has.All.Matches<BankAccount>(a => a is { BalanceCents: > 0 }));
            Assert.That(list.TotalCents, Is.EqualTo(list.Accounts.Sum(a => a.BalanceCents)));
            Assert.That(list.Accounts.Select(a => a.Number), Has.All.Match(@"^PB-\d{4}-\d{4}$"));
        });
    }

    [Test]
    public async Task Opening_an_account_adds_it_with_its_opening_amount()
    {
        await SignUpFreshCustomerAsync();

        var opened = await BankApi.OpenAccountAsync(new OpenAccountRequest("savings", "Pwcs Holiday", 12_345));
        var list = await BankApi.GetAccountsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(opened.Kind, Is.EqualTo("savings"));
            Assert.That(opened.Name, Is.EqualTo("Pwcs Holiday"));
            Assert.That(opened.BalanceCents, Is.EqualTo(12_345));
            Assert.That(list.Accounts.Select(a => a.Id), Does.Contain(opened.Id));
        });
    }

    [Test]
    public async Task Deposit_raises_the_balance_by_exactly_the_amount()
    {
        var customer = await SignUpFreshCustomerAsync();

        var after = await BankApi.DepositAsync(customer.Checking.Id, 50_000);
        var history = (await BankApi.GetTransactionsAsync(customer.Checking.Id)).Transactions;

        Assert.Multiple(() =>
        {
            Assert.That(after.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents + 50_000));
            Assert.That(history, Has.Some.Matches<Transaction>(t =>
                t is { Kind: "deposit", AmountCents: 50_000 } && t.BalanceAfterCents == after.BalanceCents));
        });
    }

    [Test]
    public async Task Another_customers_account_answers_404()
    {
        await SignUpFreshCustomerAsync();
        var otherClient = await NewClientAsync();
        var other = await Core.Data.FreshCustomer.CreateAsync(otherClient);

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetAccountAsync(other.Checking.Id));

        Assert.That((error.Status, error.Code), Is.EqualTo((404, "ACCOUNT_NOT_FOUND")));
    }
}
