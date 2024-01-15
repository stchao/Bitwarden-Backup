namespace Bitwarden_Backup.Models
{
    internal enum ExportFormat
    {
        json,
        encrypted_json
    }

    internal enum CredentialType
    {
        None = 0,
        Api,
        EmailPw
    }

    internal enum TwoFactorMethod
    {
        None = -1,
        Authenticator = 0,
        Email = 1,
        YubiKey = 3
    }
}
