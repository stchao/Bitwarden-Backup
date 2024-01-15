using System.Net;

namespace Bitwarden_Backup.Models
{
    internal class Credential
    {
        public static string Key = "Credential";

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;

        public string TwoFactorCode { get; set; } = string.Empty;

        public CredentialType CredentialType { get; set; }

        public bool HasValueForUrl() => !string.IsNullOrEmpty(Url);

        public bool HasValueForTwoFactor() => !string.IsNullOrEmpty(TwoFactorCode);

        public bool HasValuesForAPI() =>
            !string.IsNullOrEmpty(ClientId)
            && !string.IsNullOrEmpty(ClientSecret)
            && !string.IsNullOrEmpty(Password);

        public bool HasValuesForEmailPW() =>
            !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password);

        internal void SetCredentialType()
        {
            if (HasValuesForAPI())
            {
                CredentialType = CredentialType.Api;
            }
            else if (HasValuesForEmailPW())
            {
                CredentialType = CredentialType.EmailPw;
            }
            else
            {
                CredentialType = CredentialType.None;
            }
        }
    }
}
