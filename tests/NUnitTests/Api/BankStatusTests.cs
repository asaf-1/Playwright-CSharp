namespace NUnitTests.Api;

[Category("Smoke")]
[Category("Api")]
public class BankStatusTests : NUnitApiBaseTest
{
    [Test]
    public async Task Bank_status_answers_200()
    {
        var response = await Request.GetAsync("/api/bank/status");

        Assert.That(response.Status, Is.EqualTo(418), "Broken on purpose: proves a red test blocks the merge. This PR must never merge.");
    }
}
