using Umbraco.Cms.Api.Management.Security;

namespace NckExchange.ExternalUserLogin.GoogleAuthentication;

public static class GoogleBackOfficeExternalLoginProviderOptions
{
    public const string SchemeName = "Google";

    public static void Configure(BackOfficeExternalLoginProviderOptions options)
    {
        // The following options are only relevant if you
        // want to configure auto-linking on the authentication.
        options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
            // Set to true to enable auto-linking
            autoLinkExternalAccount: false,

            // [OPTIONAL]
            // Default: "Editor"
            // Specify User Group.
            defaultUserGroups: null,

            // [OPTIONAL]
            // Default: The culture specified in appsettings.json.
            // Specify the default culture to create the User as.
            // It can be dynamically assigned in the OnAutoLinking callback.
            defaultCulture: null,

            // [OPTIONAL]
            // Enable the ability to link/unlink manually from within
            // the Umbraco backoffice.
            // Set this to false if you don't want the user to unlink
            // from this external login provider.
            allowManualLinking: true
        )
        {
            // [OPTIONAL] Callback
            OnAutoLinking = (autoLinkUser, loginInfo) =>
            {
                // Customize the user before it's linked.
                // Modify the User's groups based on the Claims returned
                // in the external ogin info.
            },
            OnExternalLogin = (user, loginInfo) =>
            {
                // Customize the User before it is saved whenever they have
                // logged in with the external provider.
                // Sync the Users name based on the Claims returned
                // in the external login info

                // Returns a boolean indicating if sign-in should continue or not.
                return true;
            },
        };

        // [OPTIONAL]
        // Disable the ability for users to login with a username/password.
        // If set to true, it will disable username/password login
        // even if there are other external login providers installed.
        options.DenyLocalLogin = true;
    }
}
