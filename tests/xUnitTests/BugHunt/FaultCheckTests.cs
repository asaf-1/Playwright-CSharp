using Core.BugHunt;

namespace xUnitTests.BugHunt;

/// <summary>
/// A few of the planted faults, checked both ways by the same shared checks as
/// NUnit (Core/BugHunt/FaultChecks.cs): the rule holds with practice mode off,
/// and the fault is found with it on. NUnit runs all 28.
/// </summary>
public class FaultCheckTests : xUnitBaseTest
{
    public static TheoryData<string> SelectedFaults =>
    [
        "bankNegativeTransfer",
        "bankStatementTotal",
        "bankPayeeIdor",
        "bankGraphqlOwnerLeak",
        "bankStaleBalance",
    ];

    private static FaultCheck Check(string flag) => FaultChecks.All.Single(c => c.Flag == flag);

    [Theory]
    [Trait("Category", "Rules")]
    [MemberData(nameof(SelectedFaults))]
    public async Task Rule_holds_on_the_clean_site(string flag)
    {
        var finding = await Check(flag).Detect(NewFaultContext(practice: false));

        TestContext.Current.SendDiagnosticMessage(finding.Evidence);
        Assert.False(finding.Seen, finding.Evidence);
    }

    [Theory]
    [Trait("Category", "BugHunt")]
    [MemberData(nameof(SelectedFaults))]
    public async Task Fault_is_caught_in_practice_mode(string flag)
    {
        var finding = await Check(flag).Detect(NewFaultContext(practice: true));

        TestContext.Current.SendDiagnosticMessage($"CAUGHT {flag}: {finding.Evidence}");
        Assert.True(finding.Seen, $"MISSED {flag}: {finding.Evidence}");
    }
}
