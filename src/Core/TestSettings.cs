namespace Core;

/// <summary>
/// Where the tests point. Set the BASE_URL environment variable to test another
/// copy of Playground Bank, for example the live site.
/// </summary>
public static class TestSettings
{
    public const string LocalBaseUrl = "http://127.0.0.1:4173";

    public static string BaseUrl { get; } =
        (Environment.GetEnvironmentVariable("BASE_URL") ?? LocalBaseUrl).TrimEnd('/');
}
