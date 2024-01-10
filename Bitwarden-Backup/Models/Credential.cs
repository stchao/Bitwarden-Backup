
namespace Bitwarden_Backup.Models
{
    internal class Credential
    {
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public int OneTimePasscode { get; set; } = -1;

        public bool HasValueForUrl() => !string.IsNullOrEmpty(Url);

        public bool HasValuesForAPI() => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(Password);

        public bool HasValuesForEmailPW() => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password);

        public bool HasValueForOTP() => OneTimePasscode > -1 && HasValuesForEmailPW();
    }
}
