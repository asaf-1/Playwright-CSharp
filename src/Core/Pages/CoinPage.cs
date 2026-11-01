using Microsoft.Playwright;

namespace Core.Pages;

/// <summary>
/// One coin, /app/markets/{symbol}, with its trade panel. Buying takes two
/// steps: get a price (it holds for 15 seconds), then confirm.
/// </summary>
public sealed class CoinPage(IPage page)
{
    public ILocator TradePanel => page.GetByTestId("trade-panel");
    public ILocator Buy => page.GetByTestId("trade-side-buy");
    public ILocator Sell => page.GetByTestId("trade-side-sell");
    public ILocator Amount => page.GetByTestId("trade-amount");
    public ILocator Account => page.GetByTestId("trade-account");
    public ILocator GetPrice => page.GetByTestId("trade-quote");
    public ILocator Quote => page.GetByTestId("trade-quote-box");
    public ILocator QuoteTotal => page.GetByTestId("trade-quote-total");
    public ILocator Confirm => page.GetByTestId("trade-confirm");
    public ILocator Done => page.GetByTestId("trade-done");
    public ILocator Holding => page.GetByTestId("trade-holding");
    public ILocator Error => page.GetByTestId("trade-error");

    public async Task GotoAsync(string symbol) => await page.GotoAsync($"/app/markets/{symbol}");

    /// <summary>Buys the coin for this many dollars, paid from the chosen account.</summary>
    public async Task BuyAsync(string accountId, string dollars)
    {
        await Buy.ClickAsync();
        await Amount.FillAsync(dollars);
        await Account.SelectOptionAsync(accountId);
        await GetPrice.ClickAsync();
        await Assertions.Expect(Quote).ToBeVisibleAsync();
        await Confirm.ClickAsync();
    }
}
