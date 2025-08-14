using Slimsy.DependencyInjection;
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Core.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// NEW: Configure external authentication (Google OAuth)
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        // Get your Google Client ID and Client Secret from appsettings.json
        // Make sure you've added the "Authentication:Google:ClientId" and "ClientSecret" to your appsettings.json
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        // Request necessary scopes from Google for user profile and email
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // This is the default callback path where Google will redirect after authentication.
        // It must match one of the "Authorized redirect URIs" in your Google Cloud Console project.
        options.CallbackPath = "/signin-google";

        // Optional: If you want to specify a different sign-in scheme,
        // you might use IdentityConstants.ExternalScheme here.
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

// NEW: Configure the application cookie for Umbraco members authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    // Set the path where unauthenticated members will be redirected for login
    options.LoginPath = "/login"; 
    // Optional: Set a path for access denied redirects (e.g., if a member lacks a required role)
    options.AccessDeniedPath = "/access-denied"; 
});

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddSlimsy()
    .AddDeliveryApi()
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

// Ensure these authentication and authorization middleware are in the pipeline
// and placed before routing middleware. They should already be present in your app.
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
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
