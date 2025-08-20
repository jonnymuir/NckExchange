using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace NckExchange.ExternalUserLogin.GoogleAuthentication;

public static class GoogleAuthenticationExtensions
{
    public static IUmbracoBuilder AddGoogleAuthentication(this IUmbracoBuilder builder, ConfigurationManager configuration)
    {
        builder.Services.ConfigureOptions<GoogleBackOfficeExternalLoginProviderOptions>();

        builder.AddBackOfficeExternalLogins(logins => logins.AddBackOfficeLogin(
                backOfficeAuthenticationBuilder =>
                {
                    var schemeName =
                        backOfficeAuthenticationBuilder.SchemeForBackOffice(GoogleBackOfficeExternalLoginProviderOptions
                            .SchemeName);

                    ArgumentNullException.ThrowIfNull(schemeName);

                    backOfficeAuthenticationBuilder.AddGoogle(
                        schemeName,
                        options =>
                        {
                            options.CallbackPath = "/umbraco-google-signin";
                            options.ClientId = configuration["Authentication:Google:ClientId"]!;
                            options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;

                            options.Events = new OAuthEvents
                            {
                                OnTicketReceived = async context =>
                                {
                                    // This executes after Google authenticates the user,
                                    // but before Umbraco (or Identity) tries to sign them in.
                                    var httpContext = context.HttpContext;
                                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<GoogleBackOfficeExternalLoginProviderOptions>>();
                                    var backOfficeUserManager = httpContext.RequestServices.GetRequiredService<IBackOfficeUserManager>();
                                    var userService = httpContext.RequestServices.GetRequiredService<IUserService>();
                                    var signInManager = httpContext.RequestServices.GetRequiredService<SignInManager<BackOfficeIdentityUser>>();

                                    var externalLoginInfo = context.Principal;
                                    var loginProvider = context.Scheme.Name;
                                    var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                                    if (string.IsNullOrEmpty(providerKey))
                                    {
                                        logger.LogError("Google providerKey (NameIdentifier) missing from external login ticket.");
                                        context.Fail("Provider key missing.");
                                        return;
                                    }

                                    // 1. Try to find an existing backoffice user linked to this external login
                                    var identityUser = await backOfficeUserManager.FindByLoginAsync(loginProvider, providerKey);

                                    // 2. If no user is linked to this specific Google login yet:
                                    if (identityUser == null)
                                    {
                                        // Try to find an existing Umbraco user by email
                                        var email = externalLoginInfo?.FindFirstValue(ClaimTypes.Email);

                                        if (!string.IsNullOrEmpty(email))
                                        {
                                            var existingUmbracoUser = userService.GetByEmail(email);

                                            if (existingUmbracoUser != null)
                                            {
                                                logger.LogInformation("Existing Umbraco user found by email '{Email}'. Attempting to link Google account.", email);

                                                // Convert existing Umbraco IUser to BackOfficeIdentityUser
                                                var existingIdentityUser = await backOfficeUserManager.FindByIdAsync(existingUmbracoUser.Key.ToString());

                                                if (existingIdentityUser == null)
                                                {
                                                    logger.LogWarning("Identity user not found for existing Umbraco user with email '{Email}'. Cannot link Google account.", email);
                                                    context.Fail("Could not find identity user for existing Umbraco user.");
                                                    return;
                                                }

                                                // Link the Google login to the existing Umbraco user
                                                var addLoginResult = await backOfficeUserManager.AddLoginAsync(existingIdentityUser, new UserLoginInfo(loginProvider, providerKey, loginProvider));

                                                if (addLoginResult.Succeeded)
                                                {
                                                    logger.LogInformation("Successfully linked Google account for existing Umbraco user '{Email}'.", email);

                                                    // Sign in the Umbraco user
                                                    // This issues the authentication cookie and sets HttpContext.User
                                                    await signInManager.SignInAsync(existingIdentityUser, isPersistent: false); 
                                                    
                                                    // IMPORTANT: Transfer the principal from the SignInManager's result to the OAuth context
                                                    // The SignInManager has now set HttpContext.User correctly, we need to apply that to context.Principal
                                                    context.Principal = httpContext.User;
                                                    context.Success();
                                                    context.HandleResponse(); // Indicate that we've fully handled the authentication ticket
                                                    return; // Exit the callback
                                                }
                                                else
                                                {
                                                    logger.LogError("Failed to link Google account for existing Umbraco user '{Email}'. Errors: {Errors}", email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                                                    context.Fail("Failed to link external login.");
                                                }
                                            }
                                            else
                                            {
                                                logger.LogInformation("No existing Umbraco user found for email '{Email}'. External login will fail as auto-linking is disabled.", email);
                                                // If autoLinkExternalAccount is false (as per your config), and no existing user by email,
                                                // we let the default Identity flow fail, which is what you want.
                                                context.Fail("No linked user and auto-linking disabled."); // Explicitly fail the context
                                            }
                                        }
                                        else
                                        {
                                            logger.LogWarning("Email claim missing from Google principal. Cannot link existing user.");
                                            context.Fail("Email claim missing.");
                                        }
                                    }
                                    else
                                    {
                                        // User is already linked, nothing to do, let Identity flow continue to sign in
                                        logger.LogInformation("Google account already linked for Umbraco user '{UserName}'.", identityUser.UserName);
                                        context.Success();
                                    }
                                }
                            };
                        });
                }));
        return builder;
    }
}
