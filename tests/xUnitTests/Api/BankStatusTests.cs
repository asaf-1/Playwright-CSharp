using Core;
using Microsoft.Playwright.Xunit.v3;

namespace xUnitTests.Api;

[Trait("Category", "Smoke")]
[Trait("Category", "Api")]
public class BankStatusTests : PlaywrightTest
{
    [Fact]
    public async Task Bank_status_answers_200()
    {
        var api = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = TestSettings.BaseUrl });

        var response = await api.GetAsync("/api/bank/status");

        Assert.Equal(200, response.Status);
        await api.DisposeAsync();
    }
}
