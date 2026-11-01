using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>
/// Sending money, /app/bank/transfer. Three steps: fill the form, review the
/// summary, confirm and get a receipt.
/// </summary>
public sealed class TransferPage(IPage page)
{
    public ILocator Root => page.GetByTestId("transfer-page");

    // Step 1: the form
    public ILocator From => page.GetByTestId("transfer-from");
    public ILocator ToOwnAccount => page.GetByTestId("transfer-to-own");
    public ILocator ToSomeoneElse => page.GetByTestId("transfer-to-other");
    public ILocator ToAccount => page.GetByTestId("transfer-to-account");
    public ILocator ToNumber => page.GetByTestId("transfer-to-number");
    public ILocator Amount => page.GetByTestId("transfer-amount");
    public ILocator Memo => page.GetByTestId("transfer-memo");
    public ILocator Review => page.GetByTestId("transfer-review");
    public ILocator AmountError => page.GetByTestId("transfer-amount-error");
    public ILocator Error => page.GetByTestId("transfer-error");

    // Step 2: the summary
    public ILocator Summary => page.GetByTestId("transfer-summary");
    public ILocator Confirm => page.GetByTestId("transfer-confirm");
    public ILocator Back => page.GetByTestId("transfer-back");

    // Step 3: the receipt
    public ILocator Receipt => page.GetByTestId("transfer-receipt");
    public ILocator ReceiptAmount => page.GetByTestId("receipt-amount");
    public ILocator ReceiptTo => page.GetByTestId("receipt-to");
    public ILocator ReceiptFromBalance => page.GetByTestId("receipt-from-balance");
    public ILocator Done => page.GetByTestId("transfer-done");

    public async Task GotoAsync() => await page.GotoAsync("/app/bank/transfer");

    /// <summary>Fills the form to move money between the customer's own accounts.</summary>
    public async Task FillToOwnAccountAsync(string fromAccountId, string toAccountId, string amount, string? memo = null)
    {
        await From.SelectOptionAsync(fromAccountId);
        await ToOwnAccount.ClickAsync();
        await ToAccount.SelectOptionAsync(toAccountId);
        await Amount.FillAsync(amount);
        if (memo is not null) await Memo.FillAsync(memo);
    }

    /// <summary>Fills the form to send money to someone else's account number.</summary>
    public async Task FillToSomeoneElseAsync(string fromAccountId, string toAccountNumber, string amount, string? memo = null)
    {
        await From.SelectOptionAsync(fromAccountId);
        await ToSomeoneElse.ClickAsync();
        await ToNumber.FillAsync(toAccountNumber);
        await Amount.FillAsync(amount);
        if (memo is not null) await Memo.FillAsync(memo);
    }
}
