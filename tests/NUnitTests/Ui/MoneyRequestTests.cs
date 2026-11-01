using Core.Data;
using Core.Pages;
using Core.Pages.Components;

namespace NUnitTests.Ui;

[Category("Ui")]
public class MoneyRequestTests : NUnitBaseTest
{
    [Test]
    public async Task Customer_asks_another_customer_for_money_and_gets_paid()
    {
        var asker = await SignUpFreshCustomerAsync();
        var payer = await SignUpCustomerInSecondBrowserAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs dinner");
        var askerRequests = new RequestsPage(Page);
        var payerRequests = new RequestsPage(payer.Page);
        var payerBell = new NotificationBell(payer.Page);

        // The asker asks for $12.34 from the payer's checking account
        await askerRequests.GotoAsync();
        await askerRequests.AskForMoneyAsync(payer.Customer.Checking.Number, "12.34", asker.Checking.Id, memo);
        await Expect(askerRequests.Sent).ToBeVisibleAsync();
        await Expect(RequestsPage.OutgoingStatus(askerRequests.OutgoingRowWith(memo))).ToHaveTextAsync("Waiting");

        // The payer sees it on the bell and in their list, and pays it
        await payerRequests.GotoAsync();
        await Expect(payerBell.Count).ToHaveTextAsync("1");
        await payerRequests.PayAsync(memo, payer.Customer.Checking.Id);
        await Expect(RequestsPage.IncomingStatus(payerRequests.IncomingRowWith(memo))).ToHaveTextAsync("Paid");

        // The asker sees it paid
        await askerRequests.GotoAsync();
        await Expect(RequestsPage.OutgoingStatus(askerRequests.OutgoingRowWith(memo))).ToHaveTextAsync("Paid");

        // The money moved
        var askerChecking = await BankApi.GetAccountAsync(asker.Checking.Id);
        var payerChecking = await payer.Api.GetAccountAsync(payer.Customer.Checking.Id);
        Assert.Multiple(() =>
        {
            Assert.That(askerChecking.BalanceCents, Is.EqualTo(asker.Checking.BalanceCents + 1_234));
            Assert.That(payerChecking.BalanceCents, Is.EqualTo(payer.Customer.Checking.BalanceCents - 1_234));
        });
    }
}
