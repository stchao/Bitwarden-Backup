using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bitwarden_Backup.Services
{
    internal class CredentialService(
        ILogger<CredentialService> logger,
        IConfiguration configuration
    ) : ICredentialService
    {
        private Credential credential =
            configuration.GetSection(Credential.Key).Get<Credential>() ?? new Credential();
        private readonly bool isInteractive = configuration.GetValue<bool>("IsInteractive");

        // Prompt constants
        private const string SkipPrompt =
            "Type 'skip' or press enter to skip and use the default value";
        private const string LoginChoicePrompt =
            "How would you like to log in? Enter 1 or 'API' for API, or 2 or 'EMAIL' for Email and Password.";
        private const string BitwardenServerPrompt = "Please enter the Bitwarden server url.";
        private const string ClientIdPrompt = "Please enter your Client ID.";
        private const string ClientSecretPrompt = "Please enter your Client Secret.";
        private const string ClientPasswordPrompt =
            "When unlocking with API, your master password is required. Please enter your password.";
        private const string EmailPrompt = "Please enter your email.";
        private const string PasswordPrompt = "Please enter your password.";
        private const string TwoFactorPrompt =
            "Is two factor enabled? Enter 1 or 'AUTH' for authenticator, 2 or 'YUBI' for Yubikey, or 3 or 'EMAIL' for email.";
        private const string TwoFactorAuthenticatorPrompt =
            "Please enter the 6 digit code from your authenticator app.";
        private const string TwoFactorYubiKeyPrompt =
            "Please insert your YubiKey and then touch its button.";

        public Credential GetBitwardenCredential()
        {
            var hasValidCredential =
                credential.HasValueForUrl()
                && (credential.HasValuesForAPI() || credential.HasValuesForEmailPW());

            if (!isInteractive && !hasValidCredential)
            {
                throw new Exception(
                    "Missing credentials to log in using API, or Email and Password"
                );
            }

            var loginChoice = GetValueUsingConsole(
                LoginChoicePrompt,
                "2",
                ["1", "api", "2", "email"]
            );

            credential.Url = GetStringValueUsingConsole(
                credential.Url,
                BitwardenServerPrompt,
                "https://vault.bitwarden.com"
            );

            switch (loginChoice?.Trim().ToLower())
            {
                // API
                case "1":
                case "api":
                    credential.ClientId = GetStringValueUsingConsole(
                        credential.ClientId,
                        ClientIdPrompt
                    );

                    credential.ClientSecret = GetStringValueUsingConsole(
                        credential.ClientSecret,
                        ClientSecretPrompt
                    );

                    credential.Password = GetStringValueUsingConsole(
                        credential.Password,
                        ClientPasswordPrompt
                    );
                    break;
                // Email and Password
                case "2":
                case "email":
                    credential.Email = GetStringValueUsingConsole(credential.Email, EmailPrompt);

                    credential.Password = GetStringValueUsingConsole(
                        credential.Password,
                        PasswordPrompt
                    );

                    credential.TwoFactorCode = GetTwoFactorCodeUsingConsole();
                    break;
            }

            credential.SetCredentialType();
            return credential;
        }

        public T? GetValueUsingConsole<T>(
            string prompt,
            T? defaultValue = default,
            HashSet<string>? validValues = default
        )
            where T : IConvertible
        {
            var tempSkipPrompt = $"{SkipPrompt} '{defaultValue}'.";
            var userResponse = string.Empty;
            T? result = defaultValue;

            Console.WriteLine(prompt);
            Console.WriteLine(tempSkipPrompt);

            while (string.IsNullOrWhiteSpace(userResponse))
            {
                userResponse = Console.ReadLine();

                try
                {
                    if (userResponse is null)
                    {
                        continue;
                    }

                    if (
                        userResponse == string.Empty
                        || userResponse.Equals("skip", StringComparison.CurrentCultureIgnoreCase)
                    )
                    {
                        return defaultValue;
                    }

                    if (
                        validValues?.Count > 0
                        && validValues.Contains(userResponse.Trim().ToLower())
                    )
                    {
                        throw new Exception("Invalid user response.");
                    }

                    result = (T)Convert.ChangeType(userResponse, typeof(T));
                }
                catch
                {
                    // Failed to convert. Reprompt user.
                    Console.WriteLine(
                        $"Invalid response. The response must be of type {typeof(T).Name}."
                    );
                    Console.WriteLine(tempSkipPrompt);
                    result = defaultValue;
                    userResponse = string.Empty;
                }
            }

            return result;
        }

        private string GetStringValueUsingConsole(
            string currentValue,
            string prompt,
            string defaultValue = "",
            HashSet<string>? validValues = default
        )
        {
            if (!string.IsNullOrEmpty(currentValue))
            {
                return currentValue;
            }

            return GetValueUsingConsole(prompt, defaultValue, validValues) ?? defaultValue;
        }

        private string GetTwoFactorCodeUsingConsole()
        {
            var twoFactorChoice =
                GetValueUsingConsole(
                    TwoFactorPrompt,
                    string.Empty,
                    ["1", "auth", "2", "yubi", "3", "email"]
                ) ?? string.Empty;

            return (twoFactorChoice?.Trim().ToLower()) switch
            {
                "1"
                or "auth"
                    => GetValueUsingConsole(TwoFactorAuthenticatorPrompt, string.Empty)
                        ?? string.Empty,
                "2"
                or "yubi"
                    => GetValueUsingConsole(TwoFactorYubiKeyPrompt, string.Empty) ?? string.Empty,
                "3" or "email" => "email",
                _ => throw new NotSupportedException(),
            };
        }
    }

    internal interface ICredentialService
    {
        public Credential GetBitwardenCredential();

        public T? GetValueUsingConsole<T>(
            string prompt,
            T? defaultValue = default,
            HashSet<string>? validValues = default
        )
            where T : IConvertible;
    }
}
