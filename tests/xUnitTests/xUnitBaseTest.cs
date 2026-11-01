using Core.Api;
using Core.Config;
using Core.Data;
using Core.Reporting;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace xUnitTests;

/// <summary>
/// Base for every xUnit browser test. Each test gets its own browser context
/// (from Playwright's PageTest), pointed at TestSettings.BaseUrl, and a trace
/// that is kept, with a screenshot, only when the test fails.
/// xUnit has no [SetUp]/[TearDown]: InitializeAsync and DisposeAsync do that job.
/// </summary>
public abstract class xUnitBaseTest : PageTest
{
    protected static TestSettings Settings => TestSettings.Current;

    /// <summary>The bank's API, sharing this test's browser cookies.</summary>
    protected BankApiClient BankApi => new(Context.APIRequest);

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = Settings.BaseUrl };

    /// <summary>Signs up a new customer. The browser is signed in as them too.</summary>
    protected Task<Customer> SignUpFreshCustomerAsync() => FreshCustomer.CreateAsync(BankApi);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        Context.SetDefaultTimeout(Settings.TimeoutMs);
        Assertions.SetDefaultExpectTimeout(Settings.ExpectTimeoutMs);

        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.Current.Test?.TestDisplayName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    public override async ValueTask DisposeAsync()
    {
        if (TestContext.Current.TestState?.Result == TestResult.Failed)
        {
            var name = TestContext.Current.Test?.TestDisplayName ?? GetType().Name;
            var screenshot = TestArtifacts.ScreenshotPath(name);
            var trace = TestArtifacts.TracePath(name);

            await Page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
            await Context.Tracing.StopAsync(new() { Path = trace });

            TestContext.Current.AddAttachment("Screenshot when the test failed", screenshot);
            TestContext.Current.AddAttachment("Playwright trace", trace);
        }
        else
        {
            await Context.Tracing.StopAsync();
        }

        await base.DisposeAsync();
    }
}
