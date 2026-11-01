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
    private readonly List<IAPIRequestContext> _contexts = [];

    protected static TestSettings Settings => TestSettings.Current;

    protected IAPIRequestContext Request { get; private set; } = null!;

    protected BankApiClient BankApi { get; private set; } = null!;

    /// <summary>Signs up a new customer; this test's client is signed in as them.</summary>
    protected Task<Customer> SignUpFreshCustomerAsync() => FreshCustomer.CreateAsync(BankApi);

    /// <summary>Another client with its own cookies, signed in as nobody yet.</summary>
    protected async Task<BankApiClient> NewClientAsync()
    {
        var context = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Settings.BaseUrl,
            Timeout = Settings.TimeoutMs,
        });
        _contexts.Add(context);
        return new BankApiClient(context);
    }

    [SetUp]
    public async Task CreateClientAsync()
    {
        BankApi = await NewClientAsync();
        Request = _contexts[0];
    }

    [TearDown]
    public async Task DisposeClientsAsync()
    {
        foreach (var context in _contexts) await context.DisposeAsync();
        _contexts.Clear();
    }
}
