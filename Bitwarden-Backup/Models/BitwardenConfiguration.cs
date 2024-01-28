namespace Bitwarden_Backup.Models
{
    internal class BitwardenConfiguration
    {
        public static string Key = "BitwardenConfiguration";

        public string Url { get; set; } = string.Empty;

        public string UserLogInMethod
        {
            get => LogInMethod.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (!Enum.TryParse(value, out LogInMethod tempLogInMethod))
                {
                    throw new FormatException($"{value} is not a valid value for LogInMethod.");
                }

                LogInMethod = tempLogInMethod;
            }
        }

        public LogInMethod LogInMethod { get; set; } = LogInMethod.None;

        public bool EnableInteractiveLogIn { get; set; } = true;

        public string ExecutablePath { get; set; } = string.Empty;

        public void SetLogInMethod(BitwardenCredentials bitwardenCredentials)
        {
            if (
                bitwardenCredentials.ApiKeyCredential is not null
                && bitwardenCredentials.ApiKeyCredential.HasAtLeastOneRequiredValue()
            )
            {
                LogInMethod = LogInMethod.ApiKey;
                return;
            }

            if (
                bitwardenCredentials.EmailPasswordCredential is not null
                && bitwardenCredentials.EmailPasswordCredential.HasAtLeastOneRequiredValue()
            )
            {
                LogInMethod = LogInMethod.EmailPw;
                return;
            }
        }
    }
}
