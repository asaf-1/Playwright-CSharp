using Core.Api;
using Core.BugHunt;
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

    /// <summary>Signs up a new customer but leaves the browser signed out.</summary>
    protected Task<Customer> SignUpSignedOutCustomerAsync() => FreshCustomer.CreateSignedOutAsync(Playwright);

    private readonly List<IAPIRequestContext> _extraClients = [];

    /// <summary>What a fault check needs: new API clients and new browsers, with practice mode on or off.</summary>
    protected FaultContext NewFaultContext(bool practice) => new(
        practice,
        newClient: async () =>
        {
            var context = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = Settings.BaseUrl, Timeout = Settings.TimeoutMs });
            _extraClients.Add(context);
            return new BankApiClient(context);
        },
        newBrowser: async () =>
        {
            var context = await NewContext(ContextOptions());
            context.SetDefaultTimeout(Settings.TimeoutMs);
            return (await context.NewPageAsync(), new BankApiClient(context.APIRequest));
        });

    [TearDown]
    public async Task DisposeExtraClientsAsync()
    {
        foreach (var context in _extraClients) await context.DisposeAsync();
        _extraClients.Clear();
    }

    /// <summary>Signs up another customer in a second browser of their own.</summary>
    protected async Task<BrowserCustomer> SignUpCustomerInSecondBrowserAsync()
    {
        var context = await NewContext(ContextOptions());
        context.SetDefaultTimeout(Settings.TimeoutMs);
        var page = await context.NewPageAsync();
        var api = new BankApiClient(context.APIRequest);
        return new BrowserCustomer(await FreshCustomer.CreateAsync(api), page, api);
    }

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
