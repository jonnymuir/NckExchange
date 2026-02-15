using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Slimsy.DependencyInjection;
using Umbraco.Cms.Core;
using NckExchange.ExternalUserLogin.GoogleAuthentication;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Services;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.CallbackPath = "/signin-google";
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.AdditionalAuthorizationParameters["prompt"] = "select_account";
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";   
});

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddSlimsy()
    .AddDeliveryApi()
    .AddGoogleAuthentication(builder.Configuration)
    .AddComposers();

// Conditionally add Azure Blob services (existing code)
if (!string.IsNullOrEmpty(builder.Configuration["Umbraco:Storage:AzureBlob:MediaFileSystem:ConnectionString"]))
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
    Console.WriteLine("Azure Blob Media File System added.");
}
else
{
    Console.WriteLine("Azure Blob Media File System not added (config missing).");
}

if (!string.IsNullOrEmpty(builder.Configuration["Umbraco:Storage:AzureBlob:ImageSharpCache:ConnectionString"]))
{
    umbracoBuilder.AddAzureBlobImageSharpCache();
    Console.WriteLine("Azure Blob ImageSharp Cache added.");
}
else
{
    Console.WriteLine("Azure Blob ImageSharp Cache not added (config missing).");
}

umbracoBuilder.AddMembersIdentity();
umbracoBuilder.Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var schemeProvider = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
    var schemes = await schemeProvider.GetAllSchemesAsync();
    var backOfficeSchemes = schemes
        .Where(s => s.Name.StartsWith("Umbraco."))
        .Select(s => s.Name)
        .OrderBy(s => s)
        .ToArray();

    if (backOfficeSchemes.Length == 0)
    {
        logger.LogWarning("No backoffice external auth schemes registered.");
    }
    else
    {
        logger.LogInformation("Backoffice external auth schemes: {Schemes}", string.Join(", ", backOfficeSchemes));
    }

    var externalLoginProviders = app.Services.GetRequiredService<IBackOfficeExternalLoginProviders>();
    var providers = await externalLoginProviders.GetBackOfficeProvidersAsync();
    var providerNames = providers
        .Select(p => p.ExternalLoginProvider.AuthenticationType)
        .OrderBy(p => p)
        .ToArray();

    if (providerNames.Length == 0)
    {
        logger.LogWarning("No backoffice external login providers returned by API.");
    }
    else
    {
        logger.LogInformation("Backoffice external login providers: {Providers}", string.Join(", ", providerNames));
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapGet("/_debug/backoffice-external-logins", async (IBackOfficeExternalLoginProviders providers) =>
    {
        var items = await providers.GetBackOfficeProvidersAsync();
        var result = items.Select(item => new
        {
            item.ExternalLoginProvider.AuthenticationType,
            item.ExternalLoginProvider.Options.DenyLocalLogin,
            DisplayName = item.AuthenticationScheme.DisplayName
        });

        return Results.Ok(result);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });


app.MapGet("/debug-umbraco-core", (IRuntimeState runtimeState, IConfiguration config) =>
{
    var output = new System.Text.StringBuilder();
    output.AppendLine("UMBRACO CORE INTERNAL STATE");
    output.AppendLine("===========================");
    
    // 1. What level is the CMS currently at?
    output.AppendLine($"Current Level: {runtimeState.Level}"); 
    
    // 2. Is there a reason for this level?
    // In v17, if it's Install, it's often because it couldn't validate the database.
    output.AppendLine($"Reason: {runtimeState.Reason}");

    // 3. Check the internal ID comparison directly
    var configId = config["Umbraco:CMS:Global:Id"];
    output.AppendLine($"Config GlobalId: {configId}");
    
    // 4. Check if the database is even reachable by the Core
    // This property returns the exception if the DB check failed
    if (runtimeState.Level == RuntimeLevel.Install)
    {
        output.AppendLine("\nDIAGNOSTIC HINT:");
        output.AppendLine("If level is Install, Umbraco failed to find the 'umbracoUser' table ");
        output.AppendLine("or the Global ID in the database did not match the one above.");
    }

    return output.ToString();
});

await app.RunAsync();
