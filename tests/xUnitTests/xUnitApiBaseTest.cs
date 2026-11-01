using Core.Api;
using Core.Config;
using Core.Data;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace xUnitTests;

/// <summary>
/// Base for every xUnit API test: no browser, just an HTTP client pointed at
/// TestSettings.BaseUrl, with its own cookies, new for each test.
/// </summary>
public abstract class xUnitApiBaseTest : PlaywrightTest
{
    protected static TestSettings Settings => TestSettings.Current;

    protected IAPIRequestContext Request { get; private set; } = null!;

    protected BankApiClient BankApi { get; private set; } = null!;

    /// <summary>Signs up a new customer; this test's client is signed in as them.</summary>
    protected Task<Customer> SignUpFreshCustomerAsync() => FreshCustomer.CreateAsync(BankApi);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Settings.BaseUrl,
            Timeout = Settings.TimeoutMs,
        });
        BankApi = new BankApiClient(Request);
    }

    public override async ValueTask DisposeAsync()
    {
        await Request.DisposeAsync();
        await base.DisposeAsync();
    }
}
