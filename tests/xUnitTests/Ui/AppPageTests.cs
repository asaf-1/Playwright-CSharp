using System.Text.RegularExpressions;

namespace xUnitTests.Ui;

[Trait("Category", "Smoke")]
[Trait("Category", "Ui")]
public class AppPageTests : xUnitBaseTest
{
    [Fact]
    public async Task App_page_loads_with_its_title()
    {
        await Page.GotoAsync("/app");

        await Expect(Page).ToHaveTitleAsync(new Regex("Playground Bank"));
    }
}
