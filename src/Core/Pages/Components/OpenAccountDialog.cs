using Microsoft.Playwright;

namespace Core.Pages.Components;

/// <summary>The "Open an account" dialog on the bank page.</summary>
public sealed class OpenAccountDialog(IPage page)
{
    public ILocator Root => page.GetByTestId("open-account-dialog");
    public ILocator Kind => page.GetByTestId("open-account-kind");
    public ILocator Name => page.GetByTestId("open-account-name");
    public ILocator Amount => page.GetByTestId("open-account-amount");
    public ILocator Submit => page.GetByTestId("open-account-submit");
    public ILocator Cancel => page.GetByTestId("open-account-cancel");
    public ILocator Error => page.GetByTestId("open-account-error");
}
