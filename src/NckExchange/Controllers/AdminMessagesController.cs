using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NckExchange.Core.Models; // Your ContactMessage model
using NckExchange.Models; // Your AdminReplyViewModel
using Umbraco.Cms.Core.Mail; // For Umbraco's email sender service
using System.Security.Claims; // For getting current user's email
using Umbraco.Cms.Infrastructure.Scoping;
using System.Net;
using NPoco;

namespace NckExchange.Controllers;

//[Authorize(Roles = "Administrator")] // Only users with 'Administrator' role can access this controller
[Route("admin/messages")] // Base route for this controller
public class AdminMessagesController(
    IScopeProvider scopeProvider,
    ILogger<AdminMessagesController> logger,
    IEmailSender emailSender) : Controller
{
    // GET: /admin/messages
    // Displays a list of all contact messages with optional limit and status filter.
    public async Task<IActionResult> Index(int? limit, bool? isAnswered)
    {
        List<ContactMessage> messages;
        using (var scope = scopeProvider.CreateScope())
        {
            var database = scope.Database;
            
            var sql = new Sql("SELECT * FROM ContactMessages");

            if (isAnswered.HasValue)
            {
                sql.Append(" WHERE IsAnswered = @0", [isAnswered.Value]);
            }

            // Always order messages (unanswered first, then newest first)
            sql.Append(" ORDER BY IsAnswered ASC, DateSubmitted DESC");

            int actualLimit = limit.GetValueOrDefault(100);
            
            if (actualLimit > 0)
            {
                var pagedMessages = await database.PageAsync<ContactMessage>(1, actualLimit, sql);
                messages = [.. pagedMessages.Items];
            }
            else
            {
                // If limit is 0, fetch all messages (no limit applied)
                messages = [.. await database.FetchAsync<ContactMessage>(sql)];
            }
            
            scope.Complete();
        }

        ViewData["CurrentLimit"] = limit.HasValue && limit.Value > 0 ? limit.Value : 100;
        ViewData["CurrentIsAnswered"] = isAnswered;

        return View(messages);
    }

    // GET: /admin/messages/reply/{id}
    // Displays the form to reply to a specific message.
    [HttpGet("reply/{id:int}")] // Defines a route with an integer ID parameter
    public async Task<IActionResult> Reply(int id)
    {
        ContactMessage? message;
        using (var scope = scopeProvider.CreateScope())
        {
            var database = scope.Database;
            message = await database.SingleOrDefaultByIdAsync<ContactMessage>(id);
            scope.Complete();
        }

        if (message == null)
        {
            logger.LogWarning("Attempted to access non-existent message ID: {MessageId}", id);
            return NotFound("Message not found.");
        }

        // Create a ViewModel for the reply form, pre-populating with original message data
        var replyViewModel = new AdminReplyViewModel
        {
            Id = message.Id,
            OriginalSenderName = message.Name,
            OriginalSenderEmail = message.Email,
            OriginalMessage = message.Message,
            Answer = message.Answer ?? string.Empty // Pre-fill if already answered/edited
        };

        return View(replyViewModel);
    }

    // POST: /admin/messages/reply
    // Handles the submission of the reply form.
    [HttpPost("reply/{id:int}")]
    [ValidateAntiForgeryToken] // Protect against Cross-Site Request Forgery
    public async Task<IActionResult> Reply([FromForm] AdminReplyViewModel model,int id)
    {
        if (!ModelState.IsValid)
        {
            // If model state is invalid, return to the form with validation errors
            return View(model);
        }

        // Create a scope with autoComplete: true to automatically commit on success, rollback on exception
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        try
        {
            var database = scope.Database;
            var messageToUpdate = await database.SingleOrDefaultByIdAsync<ContactMessage>(model.Id);

            if (messageToUpdate == null)
            {
                ModelState.AddModelError("", "Message not found for reply. It might have been deleted.");
                return View(model);
            }

            messageToUpdate.Answer = model.Answer;
            messageToUpdate.IsAnswered = true;
            messageToUpdate.DateAnswered = DateTime.UtcNow;

            await database.UpdateAsync(messageToUpdate); // Update the message in the database

            // Get the email of the currently logged-in administrator
            // This relies on the ASP.NET Core Identity authentication populating the User.Claims
            var adminUserEmail = User.FindFirstValue(ClaimTypes.Email);

            // Send email back to the original sender
            await SendReplyEmail(
                model.OriginalSenderEmail,
                messageToUpdate.Name,
                messageToUpdate.Message,
                model.Answer,
                model
            );

            TempData["SuccessMessage"] = "Reply sent and message updated successfully! Email dispatched.";
            return RedirectToAction(nameof(Index)); // Redirect to the message list
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error replying to message ID {MessageId} for email {Email}", model.Id, model.OriginalSenderEmail);
            ModelState.AddModelError("", "An error occurred while sending the reply. Please try again.");
            // autoComplete: true handles the rollback if an exception occurs
            return View(model);
        }
    }

    // Helper method to send the email reply
    private async Task SendReplyEmail(
        string recipientEmail,
        string originalSenderName,
        string originalMessage,
        string answer,
        AdminReplyViewModel model
    )
    {
        // Configure your sender email in appsettings.json (Umbraco.CMS.Global.Smtp.From)
        // It's good practice to use a "no-reply" address as the 'From' address.
        var senderEmail = "support@theexchange-tod.com"; // **IMPORTANT: Configure this in appsettings.json/Umbraco config**

        var subject = "Your inquiry to The Exchange Tod has been answered";
        
        // Build the HTML email body
        var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        blockquote {{ border-left: 4px solid #007bff; padding-left: 15px; margin: 15px 0; color: #555; background-color: #f9f9f9; }}
                        .footer {{ font-size: 0.8em; color: #888; margin-top: 20px; }}
                        .btn {{ display: inline-block; padding: 10px 20px; margin-top: 15px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <p>Dear {WebUtility.HtmlEncode(originalSenderName ?? "Customer")},</p>
                    <p>Thank you for contacting us. Your original message was:</p>
                    <blockquote>
                        <p><strong>Original Message:</strong></p>
                        <p>{WebUtility.HtmlEncode(originalMessage)}</p>
                    </blockquote>
                    <p>Here is our response:</p>
                    <p>{WebUtility.HtmlEncode(answer)}</p>
                    <p>If you have any further questions, please do not hesitate to reply to this email.</p>
                    <p>Cheers,<br/>The Exchange Tod Support Team</p>
                </body>
                </html>";

        // Create an EmailMessage object for Umbraco's IEmailSender service
        var emailMessage = new Umbraco.Cms.Core.Models.Email.EmailMessage(
            to: recipientEmail,
            from: senderEmail,
            subject: subject,
            body: body,
            isBodyHtml: true // Important: Set to true for HTML emails
        );

        // Send the email. "ContactMessageReply" is an optional tag for logging/tracking.
        try
        {
            await emailSender.SendAsync(emailMessage, "ContactMessageReply");
            logger.LogInformation("Email reply sent to {RecipientEmail} for message ID {MessageId}", recipientEmail, model.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email reply to {RecipientEmail} for message ID {MessageId}. Check SMTP configuration.", recipientEmail, model.Id);
            throw; // Re-throw to ensure the calling method's catch block handles it
        }
    }
}
