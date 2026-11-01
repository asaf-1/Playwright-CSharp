using System.Text.RegularExpressions;
using Core.Data;
using Core.Pages;

namespace NUnitTests.Ui;

[Category("Ui")]
public class SignUpTests : NUnitBaseTest
{
    [Test]
    public async Task New_visitor_signs_up_and_lands_on_their_profile()
    {
        var email = FreshCustomer.NewEmail();
        var signUp = new SignUpPage(Page);
        var profile = new ProfilePage(Page);

        await signUp.GotoAsync();
        await signUp.SignUpAsync("Pwcs Signup Tester", email, FreshCustomer.NewPassword());

        await Expect(Page).ToHaveURLAsync(new Regex("/app/profile"));
        await Expect(profile.Welcome).ToBeVisibleAsync();
        await Expect(profile.Name).ToHaveTextAsync("Pwcs Signup Tester");
        await Expect(profile.Email).ToHaveTextAsync(email);
    }
}
