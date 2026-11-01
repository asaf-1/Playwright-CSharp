using Core.Api;
using Core.Data;
using Microsoft.Playwright;

namespace Core.BugHunt;

/// <summary>What a fault check saw: whether the fault showed, and the evidence.</summary>
public sealed record Finding(bool Seen, string Evidence)
{
    public static Finding Fault(string evidence) => new(true, evidence);
    public static Finding Clean(string evidence) => new(false, evidence);
}

/// <summary>A customer with their own API client, for a fault check.</summary>
public sealed record Party(Customer Customer, BankApiClient Api)
{
    /// <summary>"?runKey=..." when practice mode is on, for clients without the cookie.</summary>
    public string RunKeyQuery => Customer.RunKey is null ? "" : $"?runKey={Uri.EscapeDataString(Customer.RunKey)}";
}

/// <summary>A customer signed in in a browser of their own, for a fault check that needs the pages.</summary>
public sealed record BrowserParty(Customer Customer, BankApiClient Api, IPage Page);

/// <summary>
/// What a fault check gets to work with. Every customer it makes has practice
/// mode on or off, as the run asks, so the same check runs both ways.
/// </summary>
public sealed class FaultContext(
    bool practice,
    Func<Task<BankApiClient>> newClient,
    Func<Task<(IPage Page, BankApiClient Api)>>? newBrowser = null)
{
    public bool Practice => practice;

    /// <summary>A client with its own cookies, signed in as nobody.</summary>
    public Task<BankApiClient> NewClientAsync() => newClient();

    /// <summary>A new customer on a new client.</summary>
    public async Task<Party> NewCustomerAsync()
    {
        var api = await newClient();
        return new Party(await FreshCustomer.CreateAsync(api, practice), api);
    }

    /// <summary>Another client signed in as an existing person (e.g. a second device, or staff).</summary>
    public async Task<BankApiClient> SignInAsync(string email, string password)
    {
        var api = await newClient();
        await api.LoginAsync(email, password);
        if (practice) await PracticeMode.TurnOnAsync(api);
        return api;
    }

    /// <summary>A new customer in a browser of their own.</summary>
    public async Task<BrowserParty> NewBrowserCustomerAsync()
    {
        if (newBrowser is null)
        {
            throw new InvalidOperationException("This check needs a browser, and the test gave none.");
        }
        var (page, api) = await newBrowser();
        return new BrowserParty(await FreshCustomer.CreateAsync(api, practice), api, page);
    }
}

/// <summary>
/// One planted fault and a check that looks for it. The check answers the same
/// question both ways: with practice mode off it must find nothing (the rule
/// holds), and with practice mode on it must find the fault (the test would
/// catch it).
/// </summary>
public sealed record FaultCheck(string Flag, string Rule, Func<FaultContext, Task<Finding>> Detect, bool NeedsBrowser = false)
{
    public override string ToString() => Flag;
}

/// <summary>Thrown when a check can't set up what it needs; the check proves nothing then.</summary>
public sealed class CheckSetupException(string message) : Exception(message);
