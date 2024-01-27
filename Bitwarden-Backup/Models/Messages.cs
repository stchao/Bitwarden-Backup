namespace Bitwarden_Backup.Models
{
    internal static class Prompts
    {
        internal const string LoginMethod = "How would you like to log in? ";
        internal const string TwoFactorMethod = "How would you like to log in? ";
        internal const string ClientId = "? Client Id: ";
        internal const string ClientSecret = "? Client Secret: ";
        internal const string MasterPassword = "? Master Password: (input is hidden) ";
        internal const string Email = "? Email address: ";
        internal const string TwoFactorCode = "? Two-step login code: ";
    }

    internal static class Texts
    {
        internal const string MoreChoices = "[grey](Move up and down to reveal more choices)[/]";
        internal const string DefaultValidationResult = "[red]Value cannot be empty or null.[/]";
    }

    internal static class ErrorMessages
    {
        internal const string BwExeNotFound =
            "The Bitwarden cli native executable file is not in the app directory. Please refer to requirements section of the README.";
        internal const string AlreadyLoggedIn = "You are already logged in as";
        internal const string NoCredentials =
            "Interactive log in is disabled and there are no credential(s) in appsettings.json.";
        internal const string InvalidLogInMethod =
            "The log in methods currently supported are using api key or using email and password credentials.";
        internal const string InvalidTwoFactorMethod =
            "The two factor methods currently supported are using authenticator app, YubiKey OTP security key, or email.";
        internal const string ClientIdValidationResult =
            "[red]Client Id cannot be empty or null.[/]";
        internal const string ClientSecretValidationResult =
            "[red]Client Secret cannot be empty or null.[/]";
        internal const string MasterPasswordValidationResult =
            "[red]Master password must be at least 12 characters and cannot be empty or null.[/]";
        internal const string EmailValidationResult =
            "[red]Email Address must be valid and cannot be empty or null.[/]";
        internal const string TwoFactorCodeValidationResult =
            "[red]Two factor code cannot be empty or null.[/]";
    }
}
