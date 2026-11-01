using Microsoft.Playwright;

namespace Core.Pages.Components;

/// <summary>The notification bell in the top bar, on every page.</summary>
public sealed class NotificationBell(IPage page)
{
    public ILocator Bell => page.GetByTestId("notif-bell");

    /// <summary>The unread count. Hidden when there is nothing unread.</summary>
    public ILocator Count => page.GetByTestId("notif-count");
    public ILocator Menu => page.GetByTestId("notif-menu");
    public ILocator Items => page.Locator("[data-testid^='notif-item-']");
    public ILocator MarkAllRead => page.GetByTestId("notif-mark-all");
}
