using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>
/// The bank's main page, /app/bank: every account with its balance, the total,
/// and recent activity. This is the customer's dashboard.
/// </summary>
public sealed class BankPage(IPage page)
{
    public ILocator Root => page.GetByTestId("bank-page");
    public ILocator Total => page.GetByTestId("bank-total");
    public ILocator TransferLink => page.GetByTestId("bank-transfer-link");
    public ILocator NoAccounts => page.GetByTestId("bank-no-accounts");
    public ILocator Activity => page.GetByTestId("bank-activity");
    public ILocator Error => page.GetByTestId("bank-error");

    public ILocator AccountCard(string accountId) => page.GetByTestId($"bank-account-card-{accountId}");
    public ILocator AccountName(string accountId) => page.GetByTestId($"account-name-{accountId}");
    public ILocator AccountNumber(string accountId) => page.GetByTestId($"account-number-{accountId}");
    public ILocator AccountBalance(string accountId) => page.GetByTestId($"account-balance-{accountId}");
    public ILocator AccountHistoryLink(string accountId) => page.GetByTestId($"account-view-{accountId}");
    public ILocator AccountTransferLink(string accountId) => page.GetByTestId($"account-transfer-{accountId}");

    public async Task GotoAsync() => await page.GotoAsync("/app/bank");
}
