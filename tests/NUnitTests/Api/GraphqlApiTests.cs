using Core.Data;

namespace NUnitTests.Api;

[Category("Api")]
[Category("GraphQL")]
public class GraphqlApiTests : NUnitApiBaseTest
{
    private const string AccountsQuery = "{ accounts { id number kind balanceCents } }";
    private const string AccountQuery = "query ($id: ID!) { account(id: $id) { id number balanceCents } }";

    [Test]
    public async Task GraphQL_accounts_match_the_REST_accounts()
    {
        await SignUpFreshCustomerAsync();

        var rest = (await BankApi.GetAccountsAsync()).Accounts;
        var reply = await BankApi.GraphqlAsync(AccountsQuery);
        var graphql = reply.Data!.Value.GetProperty("accounts").EnumerateArray()
            .Select(a => (a.GetProperty("id").GetString(), a.GetProperty("number").GetString(), a.GetProperty("balanceCents").GetInt64()))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(reply.HasErrors, Is.False);
            Assert.That(graphql, Is.EquivalentTo(rest.Select(a => ((string?)a.Id, (string?)a.Number, a.BalanceCents))));
        });
    }

    [Test]
    public async Task GraphQL_never_shows_another_customers_account()
    {
        await SignUpFreshCustomerAsync();
        var otherClient = await NewClientAsync();
        var other = await FreshCustomer.CreateAsync(otherClient);

        var reply = await BankApi.GraphqlAsync(AccountQuery, new { id = other.Checking.Id });

        Assert.Multiple(() =>
        {
            Assert.That(reply.HasErrors, Is.True);
            Assert.That(reply.Errors![0].Extensions?.Code, Is.EqualTo("ACCOUNT_NOT_FOUND"));
            Assert.That(reply.Data?.ToString() ?? "", Does.Not.Contain(other.Checking.Number),
                "the other customer's account number must not leak");
        });
    }

    [Test]
    public async Task GraphQL_asks_signed_out_visitors_to_sign_in()
    {
        var reply = await BankApi.GraphqlAsync("{ me { email } }");

        Assert.That(reply.Errors?.Select(e => e.Extensions?.Code), Is.EqualTo(new[] { "NOT_SIGNED_IN" }));
    }
}
