using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitwarden_Backup.Models
{
    internal class BitwardenConfiguration
    {
        public static string Key = "BitwardenConfiguration";

        public string Url { get; set; } = string.Empty;

        public LogInMethod LogInMethod { get; set; } = LogInMethod.None;

        public bool EnableInteractiveLogIn { get; set; } = true;

        public string ExecutablePath { get; set; } = string.Empty;

        public void SetLogInMethod(BitwardenCredentials bitwardenCredentials)
        {
            if (bitwardenCredentials.ApiKeyCredential is not null)
            {
                LogInMethod = LogInMethod.ApiKey;
                return;
            }

            if (bitwardenCredentials.EmailPasswordCredential is not null)
            {
                LogInMethod = LogInMethod.EmailPw;
                return;
            }
        }
    }
}
