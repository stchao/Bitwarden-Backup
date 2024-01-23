﻿namespace Bitwarden_Backup.Models
{
    internal class BitwardenCredentials
    {
        public static string Key = "Credentials";

        public EmailPasswordCredential? EmailPasswordCredential { get; set; }

        public ApiKeyCredential? ApiKeyCredential { get; set; }
    }

    internal class EmailPasswordCredential
    {
        public string Email { get; set; } = string.Empty;

        public string MasterPassword { get; set; } = string.Empty;

        public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;

        public string TwoFactorCode { get; set; } = string.Empty;

        public bool HasNonEmptyValues()
        {
            return !string.IsNullOrWhiteSpace(Email)
                && !string.IsNullOrWhiteSpace(MasterPassword)
                && !string.IsNullOrWhiteSpace(TwoFactorCode)
                && TwoFactorMethod != TwoFactorMethod.None;
        }
    }

    internal class ApiKeyCredential
    {
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string MasterPassword { get; set; } = string.Empty;

        public bool HasNonEmptyValues()
        {
            return !string.IsNullOrWhiteSpace(ClientId)
                && !string.IsNullOrWhiteSpace(MasterPassword)
                && !string.IsNullOrWhiteSpace(ClientSecret);
        }
    }
}
