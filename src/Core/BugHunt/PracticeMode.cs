using Core.Api;

namespace Core.BugHunt;

/// <summary>
/// Playground Bank's practice mode: one switch that arms every planted fault
/// for one visitor only. The bank gives that visitor a run key in a session
/// cookie (qa_runkey), so the faults follow that client and nobody else.
///
/// Safety rule: this is the only way the tests arm faults. The bank also has
/// /api/test/flags and /api/test/reset, but a write there without a run key
/// changes the site for everyone, and the reset clears the whole server.
/// Those endpoints are never called by this project.
/// </summary>
public static class PracticeMode
{
    /// <summary>
    /// Turns practice mode on for this client's cookies and returns the run key.
    /// A client that can't carry the cookie (e.g. one using an API key) passes
    /// the run key as ?runKey= instead.
    /// </summary>
    public static async Task<string> TurnOnAsync(BankApiClient api)
    {
        var response = await api.SendRawAsync("POST", "/api/practice", new { on = true });
        if (response.Status != 200)
        {
            throw new InvalidOperationException($"Turning practice mode on answered {response.Status}: {response.Body}");
        }
        return response.Json.GetProperty("runKey").GetString()
            ?? throw new InvalidOperationException("Practice mode answered without a run key.");
    }
}
