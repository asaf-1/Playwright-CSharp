using Core.Api;
using Core.BugHunt;
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

    /// <summary>Signs up a new customer but leaves the browser signed out.</summary>
    protected Task<Customer> SignUpSignedOutCustomerAsync() => FreshCustomer.CreateSignedOutAsync(Playwright!);

    private readonly List<IAPIRequestContext> _extraClients = [];

    /// <summary>What a fault check needs: new API clients and new browsers, with practice mode on or off.</summary>
    protected FaultContext NewFaultContext(bool practice) => new(
        practice,
        newClient: async () =>
        {
            var context = await Playwright!.APIRequest.NewContextAsync(new() { BaseURL = Settings.BaseUrl, Timeout = Settings.TimeoutMs });
            _extraClients.Add(context);
            return new BankApiClient(context);
        },
        newBrowser: async () =>
        {
            var context = await NewContext(ContextOptions());
            context.SetDefaultTimeout(Settings.TimeoutMs);
            return (await context.NewPageAsync(), new BankApiClient(context.APIRequest));
        });

    /// <summary>Signs up another customer in a second browser of their own.</summary>
    protected async Task<BrowserCustomer> SignUpCustomerInSecondBrowserAsync()
    {
        var context = await NewContext(ContextOptions());
        context.SetDefaultTimeout(Settings.TimeoutMs);
        var page = await context.NewPageAsync();
        var api = new BankApiClient(context.APIRequest);
        return new BrowserCustomer(await FreshCustomer.CreateAsync(api), page, api);
    }

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

        foreach (var context in _extraClients) await context.DisposeAsync();
        _extraClients.Clear();

        await base.DisposeAsync();
    }
}
