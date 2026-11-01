namespace Core.Data;

/// <summary>
/// The bank's built-in demo accounts. They are read-only (they can't move
/// money), so tests only use them to check what a role may see or do.
/// </summary>
public static class DemoAccounts
{
    public const string Password = "demo1234";

    public const string Customer = "maya@playgroundbank.test";
    public const string Support = "sam@playgroundbank.test";
    public const string Admin = "alex@playgroundbank.test";

    /// <summary>A locked account: signing in is refused.</summary>
    public const string Locked = "lee@playgroundbank.test";
}
