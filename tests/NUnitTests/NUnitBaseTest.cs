using Core.Api;
using Core.Config;
using Core.Data;
using Core.Reporting;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace NUnitTests;

/// <summary>
/// Base for every NUnit browser test. Each test gets its own browser context
/// (from Playwright's PageTest), pointed at TestSettings.BaseUrl, and a trace
/// that is kept, with a screenshot, only when the test fails.
/// </summary>
public abstract class NUnitBaseTest : PageTest
{
    protected static TestSettings Settings => TestSettings.Current;

    /// <summary>The bank's API, sharing this test's browser cookies.</summary>
    protected BankApiClient BankApi => new(Context.APIRequest);

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = Settings.BaseUrl };

    /// <summary>Signs up a new customer. The browser is signed in as them too.</summary>
    protected Task<Customer> SignUpFreshCustomerAsync() => FreshCustomer.CreateAsync(BankApi);

    [SetUp]
    public async Task StartTracingAsync()
    {
        Context.SetDefaultTimeout(Settings.TimeoutMs);
        Assertions.SetDefaultExpectTimeout(Settings.ExpectTimeoutMs);

        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.FullName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task KeepEvidenceIfFailedAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var name = TestContext.CurrentContext.Test.FullName;
        var screenshot = TestArtifacts.ScreenshotPath(name);
        var trace = TestArtifacts.TracePath(name);

        await Page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
        await Context.Tracing.StopAsync(new() { Path = trace });

        TestContext.AddTestAttachment(screenshot, "Screenshot when the test failed");
        TestContext.AddTestAttachment(trace, "Playwright trace: open with 'playwright show-trace'");
    }
}
