using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>
/// Money requests between customers, /app/bank/requests. Ask another customer
/// for money by their account number; pay or decline requests asked of you.
/// </summary>
public sealed class RequestsPage(IPage page)
{
    public ILocator Root => page.GetByTestId("requests-page");

    // Asking for money
    public ILocator FromNumber => page.GetByTestId("request-from");
    public ILocator Amount => page.GetByTestId("request-amount");
    public ILocator ToAccount => page.GetByTestId("request-to");
    public ILocator Memo => page.GetByTestId("request-memo");
    public ILocator Submit => page.GetByTestId("request-submit");
    public ILocator Sent => page.GetByTestId("request-sent");
    public ILocator Error => page.GetByTestId("request-error");

    // The two lists
    public ILocator IncomingRows => page.Locator("[data-testid^='incoming-row-']");
    public ILocator OutgoingRows => page.Locator("[data-testid^='outgoing-row-']");
    public ILocator IncomingRowWith(string text) => IncomingRows.Filter(new() { HasText = text });
    public ILocator OutgoingRowWith(string text) => OutgoingRows.Filter(new() { HasText = text });
    public static ILocator IncomingStatus(ILocator row) => row.Locator("[data-testid^='incoming-status-']");
    public static ILocator OutgoingStatus(ILocator row) => row.Locator("[data-testid^='outgoing-status-']");

    // Paying a request
    public ILocator PayDialog => page.GetByTestId("request-pay-dialog");
    public ILocator PayFrom => page.GetByTestId("request-pay-from");
    public ILocator PayConfirm => page.GetByTestId("request-pay-confirm");

    public async Task GotoAsync() => await page.GotoAsync("/app/bank/requests");

    public async Task AskForMoneyAsync(string fromAccountNumber, string amount, string toAccountId, string memo)
    {
        await FromNumber.FillAsync(fromAccountNumber);
        await Amount.FillAsync(amount);
        await ToAccount.SelectOptionAsync(toAccountId);
        await Memo.FillAsync(memo);
        await Submit.ClickAsync();
    }

    /// <summary>Pays the incoming request that shows this memo, from the chosen account.</summary>
    public async Task PayAsync(string memo, string fromAccountId)
    {
        await IncomingRowWith(memo).Locator("[data-testid^='request-pay-']").ClickAsync();
        await PayFrom.SelectOptionAsync(fromAccountId);
        await PayConfirm.ClickAsync();
        await Assertions.Expect(PayDialog).ToBeHiddenAsync();
    }
}
