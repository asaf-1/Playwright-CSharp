using Core.Contract;
using Core.Data;

namespace NUnitTests.Api;

/// <summary>
/// Every answer in <see cref="ContractCases"/> must have the status the spec
/// lists and a body that matches the spec's schema for that status.
/// </summary>
[Category("Api")]
[Category("Contract")]
public class ContractTests : NUnitApiBaseTest
{
    private static IEnumerable<TestCaseData> Cases() =>
        ContractCases.All().Select(c => new TestCaseData(c).SetName($"Reply matches the spec: {c}"));

    [TestCaseSource(nameof(Cases))]
    public async Task Reply_matches_the_spec(ContractCase contractCase)
    {
        var customer = await SignUpFreshCustomerAsync();
        var signedOut = await NewClientAsync();
        var contract = await OpenApiContract.LoadAsync(BankApi);

        var response = await contractCase.Call(new ContractContext(customer, BankApi, signedOut));
        var problems = contract.Check(contractCase.Method, contractCase.Path, response);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(contractCase.ExpectedStatus), response.Body);
            Assert.That(problems, Is.Empty, $"Body: {response.Body}");
        });
    }

    /// <summary>
    /// Proof the checker can fail: a real reply, broken on purpose, must be
    /// caught, and so must a status the spec doesn't list.
    /// </summary>
    [Test]
    public async Task Contract_check_catches_a_reply_that_breaks_the_spec()
    {
        await SignUpFreshCustomerAsync();
        var contract = await OpenApiContract.LoadAsync(BankApi);
        var real = await BankApi.SendRawAsync("GET", "/api/bank/accounts");

        var balanceAsText = real with
        {
            Body = System.Text.RegularExpressions.Regex.Replace(real.Body, "\"balanceCents\":(\\d+)", "\"balanceCents\":\"$1\""),
        };
        var totalMissing = real with
        {
            Body = System.Text.RegularExpressions.Regex.Replace(real.Body, ",\"totalCents\":\\d+", ""),
        };
        var unlistedStatus = real with { Status = 418 };

        Assert.Multiple(() =>
        {
            Assert.That(contract.Check("GET", "/api/bank/accounts", real), Is.Empty, "the real reply matches");
            Assert.That(contract.Check("GET", "/api/bank/accounts", balanceAsText), Is.Not.Empty, "a balance sent as text");
            Assert.That(contract.Check("GET", "/api/bank/accounts", totalMissing), Is.Not.Empty, "a missing totalCents");
            Assert.That(contract.Check("GET", "/api/bank/accounts", unlistedStatus), Is.Not.Empty, "a status the spec doesn't list");
        });
    }
}
