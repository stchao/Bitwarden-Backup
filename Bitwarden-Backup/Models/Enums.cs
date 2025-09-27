namespace Bitwarden_Backup.Models
{
    public enum ExportFormat
    {
        json,
        encrypted_json,
        csv
    }

    public enum LogInMethod
    {
        None = 0,
        ApiKey,
        EmailPw
    }

    public enum TwoFactorMethod
    {
        None = -1,
        Authenticator = 0,
        Email = 1,
        YubiKey = 3
    }
}
