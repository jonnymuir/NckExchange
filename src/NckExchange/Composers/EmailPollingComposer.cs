
using Hangfire;
using NckExchange.Services;
using Umbraco.Cms.Core.Composing;

namespace NckExchange.Composers;
public class EmailPollingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {            
        // 1. Register the EmailPollingService with the DI container
        builder.Services.AddScoped<EmailPollingService>();

        // 2. Schedule the recurring job using the service method.
        // Hangfire uses the DI container to instantiate and inject dependencies 
        // into the service when the job runs.
        RecurringJob.AddOrUpdate<EmailPollingService>(
            "Email Polling", 
            x => x.DoIt(null!), // Pass null for PerformContext, Hangfire will inject it at runtime
            Cron.Hourly()
        );
    }
}