using Core.Api;
using Core.Data;

namespace NUnitTests.Api;

/// <summary>Who may do what: signed out, customer, support, demo and locked accounts.</summary>
[Category("Api")]
public class AuthApiTests : NUnitApiBaseTest
{
    [Test]
    public async Task Signed_out_visitor_gets_401_from_their_accounts()
    {
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetAccountsAsync());

        Assert.That((error.Status, error.Code), Is.EqualTo((401, "NOT_SIGNED_IN")));
    }

    [Test]
    public async Task Customer_gets_403_from_the_staff_area()
    {
        await SignUpFreshCustomerAsync();

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetAllUsersAsync());

        Assert.That((error.Status, error.Code), Is.EqualTo((403, "FORBIDDEN")));
    }

    [Test]
    public async Task Support_can_read_users_but_not_change_them()
    {
        await BankApi.LoginAsync(DemoAccounts.Support, DemoAccounts.Password);

        var users = await BankApi.GetAllUsersAsync();
        var maya = users.Single(u => u.Email == DemoAccounts.Customer);
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.ChangeUserAsync(maya.Id, status: "locked"));

        Assert.That((error.Status, error.Code), Is.EqualTo((403, "FORBIDDEN")));
    }

    [Test]
    public async Task Demo_customer_cannot_move_money()
    {
        await BankApi.LoginAsync(DemoAccounts.Customer, DemoAccounts.Password);
        var account = (await BankApi.GetAccountsAsync()).Accounts[0];

        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.DepositAsync(account.Id, 100));

        Assert.That((error.Status, error.Code), Is.EqualTo((403, "DEMO_READ_ONLY")));
    }

    [Test]
    public async Task Locked_account_cannot_sign_in()
    {
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.LoginAsync(DemoAccounts.Locked, DemoAccounts.Password));

        Assert.That((error.Status, error.Code), Is.EqualTo((403, "ACCOUNT_LOCKED")));
    }
}
