using Core.BugHunt;

namespace NUnitTests.BugHunt;

/// <summary>
/// Each of Playground Bank's 28 planted faults, checked both ways by the same
/// check (Core/BugHunt/FaultChecks.cs):
///   Rules   - practice mode off: the rule must hold (nothing found).
///   BugHunt - practice mode on: the fault must be found.
/// A check that passes both ways is proof the test would catch that bug.
/// </summary>
public class FaultCheckTests : NUnitBaseTest
{
    private static IEnumerable<TestCaseData> Rules() =>
        FaultChecks.All.Select(c => new TestCaseData(c).SetName($"Rule holds: {c.Flag} - {c.Rule}").SetCategory("Rules"));

    private static IEnumerable<TestCaseData> Faults() =>
        FaultChecks.All.Select(c => new TestCaseData(c).SetName($"Fault caught: {c.Flag} - {c.Rule}").SetCategory("BugHunt"));

    [TestCaseSource(nameof(Rules))]
    public async Task Rule_holds_on_the_clean_site(FaultCheck check)
    {
        var finding = await check.Detect(NewFaultContext(practice: false));

        TestContext.Out.WriteLine(finding.Evidence);
        Assert.That(finding.Seen, Is.False, finding.Evidence);
    }

    [TestCaseSource(nameof(Faults))]
    public async Task Fault_is_caught_in_practice_mode(FaultCheck check)
    {
        var finding = await check.Detect(NewFaultContext(practice: true));

        TestContext.Out.WriteLine($"CAUGHT {check.Flag}: {finding.Evidence}");
        Assert.That(finding.Seen, Is.True, $"MISSED {check.Flag}: {finding.Evidence}");
    }
}
