using Core.Api.Models;
using Core.Data;
using Core.Pages;

namespace NUnitTests.Ui;

[Category("Ui")]
public class StatementTests : NUnitBaseTest
{
    [Test]
    public async Task Statement_download_lists_the_transactions_and_a_matching_total()
    {
        var customer = await SignUpFreshCustomerAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs statement");
        await BankApi.TransferAsync(new TransferRequest(customer.Checking.Id, customer.Savings.Number, 1_234, memo));
        var account = new AccountPage(Page);

        await account.GotoAsync(customer.Checking.Id);
        await Expect(account.HistoryRowWith(memo)).ToBeVisibleAsync();
        var download = await Page.RunAndWaitForDownloadAsync(() => account.StatementDownload.ClickAsync());
        var statement = Statement.Parse(await File.ReadAllTextAsync((await download.PathAsync())!));

        Assert.Multiple(() =>
        {
            Assert.That(download.SuggestedFilename, Does.StartWith($"statement-{customer.Checking.Number}-").And.EndWith(".csv"));
            Assert.That(statement.Header, Is.EqualTo(Statement.ExpectedHeader));
            Assert.That(statement.Rows.Select(r => r.Type), Does.Contain("opening"));

            var transferRow = statement.Rows.Single(r => r.Memo == memo);
            Assert.That(transferRow.Type, Is.EqualTo("transfer_out"));
            Assert.That(transferRow.Amount, Is.EqualTo("-12.34"));

            Assert.That(Money.ParsePlain(statement.Total),
                Is.EqualTo(statement.Rows.Sum(r => Money.ParsePlain(r.Amount))),
                "the Total row must add up every row above it");
        });
    }
}
