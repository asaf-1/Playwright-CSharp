using System.Text.RegularExpressions;
using Core.Api;
using Core.Pages;
using Core.Pages.Components;

namespace NUnitTests.Ui;

[Category("Ui")]
public class LoginTests : NUnitBaseTest
{
    [Test]
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

    [Test]
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
        Assert.That(error.Status, Is.EqualTo(401), "the session should be over on the server too");
    }

    [Test]
    public async Task Wrong_password_shows_an_error_and_keeps_the_visitor_signed_out()
    {
        var customer = await SignUpSignedOutCustomerAsync();
        var login = new LoginPage(Page);

        await login.GotoAsync();
        await login.SignInAsync(customer.Email, "Wrong-password-1");

        await Expect(login.Error).ToHaveTextAsync("Wrong email or password.");
        await Expect(Page).ToHaveURLAsync(new Regex("/app/login"));
    }

    [Test]
    public async Task Fifth_wrong_password_in_a_row_puts_the_login_on_hold()
    {
        var customer = await SignUpSignedOutCustomerAsync();
        var login = new LoginPage(Page);
        await login.GotoAsync();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var answer = await login.SignInAndWaitAsync(customer.Email, "Wrong-password-1");
            Assert.That(answer.Status, Is.EqualTo(401), $"wrong password number {attempt}");
            await Expect(login.Error).ToHaveTextAsync("Wrong email or password.");
        }

        var fifth = await login.SignInAndWaitAsync(customer.Email, "Wrong-password-1");
        Assert.That(fifth.Status, Is.EqualTo(429), "the fifth wrong password starts the hold");
        await Expect(login.Error).ToContainTextAsync("Too many failed log-ins");

        var correct = await login.SignInAndWaitAsync(customer.Email, customer.Password);
        Assert.That(correct.Status, Is.EqualTo(429), "even the right password waits out the hold");
        await Expect(Page).ToHaveURLAsync(new Regex("/app/login"));
    }
}
