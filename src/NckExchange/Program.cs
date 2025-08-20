using Slimsy.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using NckExchange.ExternalUserLogin.GoogleAuthentication;


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
