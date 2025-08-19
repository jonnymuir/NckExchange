using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NckExchange.Core.Models;
using NckExchange.Models;
using Umbraco.Cms.Core.Mail;
using System.Security.Claims;
using Umbraco.Cms.Infrastructure.Scoping;
using System.Net;
using NPoco;

namespace NckExchange.Controllers;

[Authorize(Roles = "Administrator")]
[Route("admin")] 
public class AdminController(
    IScopeProvider scopeProvider,
    ILogger<AdminController> logger,
    IEmailSender emailSender) : Controller
{
    // GET: /admin
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

    [HttpGet("reply/{id:int}")]
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
            Answer = message.Answer ?? string.Empty
        };

        return View(replyViewModel);
    }

    // POST: /admin/reply
    // Handles the submission of the reply form.
    [HttpPost("reply/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply([FromForm] AdminReplyViewModel model,int id)
    {
        if (!ModelState.IsValid)
        {
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

            await SendReplyEmail(
                model.OriginalSenderEmail,
                messageToUpdate.Name,
                messageToUpdate.Message,
                model.Answer,
                model
            );

            TempData["SuccessMessage"] = "Reply sent and message updated successfully! Email dispatched.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error replying to message ID {MessageId} for email {Email}", model.Id, model.OriginalSenderEmail);
            ModelState.AddModelError("", "An error occurred while sending the reply. Please try again.");
            return View(model);
        }
    }

    private async Task SendReplyEmail(
        string recipientEmail,
        string originalSenderName,
        string originalMessage,
        string answer,
        AdminReplyViewModel model
    )
    {
        var senderEmail = "support@theexchange-tod.com"; 

        var subject = "Your inquiry to The Exchange Tod has been answered";
        
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

        var emailMessage = new Umbraco.Cms.Core.Models.Email.EmailMessage(
            to: recipientEmail,
            from: senderEmail,
            subject: subject,
            body: body,
            isBodyHtml: true
        );

        try
        {
            await emailSender.SendAsync(emailMessage, "ContactMessageReply");
            logger.LogInformation("Email reply sent to {RecipientEmail} for message ID {MessageId}", recipientEmail, model.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email reply to {RecipientEmail} for message ID {MessageId}. Check SMTP configuration.", recipientEmail, model.Id);
            throw;
        }
    }
}
