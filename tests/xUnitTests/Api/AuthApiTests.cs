using Core.Api;

namespace xUnitTests.Api;

[Trait("Category", "Api")]
public class AuthApiTests : xUnitApiBaseTest
{
    [Fact]
    public async Task Signed_out_visitor_gets_401_from_their_accounts()
    {
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetAccountsAsync());

        Assert.Equal(401, error.Status);
        Assert.Equal("NOT_SIGNED_IN", error.Code);
    }

    [Fact]
    public async Task Customer_gets_403_from_the_staff_area()
    {
        await SignUpFreshCustomerAsync();

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetAllUsersAsync());

        Assert.Equal(403, error.Status);
        Assert.Equal("FORBIDDEN", error.Code);
    }
}
