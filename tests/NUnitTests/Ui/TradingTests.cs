using Core.Pages;

namespace NUnitTests.Ui;

[Category("Ui")]
public class TradingTests : NUnitBaseTest
{
    [Test]
    public async Task Customer_buys_a_coin_and_sees_it_in_the_portfolio()
    {
        var customer = await SignUpFreshCustomerAsync();
        var coin = (await BankApi.GetMarketAsync()).Coins.First();
        var coinPage = new CoinPage(Page);
        var portfolio = new PortfolioPage(Page);

        await coinPage.GotoAsync(coin.Symbol);
        await coinPage.BuyAsync(customer.Checking.Id, "250.00");
        await Expect(coinPage.Done).ToBeVisibleAsync();

        await portfolio.GotoAsync();
        await Expect(portfolio.Position(coin.Symbol)).ToBeVisibleAsync();
        await Expect(portfolio.TradeRows).ToHaveCountAsync(1);

        var checking = await BankApi.GetAccountAsync(customer.Checking.Id);
        Assert.That(checking.BalanceCents, Is.LessThanOrEqualTo(customer.Checking.BalanceCents - 25_000),
            "the coin and its fee were paid from Checking");
    }
}
