using Hangfire.Console;
using Hangfire.Server;
using MailKit.Net.Imap;
using MailKit.Search;
using Umbraco.Cms.Infrastructure.Scoping;
using MailKit;
using NckExchange.Core.Models;


namespace NckExchange.Services;

public class EmailPollingService(IConfiguration configuration, IScopeProvider scopeProvider, ILogger<EmailPollingService> logger)
{
    public async Task DoIt(PerformContext context)
    {
        var settings = configuration.GetSection("EmailPollingSettings");
        
        var imapHost = settings["ImapHost"];
        var imapPort = int.Parse(settings["ImapPort"] ?? "993");
        var imapUseSsl = bool.Parse(settings["ImapUseSsl"] ?? "true");
        var username = settings["Username"];
        var password = settings["Password"];

        if (string.IsNullOrEmpty(imapHost) || string.IsNullOrEmpty(username))
        {
            context.WriteLine("🚨 Email polling configuration is missing or incomplete. Aborting job.");
            return;
        }

        context.WriteLine($"Attempting to connect to IMAP server: {imapHost}:{imapPort}...");

        using var client = new ImapClient();
        try
        {
            client.Connect(imapHost, imapPort, imapUseSsl);

            if (imapUseSsl)
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }

            client.Authenticate(username, password);
            context.WriteLine("✅ Successfully connected and authenticated.");

            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadWrite);

            context.WriteLine($"Total messages in Inbox: {inbox.Count}.");

            var uids = inbox.Search(SearchQuery.NotSeen);
            context.WriteLine($"Found {uids.Count} unread emails.");

            if (uids.Any())
            {
                var progressBar = context.WriteProgressBar();

                foreach (var uid in uids.WithProgress(progressBar, uids.Count))
                {
                    var message = inbox.GetMessage(uid);

                    using var scope = scopeProvider.CreateScope(autoComplete: true);
                    var database = scope.Database;

                    var senderName = message.From.Mailboxes.FirstOrDefault()?.Name ?? "Unknown";
                    var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown@email.com";
                    var emailBody = message.TextBody ?? message.HtmlBody ?? "No content found.";

                    var contactMessage = new ContactMessage
                    {
                        Name = senderName,
                        Email = senderEmail,
                        Message = emailBody,
                        DateSubmitted = message.Date.DateTime,
                        IsAnswered = false
                    };

                    await database.InsertAsync(contactMessage);

                    inbox.AddFlags(uid, MessageFlags.Seen, true);

                    context.WriteLine($"✅ Email processed and saved: {message.Subject} from {senderEmail}.");

                }
            }

            client.Disconnect(quit: true);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "A critical failure occurred during the IMAP connection or search phase for host {Host}.", imapHost);
            
            context.WriteLine($"🚨 A critical error occurred during email polling. Check application logs for full details. Error: {ex.Message}");
        }
    }
}