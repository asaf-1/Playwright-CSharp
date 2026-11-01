using Microsoft.Extensions.Configuration;

namespace Core.Config;

/// <summary>
/// Everything the tests need to know about where and how to run.
/// Read once, in this order (later wins):
///   1. appsettings.json                 the local defaults
///   2. appsettings.{TEST_ENV}.json      e.g. TEST_ENV=Live for the live site
///   3. environment variables            e.g. TestSettings__TimeoutMs=30000
///   4. BASE_URL                         a shortcut for the address alone
/// PRACTICE_MODE=on turns Playground Bank's practice mode on for every test
/// customer, to see which tests catch the planted faults.
/// The browser itself is picked by Playwright's own variables:
/// BROWSER=chromium|firefox|webkit, and HEADED=1 to watch it.
/// </summary>
public sealed record TestSettings
{
    public string Environment { get; init; } = "Local";
    public string BaseUrl { get; init; } = "http://127.0.0.1:4173";
    public int TimeoutMs { get; init; } = 10_000;
    public int ExpectTimeoutMs { get; init; } = 5_000;
    public string TestUserPrefix { get; init; } = "pwcs";
    public string TestUserDomain { get; init; } = "playgroundbank.test";
    public bool PracticeMode { get; init; }

    public static TestSettings Current { get; } = Load();

    private static TestSettings Load()
    {
        var environment = System.Environment.GetEnvironmentVariable("TEST_ENV") ?? "Local";
        var environmentFile = $"appsettings.{environment}.json";

        if (environment != "Local" && !File.Exists(Path.Combine(AppContext.BaseDirectory, environmentFile)))
        {
            throw new InvalidOperationException(
                $"TEST_ENV is \"{environment}\", but there is no {environmentFile}.");
        }

        var settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(environmentFile, optional: true)
            .AddEnvironmentVariables()
            .Build()
            .GetSection("TestSettings")
            .Get<TestSettings>() ?? new TestSettings();

        var baseUrl = System.Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            settings = settings with { BaseUrl = baseUrl };
        }

        var practice = System.Environment.GetEnvironmentVariable("PRACTICE_MODE");
        if (practice is "on" or "1" or "true")
        {
            settings = settings with { PracticeMode = true };
        }

        return settings with { BaseUrl = settings.BaseUrl.TrimEnd('/') };
    }
}
