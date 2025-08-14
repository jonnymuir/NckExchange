using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity; // For SignInManager<MemberIdentityUser>
using Umbraco.Cms.Core.Security; // For MemberIdentityUser, IMemberManager
using System.Security.Claims;
using Umbraco.Cms.Core.Services; // For FindFirstValue(ClaimTypes.Email)

namespace NckExchange.Controllers;

[Route("[controller]")] // All actions in this controller will be prefixed with /Account
public class AccountController(
    SignInManager<MemberIdentityUser> signInManager,
    IMemberManager memberManager,
    ILogger<AccountController> logger,
    IMemberGroupService memberGroupService) : Controller
{
    // GET: /Account/Login
    [HttpGet("Login")] // Responds to /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(); // Renders Views/Account/Login.cshtml
    }

    // POST: /Account/ExternalLogin (Initiates the Google login flow)
    [HttpPost("ExternalLogin")] // Responds to /Account/ExternalLogin
    [ValidateAntiForgeryToken] // Protects against CSRF
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        // Request a redirect to the external login provider (e.g., Google).
        // The callback URL is where Google will send the user back after authentication.
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties); // Triggers the external authentication
    }

    [HttpGet("ExternalLoginCallback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            logger.LogError("Error from external provider during Google login: {RemoteError}", remoteError);
            return View(nameof(Login)); // Return to login page with error
        }

        // Get external login info (user's claims from Google)
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ModelState.AddModelError(string.Empty, "Error loading external login information.");
            logger.LogError("Error loading external login information after Google callback.");
            return View(nameof(Login));
        }

        // This is the key change: REMOVE the ExternalLoginSignInAsync call.
        // Instead, go directly to creating or linking the member.

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "Email claim not found from Google. Cannot create member.");
            logger.LogError("Email claim missing from Google for user: {NameIdentifier}", info.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
            return View(nameof(Login));
        }

        // Check if a member already exists by email (to link existing members)
        var existingMember = await memberManager.FindByEmailAsync(email);
        MemberIdentityUser? member;

        if (existingMember == null)
        {
            // If no existing member, create a new Umbraco Member
            member = new MemberIdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await memberManager.CreateAsync(member);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                logger.LogError("Failed to create new Umbraco member for Google user: {Email}", email);
                return View(nameof(Login));
            }

            logger.LogInformation("Created new Umbraco member for Google user: {Email}", email);

            // Link the external login to the newly created member
            var addLoginResult = await signInManager.UserManager.AddLoginAsync(member, info);
            if (!addLoginResult.Succeeded)
            {
                logger.LogError("Failed to add external login for new member {Email}", email);
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(nameof(Login));
            }
        }
        else
        {
            // If member exists but doesn't have this specific external login linked, add it
            member = existingMember;

            // Check if the login is already linked before trying to add it
            var logins = await signInManager.UserManager.GetLoginsAsync(member);
            if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
            {
                var addLoginResult = await signInManager.UserManager.AddLoginAsync(member, info);
                if (!addLoginResult.Succeeded)
                {
                    logger.LogError("Failed to link external login to existing member {Email}", email);
                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(nameof(Login));
                }
                logger.LogInformation("Linked {LoginProvider} login to existing member {Email}", info.LoginProvider, email);
            }
        }

        // Get the ClaimsPrincipal immediately after sign-in
        if (member != null)
        {
            var memberRolesBeforeSignIn = await memberManager.GetRolesAsync(member);
            var claimsFactory = HttpContext.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<MemberIdentityUser>>();
            var principalForSignIn = await claimsFactory.CreateAsync(member);

            var claimsRolesFromFactory = principalForSignIn.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            // Manually add role claims to the principal before signing in
            if (memberRolesBeforeSignIn != null && memberRolesBeforeSignIn.Any())
            {
                if (principalForSignIn.Identity is ClaimsIdentity identity)
                {
                    foreach (var role in memberRolesBeforeSignIn)
                    {
                        if (!identity.HasClaim(ClaimTypes.Role, role)) // Prevent duplicate claims
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }
                    }
                }
            }

            // Sign in the user with the (newly created or linked) member account, using the generated principal
            await signInManager.SignInWithClaimsAsync(member, false, principalForSignIn.Claims);

            logger.LogInformation("User {Email} signed in after external login.", member.Email);

            // Get the ClaimsPrincipal immediately after sign-in from HttpContext.User
            // (This is the principal that will be used for authorization checks after redirect)
            var currentUserAfterSignIn = HttpContext.User;
            var claimsRolesAfterSignIn = currentUserAfterSignIn.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            logger.LogInformation("DEBUG: HttpContext.User roles AFTER SignIn: {Roles}", string.Join(", ", claimsRolesAfterSignIn));
        }

        return RedirectToLocal(returnUrl);
    }

    // POST: /Account/Logout
    [HttpPost("Logout")] // Responds to /Account/Logout
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");
        // Redirect to homepage or login page after logout
        return RedirectToLocal(Url.Content("~/"));
    }

    // Helper method to redirect to a local URL to prevent open redirect vulnerabilities
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        // Fallback redirect if returnUrl is not local or null
        return RedirectToAction(nameof(Index), "Home");
    }
}
