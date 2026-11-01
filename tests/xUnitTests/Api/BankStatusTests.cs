namespace xUnitTests.Api;

[Trait("Category", "Smoke")]
[Trait("Category", "Api")]
public class BankStatusTests : xUnitApiBaseTest
{
    [Fact]
    public async Task Bank_status_answers_200()
    {
        var response = await Request.GetAsync("/api/bank/status");

        Assert.Equal(200, response.Status);
    }
}
