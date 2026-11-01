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

    /// <summary>
    /// Signs in and waits for the bank's answer. Needed when the same error text
    /// can show twice in a row, so the test knows a new answer has arrived.
    /// </summary>
    public Task<IResponse> SignInAndWaitAsync(string email, string password) =>
        page.RunAndWaitForResponseAsync(
            () => SignInAsync(email, password),
            response => response.Url.EndsWith("/api/bank/login"));
}
