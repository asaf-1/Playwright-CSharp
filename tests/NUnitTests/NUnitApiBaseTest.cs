using Core.Api;
using Core.Config;
using Core.Data;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace NUnitTests;

/// <summary>
/// Base for every NUnit API test: no browser, just an HTTP client pointed at
/// TestSettings.BaseUrl, with its own cookies, new for each test.
/// </summary>
public abstract class NUnitApiBaseTest : PlaywrightTest
{
    protected static TestSettings Settings => TestSettings.Current;

    protected IAPIRequestContext Request { get; private set; } = null!;

    protected BankApiClient BankApi { get; private set; } = null!;

    /// <summary>Signs up a new customer; this test's client is signed in as them.</summary>
    protected Task<Customer> SignUpFreshCustomerAsync() => FreshCustomer.CreateAsync(BankApi);

    [SetUp]
    public async Task CreateClientAsync()
    {
        Request = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Settings.BaseUrl,
            Timeout = Settings.TimeoutMs,
        });
        BankApi = new BankApiClient(Request);
    }

    [TearDown]
    public async Task DisposeClientAsync() => await Request.DisposeAsync();
}
