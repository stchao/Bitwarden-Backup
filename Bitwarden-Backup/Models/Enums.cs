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
}
