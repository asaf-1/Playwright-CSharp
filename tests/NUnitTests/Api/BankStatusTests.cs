using Core;
using Microsoft.Playwright.NUnit;

namespace NUnitTests.Api;

[Category("Smoke")]
[Category("Api")]
public class BankStatusTests : PlaywrightTest
{
    [Test]
    public async Task Bank_status_answers_200()
    {
        var api = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = TestSettings.BaseUrl });

        var response = await api.GetAsync("/api/bank/status");

        Assert.That(response.Status, Is.EqualTo(200));
        await api.DisposeAsync();
    }
}
