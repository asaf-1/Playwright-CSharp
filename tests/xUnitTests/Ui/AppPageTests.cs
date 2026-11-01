using System.Text.RegularExpressions;
using Core;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace xUnitTests.Ui;

[Trait("Category", "Smoke")]
[Trait("Category", "Ui")]
public class AppPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() =>
        new() { BaseURL = TestSettings.BaseUrl };

    [Fact]
    public async Task App_page_loads_with_its_title()
    {
        await Page.GotoAsync("/app");

        await Expect(Page).ToHaveTitleAsync(new Regex("Playground Bank"));
    }
}
