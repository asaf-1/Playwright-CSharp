using Core.Api.Models;
using Core.Data;

namespace xUnitTests.Api;

[Trait("Category", "Api")]
public class TransfersApiTests : xUnitApiBaseTest
{
    [Fact]
    public async Task Transfer_between_own_accounts_moves_the_money_both_ways()
    {
        var customer = await SignUpFreshCustomerAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs api move");

        var reply = await BankApi.TransferAsync(new TransferRequest(customer.Checking.Id, customer.Savings.Number, 7_500, memo));
        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);
        var savings = await BankApi.GetAccountAsync(customer.Savings.Id);
        var checkingHistory = (await BankApi.GetTransactionsAsync(customer.Checking.Id)).Transactions;
        var savingsHistory = (await BankApi.GetTransactionsAsync(customer.Savings.Id)).Transactions;

        Assert.False(reply.Replayed);
        Assert.Equal(customer.Checking.BalanceCents - 7_500, checking.BalanceCents);
        Assert.Equal(customer.Savings.BalanceCents + 7_500, savings.BalanceCents);
        Assert.Contains(checkingHistory, t =>
            t.Kind == "transfer_out" && t.AmountCents == -7_500 && t.Memo == memo && t.BalanceAfterCents == checking.BalanceCents);
        Assert.Contains(savingsHistory, t =>
            t.Kind == "transfer_in" && t.AmountCents == 7_500 && t.Memo == memo && t.BalanceAfterCents == savings.BalanceCents);
    }
}
