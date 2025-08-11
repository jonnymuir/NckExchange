using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using NckExchange.Core.Models; // Your ContactMessage model
using NckExchange.Models; // Your ContactFormViewModel
using Umbraco.Cms.Infrastructure.Scoping;


namespace NckExchange.Controllers;

[Route("api/[controller]/[action]")] // Route for your API controller
[ApiController] // Indicates this is an API controller
public class ContactApiController(IScopeProvider scopeProvider, ILogger<ContactApiController> logger) : UmbracoApiController // Inherit from UmbracoApiController
{
    [HttpPost]
    [ValidateAntiForgeryToken] // Protect against Cross-Site Request Forgery
    public async Task<IActionResult> SubmitMessage([FromForm] ContactFormViewModel model)
    {
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
}