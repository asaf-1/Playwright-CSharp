using Core.Pages.Components;
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
    public ILocator OpenAccountButton => page.GetByTestId("bank-open-account");

    /// <summary>Shown instead of the accounts when nobody is signed in.</summary>
    public ILocator SignInPrompt => page.GetByTestId("signin-prompt");

    public OpenAccountDialog OpenAccountDialog { get; } = new(page);
    public AddFundsDialog AddFundsDialog { get; } = new(page);

    public ILocator AccountCard(string accountId) => page.GetByTestId($"bank-account-card-{accountId}");
    public ILocator AccountName(string accountId) => page.GetByTestId($"account-name-{accountId}");
    public ILocator AccountNumber(string accountId) => page.GetByTestId($"account-number-{accountId}");
    public ILocator AccountBalance(string accountId) => page.GetByTestId($"account-balance-{accountId}");
    public ILocator AccountHistoryLink(string accountId) => page.GetByTestId($"account-view-{accountId}");
    public ILocator AccountTransferLink(string accountId) => page.GetByTestId($"account-transfer-{accountId}");
    public ILocator AddFundsButton(string accountId) => page.GetByTestId($"account-add-funds-{accountId}");

    public async Task GotoAsync() => await page.GotoAsync("/app/bank");

    /// <summary>Opens a new account through the dialog. Kind is "checking" or "savings".</summary>
    public async Task OpenAccountAsync(string kind, string name, string openingAmount)
    {
        await OpenAccountButton.ClickAsync();
        await OpenAccountDialog.Kind.SelectOptionAsync(kind);
        await OpenAccountDialog.Name.FillAsync(name);
        await OpenAccountDialog.Amount.FillAsync(openingAmount);
        await OpenAccountDialog.Submit.ClickAsync();
        await Assertions.Expect(OpenAccountDialog.Root).ToBeHiddenAsync();
    }

    /// <summary>Adds practice money to one account through the dialog.</summary>
    public async Task AddFundsAsync(string accountId, string amount)
    {
        await AddFundsButton(accountId).ClickAsync();
        await AddFundsDialog.Amount.FillAsync(amount);
        await AddFundsDialog.Submit.ClickAsync();
        await Assertions.Expect(AddFundsDialog.Root).ToBeHiddenAsync();
    }
}
