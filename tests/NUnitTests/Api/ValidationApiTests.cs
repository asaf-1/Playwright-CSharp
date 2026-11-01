using Core.Api;
using Core.Api.Models;
using Core.Data;

namespace NUnitTests.Api;

[Category("Api")]
public class ValidationApiTests : NUnitApiBaseTest
{
    [Test]
    public async Task Sign_up_with_bad_fields_lists_every_problem()
    {
        var error = await Assert.ThrowsAsync<BankApiException>(() => BankApi.RegisterAsync(
            new RegisterRequest("X", "not-an-email", "short", AcceptTerms: false)));

        Assert.Multiple(() =>
        {
            Assert.That((error.Status, error.Code), Is.EqualTo((400, "VALIDATION_FAILED")));
            Assert.That(error.Error?.Errors?.Keys, Is.EquivalentTo(new[] { "fullName", "email", "password", "acceptTerms" }));
        });
    }

    [Test]
    public async Task Sign_up_with_a_taken_email_answers_409()
    {
        var customer = await SignUpFreshCustomerAsync();
        var otherClient = await NewClientAsync();

        var error = await Assert.ThrowsAsync<BankApiException>(() => otherClient.RegisterAsync(
            new RegisterRequest("Pwcs Copycat", customer.Email, FreshCustomer.NewPassword())));

        Assert.That((error.Status, error.Code), Is.EqualTo((409, "EMAIL_TAKEN")));
    }

    [Test]
    public async Task A_request_that_is_not_json_answers_415()
    {
        var customer = await SignUpFreshCustomerAsync();

        var response = await BankApi.SendRawAsync("POST", $"/api/bank/accounts/{customer.Checking.Id}/deposits",
            "amountCents=500", contentType: "application/x-www-form-urlencoded");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(415));
            Assert.That(response.Json.GetProperty("code").GetString(), Is.EqualTo("JSON_REQUIRED"));
        });
    }
}
