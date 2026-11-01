using Core.Api.Models;

namespace NUnitTests.Api;

[Category("Api")]
public class PayeesAndCardsApiTests : NUnitApiBaseTest
{
    [Test]
    public async Task Payee_can_be_added_listed_paid_and_deleted()
    {
        var customer = await SignUpFreshCustomerAsync();

        var payee = await BankApi.AddPayeeAsync("Pwcs Water", "WTR-001");
        var listed = await BankApi.GetPayeesAsync();
        var payment = await BankApi.PayBillAsync(new BillPaymentRequest(customer.Checking.Id, payee.Id, 1_999, "Pwcs water bill"));
        await BankApi.DeletePayeeAsync(payee.Id);
        var afterDelete = await BankApi.GetPayeesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(listed.Select(p => p.Id), Does.Contain(payee.Id));
            Assert.That(payment.Payment.PayeeName, Is.EqualTo("Pwcs Water"));
            Assert.That(payment.FromAccount.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents - 1_999));
            Assert.That(afterDelete.Select(p => p.Id), Does.Not.Contain(payee.Id));
        });
    }

    [Test]
    public async Task Card_is_issued_with_a_daily_limit_of_a_tenth_of_the_balance_in_whole_dollars()
    {
        var customer = await SignUpFreshCustomerAsync();

        // A tenth of the balance, rounded down to whole dollars (bank/cards.js defaultLimit).
        var expectedLimit = customer.Checking.BalanceCents / 10 / 100 * 100;

        var card = await BankApi.IssueCardAsync(customer.Checking.Id);
        var cards = await BankApi.GetCardsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(card.AccountId, Is.EqualTo(customer.Checking.Id));
            Assert.That(card.Status, Is.EqualTo("active"));
            Assert.That(card.Last4, Does.Match(@"^\d{4}$"));
            Assert.That(card.LimitCents, Is.EqualTo(expectedLimit));
            Assert.That(card.SpentTodayCents, Is.Zero);
            Assert.That(cards.Select(c => c.Id), Does.Contain(card.Id));
        });
    }
}
