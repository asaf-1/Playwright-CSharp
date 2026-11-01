using System.Text.RegularExpressions;

namespace NUnitTests.Ui;

[Category("Smoke")]
[Category("Ui")]
public class AppPageTests : NUnitBaseTest
{
    [Test]
    public async Task App_page_loads_with_its_title()
    {
        await Page.GotoAsync("/app");

        await Expect(Page).ToHaveTitleAsync(new Regex("Playground Bank"));
    }
}
