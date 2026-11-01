using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>The customer's profile, /app/profile. Sign-up and log-in land here.</summary>
public sealed class ProfilePage(IPage page)
{
    public ILocator Root => page.GetByTestId("profile-page");

    /// <summary>The welcome note shown right after signing up.</summary>
    public ILocator Welcome => page.GetByTestId("profile-welcome");
    public ILocator Name => page.GetByTestId("profile-name");
    public ILocator Email => page.GetByTestId("profile-email");
    public ILocator Role => page.GetByTestId("profile-role");

    public async Task GotoAsync() => await page.GotoAsync("/app/profile");
}
