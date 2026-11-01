namespace Core.Reporting;

/// <summary>Where the trace and screenshot of a failed test are saved.</summary>
public static class TestArtifacts
{
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "playwright-traces");

    public static string TracePath(string testName) => PathFor(testName, ".zip");

    public static string ScreenshotPath(string testName) => PathFor(testName, ".png");

    private static string PathFor(string testName, string extension)
    {
        Directory.CreateDirectory(Folder);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(testName.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return Path.Combine(Folder, safe + extension);
    }
}
