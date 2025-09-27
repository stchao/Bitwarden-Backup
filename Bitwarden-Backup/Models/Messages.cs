namespace Bitwarden_Backup.Models
{
    public static class Prompts
    {
        public const string LoginMethod = "How would you like to log in? ";
        public const string TwoFactorMethod = "How would you like to log in? ";
        public const string ClientId = "? Client Id: ";
        public const string ClientSecret = "? Client Secret: ";
        public const string MasterPassword = "? Master Password: (input is hidden) ";
        public const string Email = "? Email address: ";
        public const string TwoFactorCode = "? Two-step login code: ";
        public const string Retry = "Would you like to try again (Y/N)? ";
    }

    public static class Texts
    {
        public const string MoreChoices = "[grey](Move up and down to reveal more choices)[/]";
        public const string DefaultValidationResult = "[red]Value cannot be empty or null.[/]";
    }

    public static class ErrorMessages
    {
        public const string BwExeNotFound =
            "The Bitwarden cli native executable file is not in the app directory. Please refer to requirements section of the README.";
        public const string AlreadyLoggedIn = "You are already logged in as";
        public const string NoCredentials =
            "Interactive log in is disabled and there are no credential(s) in appsettings.json.";
        public const string InvalidLogInMethod =
            "The log in methods currently supported are using api key or using email and password credentials.";
        public const string InvalidTwoFactorMethod =
            "The two factor methods currently supported are using authenticator app, YubiKey OTP security key, or email.";
        public const string ClientIdValidationResult = "[red]Client Id cannot be empty or null.[/]";
        public const string ClientSecretValidationResult =
            "[red]Client Secret cannot be empty or null.[/]";
        public const string MasterPasswordValidationResult =
            "[red]Master password must be at least 12 characters and cannot be empty or null.[/]";
        public const string EmailValidationResult =
            "[red]Email Address must be valid and cannot be empty or null.[/]";
        public const string TwoFactorCodeValidationResult =
            "[red]Two factor code cannot be empty or null.[/]";
        public const string YNValidationResult = "[red]Valid response(s) are Y or N.[/]";
    }
}
