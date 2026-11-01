using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>
/// Paying bills, /app/bank/bills: save payees, then pay one in three steps
/// (form, review, receipt). Recent payments are listed below.
/// </summary>
public sealed class BillPayPage(IPage page)
{
    public ILocator Root => page.GetByTestId("bills-page");

    // Payees
    public ILocator PayeeName => page.GetByTestId("payee-name");
    public ILocator PayeeReference => page.GetByTestId("payee-reference");
    public ILocator AddPayee => page.GetByTestId("payee-add");
    public ILocator PayeeRows => page.Locator("[data-testid^='payee-row-']");
    public ILocator PayeeError => page.GetByTestId("payee-error");

    // Paying: the form
    public ILocator From => page.GetByTestId("pay-from");
    public ILocator Payee => page.GetByTestId("pay-payee");
    public ILocator Amount => page.GetByTestId("pay-amount");
    public ILocator Memo => page.GetByTestId("pay-memo");
    public ILocator Review => page.GetByTestId("pay-review");
    public ILocator Error => page.GetByTestId("pay-error");

    // Paying: the summary and the receipt
    public ILocator Summary => page.GetByTestId("pay-summary");
    public ILocator SummaryPayee => page.GetByTestId("pay-summary-payee");
    public ILocator SummaryAmount => page.GetByTestId("pay-summary-amount");
    public ILocator Confirm => page.GetByTestId("pay-confirm");
    public ILocator Receipt => page.GetByTestId("pay-receipt");
    public ILocator ReceiptAmount => page.GetByTestId("pay-receipt-amount");
    public ILocator ReceiptBalance => page.GetByTestId("pay-receipt-balance");

    // Recent payments
    public ILocator PaymentsTable => page.GetByTestId("payments-table");
    public ILocator PaymentRows => page.Locator("[data-testid^='payment-row-']");

    public async Task GotoAsync() => await page.GotoAsync("/app/bank/bills");

    public async Task AddPayeeAsync(string name, string reference)
    {
        await PayeeName.FillAsync(name);
        await PayeeReference.FillAsync(reference);
        await AddPayee.ClickAsync();
    }

    /// <summary>Fills the payment form. The payee list shows "name · reference".</summary>
    public async Task FillPaymentAsync(string fromAccountId, string payeeName, string payeeReference, string amount, string memo)
    {
        await From.SelectOptionAsync(fromAccountId);
        await Payee.SelectOptionAsync(new SelectOptionValue { Label = $"{payeeName} · {payeeReference}" });
        await Amount.FillAsync(amount);
        await Memo.FillAsync(memo);
    }
}
