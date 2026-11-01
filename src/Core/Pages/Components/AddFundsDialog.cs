using Microsoft.Playwright;

namespace Core.Pages.Components;

/// <summary>The "Add funds" dialog on the bank page: practice money for one account.</summary>
public sealed class AddFundsDialog(IPage page)
{
    public ILocator Root => page.GetByTestId("add-funds-dialog");
    public ILocator Amount => page.GetByTestId("add-funds-amount");
    public ILocator Submit => page.GetByTestId("add-funds-submit");
    public ILocator Cancel => page.GetByTestId("add-funds-cancel");
    public ILocator Error => page.GetByTestId("add-funds-error");
}
