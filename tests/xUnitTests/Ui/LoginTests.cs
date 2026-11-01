using System.Text.RegularExpressions;
using Core.Api;
using Core.Pages;
using Core.Pages.Components;

namespace xUnitTests.Ui;

[Trait("Category", "Ui")]
public class LoginTests : xUnitBaseTest
{
    [Fact]
    public async Task Customer_logs_in_with_email_and_password()
    {
        var customer = await SignUpSignedOutCustomerAsync();
        var login = new LoginPage(Page);
        var menu = new AccountMenu(Page);
        var profile = new ProfilePage(Page);

        await login.GotoAsync();
        await login.SignInAsync(customer.Email, customer.Password);

        await Expect(Page).ToHaveURLAsync(new Regex("/app/profile"));
        await Expect(profile.Email).ToHaveTextAsync(customer.Email);
        await Expect(menu.Trigger).ToContainTextAsync(customer.User.FullName.Split(' ')[0]);
    }

    [Fact]
    public async Task Customer_logs_out_from_the_account_menu()
    {
        await SignUpFreshCustomerAsync();
        var bank = new BankPage(Page);
        var menu = new AccountMenu(Page);

        await bank.GotoAsync();
        await Expect(bank.Total).ToBeVisibleAsync();
        await menu.LogOutAsync();

        await Expect(menu.Trigger).ToContainTextAsync("Guest");
        await Expect(bank.SignInPrompt).ToBeVisibleAsync();
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.GetMeAsync());
        Assert.Equal(401, error.Status);
    }
}
