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
        private readonly Credential credential =
            configuration.GetSection(Credential.Key).Get<Credential>() ?? new Credential();
        private readonly bool isInteractive = configuration.GetValue<bool>("IsInteractive");

        // Prompt constants
        private const string SkipPrompt =
            "Type 'skip' or press enter to skip and use the default value";
        private const string LoginChoicePrompt =
            "How would you like to log in? Enter 1 or 'API' to use api, or enter 2 or 'EMAIL' to use email and password.";
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
                "Choice: ",
                false,
                "2",
                ["1", "api", "2", "email"]
            );

            credential.Url = GetStringValueUsingConsole(
                credential.Url,
                BitwardenServerPrompt,
                "Url: ",
                false,
                "https://vault.bitwarden.com"
            );

            switch (loginChoice?.Trim().ToLower())
            {
                // API
                case "1":
                case "api":
                    credential.ClientId = GetStringValueUsingConsole(
                        credential.ClientId,
                        ClientIdPrompt,
                        "Client Id: "
                    );

                    credential.ClientSecret = GetStringValueUsingConsole(
                        credential.ClientSecret,
                        ClientSecretPrompt,
                        "Client Secret: "
                    );

                    credential.Password = GetStringValueUsingConsole(
                        credential.Password,
                        ClientPasswordPrompt,
                        "Password: ",
                        true
                    );
                    break;
                // Email and Password
                case "2":
                case "email":
                    credential.Email = GetStringValueUsingConsole(
                        credential.Email,
                        EmailPrompt,
                        "Email: "
                    );

                    credential.Password = GetStringValueUsingConsole(
                        credential.Password,
                        PasswordPrompt,
                        "Password: ",
                        true
                    );

                    var (twoFactorMethod, twoFactorCode) = GetTwoFactorCodeUsingConsole(
                        credential.TwoFactorMethod
                    );
                    credential.TwoFactorMethod = twoFactorMethod;
                    credential.TwoFactorCode = twoFactorCode;
                    break;
            }

            credential.SetCredentialType();
            return credential;
        }

        public T? GetValueUsingConsole<T>(
            string prompt,
            string outputLineLabel = "",
            bool hideUserInput = false,
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
                if (!string.IsNullOrEmpty(outputLineLabel))
                {
                    Console.Write(outputLineLabel);
                }

                userResponse = hideUserInput
                    ? GetUserResponseWithoutDisplaying()
                    : Console.ReadLine();

                // Add new line for spacing
                Console.WriteLine();

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
                        && !validValues.Contains(userResponse.Trim().ToLower())
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
                        $"Invalid response. The response must be of type {typeof(T).Name}.\n"
                    );
                    result = defaultValue;
                    userResponse = string.Empty;
                }
            }

            return result;
        }

        private string GetStringValueUsingConsole(
            string currentValue,
            string prompt,
            string outputLineLabel = "",
            bool hideUserInput = false,
            string defaultValue = "",
            HashSet<string>? validValues = default
        )
        {
            if (!string.IsNullOrEmpty(currentValue))
            {
                return currentValue;
            }

            return GetValueUsingConsole(
                    prompt,
                    outputLineLabel,
                    hideUserInput,
                    defaultValue,
                    validValues
                ) ?? defaultValue;
        }

        private (
            TwoFactorMethod twoFactorMethod,
            string twoFactorCode
        ) GetTwoFactorCodeUsingConsole(TwoFactorMethod twoFactorMethod)
        {
            var twoFactorChoice =
                twoFactorMethod == TwoFactorMethod.None
                    ? GetValueUsingConsole(
                        TwoFactorPrompt,
                        "Two Factor Choice: ",
                        false,
                        string.Empty,
                        ["1", "auth", "2", "yubi", "3", "email"]
                    ) ?? string.Empty
                    : twoFactorMethod.ToString();

            return (twoFactorChoice?.Trim().ToLower()) switch
            {
                "1"
                or "auth"
                or "authenticator"
                    => (
                        TwoFactorMethod.Authenticator,
                        GetValueUsingConsole<string>(
                            TwoFactorAuthenticatorPrompt,
                            "Authenticator Code: "
                        ) ?? string.Empty
                    ),
                "2"
                or "yubi"
                or "yubikey"
                    => (
                        TwoFactorMethod.YubiKey,
                        GetValueUsingConsole<string>(TwoFactorYubiKeyPrompt, "Yubi Key Code: ")
                            ?? string.Empty
                    ),
                "3" or "email" => (TwoFactorMethod.Email, string.Empty),
                _ => throw new NotSupportedException(),
            };
        }

        private static string GetUserResponseWithoutDisplaying()
        {
            var result = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }

                result.Append(key.KeyChar);
            }

            // Add new line for spacing
            Console.WriteLine();

            return result.ToString();
        }
    }

    internal interface ICredentialService
    {
        public Credential GetBitwardenCredential();

        public T? GetValueUsingConsole<T>(
            string prompt,
            string outputLineLabel = "",
            bool hideUserInput = false,
            T? defaultValue = default,
            HashSet<string>? validValues = default
        )
            where T : IConvertible;
    }
}
