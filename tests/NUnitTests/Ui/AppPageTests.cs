using System.Text.RegularExpressions;
using Core;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace NUnitTests.Ui;

[Category("Smoke")]
[Category("Ui")]
public class AppPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() =>
        new() { BaseURL = TestSettings.BaseUrl };

    [Test]
    public async Task App_page_loads_with_its_title()
    {
        await Page.GotoAsync("/app");

        await Expect(Page).ToHaveTitleAsync(new Regex("Playground Bank"));
    }
}
