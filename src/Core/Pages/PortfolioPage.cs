using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>What the customer holds, /app/portfolio: positions, totals and filled trades.</summary>
public sealed class PortfolioPage(IPage page)
{
    public ILocator Root => page.GetByTestId("portfolio-page");
    public ILocator Empty => page.GetByTestId("portfolio-empty");
    public ILocator Value => page.GetByTestId("portfolio-value");
    public ILocator TradeRows => page.Locator("[data-testid^='trade-row-']");

    public ILocator Position(string symbol) => page.GetByTestId($"position-{symbol}");
    public ILocator PositionValue(string symbol) => page.GetByTestId($"position-value-{symbol}");

    public async Task GotoAsync() => await page.GotoAsync("/app/portfolio");
}
