using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;

namespace NckExchange.ExternalUserLogin.GoogleAuthentication;

public static class GoogleAuthenticationExtensions
{
    public static IUmbracoBuilder AddGoogleAuthentication(this IUmbracoBuilder builder, ConfigurationManager configuration)
    {
        builder.AddBackOfficeExternalLogins(logins => logins.AddBackOfficeLogin(
                backOfficeAuthenticationBuilder =>
                {
                    var schemeName = BackOfficeAuthenticationBuilder.SchemeForBackOffice(
                        GoogleBackOfficeExternalLoginProviderOptions.SchemeName);

                    ArgumentNullException.ThrowIfNull(schemeName);

                    backOfficeAuthenticationBuilder.AddGoogle(
                        schemeName,
                        "Google",
                        options =>
                        {
                            options.CallbackPath = "/umbraco-google-signin";
                            options.ClientId = configuration["Authentication:Google:ClientId"]!;
                            options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                            options.AdditionalAuthorizationParameters["prompt"] = "select_account";

                            options.Events = new OAuthEvents
                            {
                                OnTicketReceived = async context =>
                                {
                                    var httpContext = context.HttpContext;
                                    var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                                    var logger = loggerFactory.CreateLogger("GoogleBackOfficeAuth");
                                    var backOfficeUserManager = httpContext.RequestServices.GetRequiredService<IBackOfficeUserManager>();
                                    var userService = httpContext.RequestServices.GetRequiredService<IUserService>();

                                    var externalLoginInfo = context.Principal;
                                    var loginProvider = context.Scheme.Name;
                                    var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                                    if (string.IsNullOrEmpty(providerKey))
                                    {
                                        logger.LogError("Google providerKey (NameIdentifier) missing from external login ticket.");
                                        context.Fail("Provider key missing.");
                                        return;
                                    }

                                    // 1. Try to find an existing backoffice user linked to this specific external login (providerKey)
                                    var identityUser = await backOfficeUserManager.FindByLoginAsync(loginProvider, providerKey);

                                    // 2. If no user is linked to this specific Google login (providerKey) yet:
                                    if (identityUser == null)
                                    {
                                        // Try to find an existing Umbraco user by email
                                        var email = externalLoginInfo?.FindFirstValue(ClaimTypes.Email);

                                        if (!string.IsNullOrEmpty(email))
                                        {
                                            var existingUmbracoUser = userService.GetByEmail(email);

                                            if (existingUmbracoUser != null)
                                            {
                                                logger.LogInformation("Existing Umbraco user found by email '{Email}'. Evaluating Google account linking.", email);

                                                var existingIdentityUser = await backOfficeUserManager.FindByIdAsync(existingUmbracoUser.Key.ToString());

                                                if (existingIdentityUser == null)
                                                {
                                                    logger.LogWarning("Identity user not found for existing Umbraco user with email '{Email}'. Cannot link Google account.", email);
                                                    context.Fail("Could not find identity user for existing Umbraco user.");
                                                    return;
                                                }

                                                // --- CRITICAL SECURITY CHECK ---
                                                // Fetch ALL external logins for this EXISTING Umbraco user (found by email).
                                                var existingLoginsForThisUmbracoUser = await backOfficeUserManager.GetLoginsAsync(existingIdentityUser);

                                                // CHECK 1: Is this specific incoming Google providerKey already linked to this user? (Should be caught by FindByLoginAsync earlier, but defensive)
                                                if (existingLoginsForThisUmbracoUser.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
                                                {
                                                    logger.LogInformation("Google account for '{Email}' is ALREADY linked to this specific user. Skipping re-linking and allowing login.", email);
                                                    context.Success();
                                                    return;
                                                }
                                                // Does this user already have *ANY* Google account linked, but with a DIFFERENT providerKey?
                                                // This addresses the potential hijacking/duplication (unlikely because the email is unique, but still a good check).
                                                else if (existingLoginsForThisUmbracoUser.Any(l => l.LoginProvider == loginProvider))
                                                {
                                                    logger.LogWarning("Existing Umbraco user '{Email}' is already linked to a DIFFERENT Google account. Preventing link to new Google account (Potential security issue or misconfiguration).", email);
                                                    // This is the point where you prevent the linking if another Google account is already there.
                                                    context.Fail("User is already linked to a different Google account. Cannot link another.");
                                                    return;
                                                }
                                                // If neither of the above, then the user exists, and has no Google account linked yet. Proceed to link.
                                                else
                                                {
                                                    logger.LogInformation("Existing Umbraco user '{Email}' found and has no Google account linked yet. Proceeding to link.", email);
                                                    // Link the Google login to the existing Umbraco user
                                                    var addLoginResult = await backOfficeUserManager.AddLoginAsync(existingIdentityUser, new UserLoginInfo(loginProvider, providerKey, loginProvider));

                                                    if (addLoginResult.Succeeded)
                                                    {
                                                        logger.LogInformation("Successfully linked Google account for existing Umbraco user '{Email}'.", email);

                                                        if (existingUmbracoUser.IsApproved == false)
                                                        {
                                                            existingUmbracoUser.IsApproved = true;
                                                            userService.Save(existingUmbracoUser);
                                                            logger.LogInformation("Automatically approved invited user '{Email}'.", email);
                                                        }
                                                        context.Success();
                                                    }
                                                    else
                                                    {
                                                        logger.LogError("Failed to link Google account for existing Umbraco user '{Email}'. Errors: {Errors}", email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                                                        context.Fail("Failed to link external login.");
                                                    }
                                                }
                                            }
                                            else // No existing Umbraco user found by email
                                            {
                                                logger.LogInformation("No existing Umbraco user found for email '{Email}'. External login will fail as auto-linking is disabled.", email);
                                                context.Fail("No linked user and auto-linking disabled."); // Explicitly fail the context
                                            }
                                        }
                                        else // Email claim missing from Google principal
                                        {
                                            logger.LogWarning("Email claim missing from Google principal. Cannot link existing user.");
                                            context.Fail("Email claim missing.");
                                        }
                                    }
                                    else
                                    {
                                        // User is already linked (via FindByLoginAsync at the start). Simply set success.
                                        logger.LogInformation("Google account already linked for Umbraco user '{UserName}'.", identityUser.UserName);
                                        context.Success();
                                    }
                                }
                            };
                        });
                },
                GoogleBackOfficeExternalLoginProviderOptions.Configure));
        return builder;
    }
}
