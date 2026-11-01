using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>One account, /app/bank/accounts/{id}: its balance, history and statement.</summary>
public sealed class AccountPage(IPage page)
{
    public ILocator Root => page.GetByTestId("account-page");
    public ILocator Number => page.GetByTestId("account-number");
    public ILocator Balance => page.GetByTestId("account-balance");
    public ILocator SendMoney => page.GetByTestId("account-send");
    public ILocator NotFound => page.GetByTestId("account-not-found");

    /// <summary>A download link for the history as a CSV file.</summary>
    public ILocator StatementDownload => page.GetByTestId("statement-download");

    public ILocator HistoryTable => page.GetByTestId("history-table");
    public ILocator HistoryRows => page.Locator("[data-testid^='history-row-']");
    public ILocator HistoryEmpty => page.GetByTestId("history-empty");
    public ILocator HistoryCount => page.GetByTestId("history-count");

    /// <summary>The history row that shows this text, e.g. a test's unique memo.</summary>
    public ILocator HistoryRowWith(string text) => HistoryRows.Filter(new() { HasText = text });

    public async Task GotoAsync(string accountId) => await page.GotoAsync($"/app/bank/accounts/{accountId}");
}
