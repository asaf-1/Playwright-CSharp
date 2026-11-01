using Microsoft.Playwright;

namespace Core.Pages.Components;

/// <summary>The account menu in the top bar, on every page.</summary>
public sealed class AccountMenu(IPage page)
{
    /// <summary>The button that opens the menu. Shows the first name, or "Guest".</summary>
    public ILocator Trigger => page.GetByTestId("account-menu-trigger");
    public ILocator Menu => page.GetByTestId("account-menu");
    public ILocator LogOut => page.GetByTestId("account-menu-logout");
    public ILocator LogIn => page.GetByTestId("account-menu-login");
    public ILocator SignUp => page.GetByTestId("account-menu-signup");

    public async Task LogOutAsync()
    {
        await Trigger.ClickAsync();
        await LogOut.ClickAsync();
    }
}
