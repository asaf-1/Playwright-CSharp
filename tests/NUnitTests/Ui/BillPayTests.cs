using Core.Data;
using Core.Pages;

namespace NUnitTests.Ui;

[Category("Ui")]
public class BillPayTests : NUnitBaseTest
{
    [Test]
    public async Task Customer_adds_a_payee_and_pays_a_bill()
    {
        var customer = await SignUpFreshCustomerAsync();
        var id = Guid.NewGuid().ToString("N")[..6];
        var payeeName = $"Pwcs Power {id}";
        var reference = $"ACC-{id}";
        var memo = FreshCustomer.UniqueMemo("Pwcs bill");
        var bills = new BillPayPage(Page);

        await bills.GotoAsync();
        await bills.AddPayeeAsync(payeeName, reference);
        await Expect(bills.PayeeRows.Filter(new() { HasText = payeeName })).ToBeVisibleAsync();

        await bills.FillPaymentAsync(customer.Checking.Id, payeeName, reference, "42.50", memo);
        await bills.Review.ClickAsync();
        await Expect(bills.SummaryPayee).ToContainTextAsync(payeeName);
        await Expect(bills.SummaryAmount).ToContainTextAsync(Money.Format(4_250));
        await bills.Confirm.ClickAsync();

        await Expect(bills.ReceiptAmount).ToContainTextAsync(Money.Format(4_250));
        await Expect(bills.ReceiptBalance).ToContainTextAsync(Money.Format(customer.Checking.BalanceCents - 4_250));
        await Expect(bills.PaymentRows.Filter(new() { HasText = payeeName })).ToBeVisibleAsync();

        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);
        Assert.That(checking.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents - 4_250));
    }
}
