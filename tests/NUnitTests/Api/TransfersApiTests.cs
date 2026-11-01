using Core.Api;
using Core.Api.Models;
using Core.Data;

namespace NUnitTests.Api;

[Category("Api")]
public class TransfersApiTests : NUnitApiBaseTest
{
    [Test]
    public async Task Transfer_between_own_accounts_moves_the_money_both_ways()
    {
        var customer = await SignUpFreshCustomerAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs api move");

        var reply = await BankApi.TransferAsync(new TransferRequest(customer.Checking.Id, customer.Savings.Number, 7_500, memo));
        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);
        var savings = await BankApi.GetAccountAsync(customer.Savings.Id);
        var checkingHistory = (await BankApi.GetTransactionsAsync(customer.Checking.Id)).Transactions;
        var savingsHistory = (await BankApi.GetTransactionsAsync(customer.Savings.Id)).Transactions;

        Assert.Multiple(() =>
        {
            Assert.That(reply.Replayed, Is.False);
            Assert.That(reply.FromAccount.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents - 7_500));
            Assert.That(checking.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents - 7_500));
            Assert.That(savings.BalanceCents, Is.EqualTo(customer.Savings.BalanceCents + 7_500));
            Assert.That(checkingHistory, Has.Some.Matches<Transaction>(t =>
                t is { Kind: "transfer_out", AmountCents: -7_500 } && t.Memo == memo && t.BalanceAfterCents == checking.BalanceCents));
            Assert.That(savingsHistory, Has.Some.Matches<Transaction>(t =>
                t is { Kind: "transfer_in", AmountCents: 7_500 } && t.Memo == memo && t.BalanceAfterCents == savings.BalanceCents));
        });
    }

    [Test]
    public async Task Transfer_to_another_customer_reaches_their_account()
    {
        var sender = await SignUpFreshCustomerAsync();
        var receiverClient = await NewClientAsync();
        var receiver = await FreshCustomer.CreateAsync(receiverClient);

        await BankApi.TransferAsync(new TransferRequest(sender.Checking.Id, receiver.Checking.Number, 3_300));

        var received = await receiverClient.GetAccountAsync(receiver.Checking.Id);
        Assert.That(received.BalanceCents, Is.EqualTo(receiver.Checking.BalanceCents + 3_300));
    }

    [Test]
    public async Task Same_idempotency_key_twice_moves_the_money_once()
    {
        var customer = await SignUpFreshCustomerAsync();
        var key = Guid.NewGuid().ToString();
        var transfer = new TransferRequest(customer.Checking.Id, customer.Savings.Number, 2_000);

        var first = await BankApi.TransferAsync(transfer, key);
        var second = await BankApi.TransferAsync(transfer, key);
        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.Replayed, Is.False);
            Assert.That(second.Replayed, Is.True);
            Assert.That(second.Transfer.Id, Is.EqualTo(first.Transfer.Id));
            Assert.That(checking.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents - 2_000));
        });
    }

    [Test]
    public async Task Transfer_of_more_than_the_balance_is_refused_and_nothing_moves()
    {
        var customer = await SignUpFreshCustomerAsync();

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.TransferAsync(
            new TransferRequest(customer.Checking.Id, customer.Savings.Number, customer.Checking.BalanceCents + 1)));
        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);

        Assert.Multiple(() =>
        {
            Assert.That((error.Status, error.Code), Is.EqualTo((409, "INSUFFICIENT_FUNDS")));
            Assert.That(checking.BalanceCents, Is.EqualTo(customer.Checking.BalanceCents));
        });
    }

    [Test]
    public async Task Transfer_of_zero_is_refused_with_a_field_message()
    {
        var customer = await SignUpFreshCustomerAsync();

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.TransferAsync(
            new TransferRequest(customer.Checking.Id, customer.Savings.Number, 0)));

        Assert.Multiple(() =>
        {
            Assert.That((error.Status, error.Code), Is.EqualTo((400, "VALIDATION_FAILED")));
            Assert.That(error.Error?.Errors, Does.ContainKey("amountCents"));
        });
    }
}
