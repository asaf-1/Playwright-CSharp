using Core.Data;
using Core.Pages;

namespace xUnitTests.Ui;

[Trait("Category", "Ui")]
public class OwnAccountsTests : xUnitBaseTest
{
    [Fact]
    public async Task Customer_opens_an_account_tops_it_up_and_moves_money_to_checking()
    {
        var customer = await SignUpFreshCustomerAsync();
        var accountName = $"Goals {Guid.NewGuid().ToString("N")[..6]}";
        var memo = FreshCustomer.UniqueMemo("Pwcs move");
        var bank = new BankPage(Page);
        var transfer = new TransferPage(Page);
        var account = new AccountPage(Page);

        // Open a new savings account with nothing in it
        await bank.GotoAsync();
        await bank.OpenAccountAsync("savings", accountName, "0.00");
        var goals = (await BankApi.GetAccountsAsync()).Accounts.Single(a => a.Name == accountName);
        await Expect(bank.AccountName(goals.Id)).ToHaveTextAsync(accountName);
        await Expect(bank.AccountBalance(goals.Id)).ToHaveTextAsync(Money.Format(0));

        // Top it up with $1,000.00
        await bank.AddFundsAsync(goals.Id, "1000.00");
        await Expect(bank.AccountBalance(goals.Id)).ToHaveTextAsync(Money.Format(100_000));

        // Move $250.00 of it to Checking
        await transfer.GotoAsync();
        await transfer.FillToOwnAccountAsync(goals.Id, customer.Checking.Id, "250.00", memo);
        await transfer.Review.ClickAsync();
        await Expect(transfer.Summary).ToBeVisibleAsync();
        await transfer.Confirm.ClickAsync();
        await Expect(transfer.ReceiptAmount).ToContainTextAsync(Money.Format(25_000));

        // Both balances moved on the bank page
        await bank.GotoAsync();
        await Expect(bank.AccountBalance(goals.Id)).ToHaveTextAsync(Money.Format(75_000));
        await Expect(bank.AccountBalance(customer.Checking.Id)).ToHaveTextAsync(Money.Format(customer.Checking.BalanceCents + 25_000));

        // Both histories show the transfer
        await account.GotoAsync(goals.Id);
        await Expect(account.HistoryRowWith(memo)).ToContainTextAsync(Money.Format(25_000));
        await account.GotoAsync(customer.Checking.Id);
        await Expect(account.HistoryRowWith(memo)).ToContainTextAsync(Money.Format(25_000));

        // And the API agrees with the pages
        var history = (await BankApi.GetTransactionsAsync(goals.Id)).Transactions;
        Assert.Contains(history, t => t.Kind == "deposit" && t.AmountCents == 100_000);
        Assert.Contains(history, t => t.Kind == "transfer_out" && t.AmountCents == -25_000);
    }
}
