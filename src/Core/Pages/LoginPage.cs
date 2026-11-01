using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>The sign-in page, /app/login.</summary>
public sealed class LoginPage(IPage page)
{
    public ILocator Root => page.GetByTestId("login-page");
    public ILocator Email => page.GetByTestId("login-email");
    public ILocator Password => page.GetByTestId("login-password");
    public ILocator Submit => page.GetByTestId("login-submit");
    public ILocator Error => page.GetByTestId("login-error");
    public ILocator EmailError => page.GetByTestId("login-email-error");
    public ILocator PasswordError => page.GetByTestId("login-password-error");
    public ILocator SignUpLink => page.GetByTestId("login-signup-link");

    public async Task GotoAsync() => await page.GotoAsync("/app/login");

    public async Task SignInAsync(string email, string password)
    {
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await Submit.ClickAsync();
    }
}
