using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using NckExchange.Core.Models; // Your ContactMessage model
using NckExchange.Models; // Your ContactFormViewModel
using Umbraco.Cms.Infrastructure.Scoping;
using Newtonsoft.Json;


namespace NckExchange.Controllers;

[Route("api/[controller]/[action]")]
[ApiController] 
public class ContactApiController(
    IScopeProvider scopeProvider,
    ILogger<ContactApiController> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : UmbracoApiController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitMessage([FromForm] ContactFormViewModel model)
    {
        if (!await VerifyRecaptcha(model.RecaptchaToken))
        {
            logger.LogWarning("reCAPTCHA verification failed for submission from {Email}.", model.Email);
            ModelState.AddModelError("RecaptchaToken", "reCAPTCHA verification failed. Please try again.");
        }

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Invalid contact form submission: {Errors}", ModelState.Values);
            return BadRequest(new { success = false, message = "Please correct the form errors.", errors = ModelState });
        }

        using var scope = scopeProvider.CreateScope(autoComplete: true); // autoComplete: true ensures transaction commit on success
        try
        {
            var database = scope.Database; // Access the Umbraco database context
            var contactMessage = new ContactMessage
            {
                Name = model.Name,
                Email = model.Email,
                Message = model.Message,
                DateSubmitted = DateTime.UtcNow,
                IsAnswered = false // New messages are initially unanswered
            };

            await database.InsertAsync(contactMessage); // Insert the message into the database
            logger.LogInformation("Contact message from {Email} saved successfully.", model.Email);

            return Ok(new { success = true, message = "Your message has been sent successfully! We will get back to you soon." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving contact message from {Email}", model.Email);
            // No need to call scope.Complete(false) explicitly here as autoComplete: true handles rollback on exception
            return StatusCode(500, new { success = false, message = "An error occurred while sending your message. Please try again." });
        }
    }

    private async Task<bool> VerifyRecaptcha(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("reCAPTCHA token is empty.");
            return false;
        }

        var secretKey = configuration["ReCaptcha:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            logger.LogError("reCAPTCHA SecretKey is not configured in appsettings.json.");
            // In production, you might want to fail the request if the secret key is missing.
            return false;
        }

        using (var httpClient = httpClientFactory.CreateClient())
        {
            var response = await httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null); // No content needed, parameters are in the URL

            response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response status is an error code

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaVerificationResult>(jsonResponse);

            // Check the success and score. A typical threshold for v3 is 0.5 or 0.7.
            // You can adjust this threshold based on your needs.
            if (recaptchaResult != null && recaptchaResult.Success && recaptchaResult.Score >= 0.5) // Adjust score threshold as needed
            {
                logger.LogInformation("reCAPTCHA verification successful. Score: {Score}", recaptchaResult.Score);
                return true;
            }
            else
            {
                logger.LogWarning("reCAPTCHA verification failed. Success: {Success}, Score: {Score}, Errors: {ErrorCodes}",
                    recaptchaResult?.Success, recaptchaResult?.Score, string.Join(", ", recaptchaResult?.ErrorCodes ?? Array.Empty<string>()));
                return false;
            }
        }
    }

    private class RecaptchaVerificationResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("score")]
        public float Score { get; set; }

        [JsonProperty("action")]
        public string? Action { get; set; }

        [JsonProperty("challenge_ts")]
        public DateTime ChallengeTimestamp { get; set; }

        [JsonProperty("hostname")]
        public string? Hostname { get; set; }

        [JsonProperty("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }

}