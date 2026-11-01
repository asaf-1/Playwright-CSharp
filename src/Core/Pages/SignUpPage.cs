using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>The sign-up page, /app/signup.</summary>
public sealed class SignUpPage(IPage page)
{
    public ILocator Root => page.GetByTestId("signup-page");
    public ILocator FullName => page.GetByTestId("signup-fullName");
    public ILocator Email => page.GetByTestId("signup-email");
    public ILocator Password => page.GetByTestId("signup-password");
    public ILocator ConfirmPassword => page.GetByTestId("signup-confirmPassword");
    public ILocator AcceptTerms => page.GetByTestId("signup-acceptTerms");
    public ILocator Submit => page.GetByTestId("signup-submit");
    public ILocator Error => page.GetByTestId("signup-error");

    public async Task GotoAsync() => await page.GotoAsync("/app/signup");

    public async Task SignUpAsync(string fullName, string email, string password)
    {
        await FullName.FillAsync(fullName);
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await ConfirmPassword.FillAsync(password);
        await AcceptTerms.CheckAsync();
        await Submit.ClickAsync();
    }
}
