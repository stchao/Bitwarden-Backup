using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Bitwarden_Backup.Services
{
    internal class CredentialService(IConfiguration configuration) : ICredentialService
    {
        private readonly BitwardenCredentials bitwardenCredentials =
            configuration.GetSection(BitwardenCredentials.Key).Get<BitwardenCredentials>()
            ?? new BitwardenCredentials();
        private readonly BitwardenConfiguration bitwardenConfiguration =
            configuration.GetSection(BitwardenConfiguration.Key).Get<BitwardenConfiguration>()
            ?? new BitwardenConfiguration();

        // Prompts
        private const string LoginMethodPrompt = "How would you like to log in? ";
        private const string TwoFactorMethodPrompt = "How would you like to log in? ";
        private const string ClientIdPrompt = "? Client Id: ";
        private const string ClientSecretPrompt = "? Client Secret: ";
        private const string MasterPasswordPrompt = "? Master Password: [input is hidden] ";
        private const string EmailPrompt = "? Email address: ";
        private const string TwoFactorCodePrompt = "? Two-step login code: ";
        private const string MoreChoicesText = "[grey](Move up and down to reveal more choices)[/]";

        // Error Messages
        private const string NoCredentialsErrorMessage =
            "Interactive log in is disabled and there are no credential(s) in appsettings.json.";
        private const string InvalidLogInMethodErrorMessage =
            "The log in methods currently supported are using api key or using email and password credentials.";
        private const string InvalidTwoFactorMethodErrorMessage =
            "The two factor methods currently supported are using authenticator app, YubiKey OTP security key, or email.";
        private const string ClientIdValidationResultErrorMessage =
            "[red]Client Id cannot be empty or null.[/]";
        private const string ClientSecretValidationResultErrorMessage =
            "[red]Client Secret cannot be empty or null.[/]";
        private const string MasterPasswordValidationResultErrorMessage =
            "[red]Client Id cannot be empty or null.[/]";
        private const string EmailValidationResultErrorMessage =
            "[red]Email Address cannot be empty or null.[/]";
        private const string TwoFactorCodeValidationResultErrorMessage =
            "[red]Email Address cannot be empty or null.[/]";

        public BitwardenCredentials GetBitwardenCredential()
        {
            var hasValidCredential =
                bitwardenCredentials.ApiKeyCredential is not null
                || bitwardenCredentials.EmailPasswordCredential is not null;

            if (!bitwardenConfiguration.EnableInteractiveLogIn && !hasValidCredential)
            {
                throw new Exception(NoCredentialsErrorMessage);
            }

            GetBitwardenConfiguration();

            if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
            {
                // Implement cancel
            }

            switch (bitwardenConfiguration.LogInMethod)
            {
                case LogInMethod.ApiKey:
                    GetApiKeyCredentials();
                    break;
                case LogInMethod.EmailPw:
                    GetEmailPasswordCredentials();
                    break;
                default:
                    throw new NotImplementedException(InvalidLogInMethodErrorMessage);
            }

            if (
                bitwardenCredentials.EmailPasswordCredential?.TwoFactorMethod
                == TwoFactorMethod.Cancel
            )
            {
                // Implement cancel
            }

            return bitwardenCredentials;
        }

        public BitwardenConfiguration GetBitwardenConfiguration()
        {
            // Set log in method to avoid reprompt if set in appsettings or enough credentials were provided
            bitwardenConfiguration.SetLogInMethod(bitwardenCredentials);

            if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
            {
                bitwardenConfiguration.LogInMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<LogInMethod>()
                        .Title(LoginMethodPrompt)
                        .PageSize(5)
                        .MoreChoicesText(MoreChoicesText)
                        .AddChoices([LogInMethod.ApiKey, LogInMethod.EmailPw, LogInMethod.None])
                        .UseConverter(
                            logInMethod =>
                                logInMethod switch
                                {
                                    LogInMethod.ApiKey => "Using Api Key",
                                    LogInMethod.EmailPw => "Using Email and Password",
                                    LogInMethod.None => "Cancel",
                                    _
                                        => throw new NotImplementedException(
                                            InvalidLogInMethodErrorMessage
                                        )
                                }
                        )
                );
            }

            return bitwardenConfiguration;
        }

        private void GetApiKeyCredentials()
        {
            bitwardenCredentials.ApiKeyCredential ??= new ApiKeyCredential();

            var apiKeyCredential = bitwardenCredentials.ApiKeyCredential;

            apiKeyCredential.ClientId.GetUserInputAsStringUsingConsole(
                ClientIdPrompt,
                ClientIdValidationResultErrorMessage
            );

            apiKeyCredential.ClientSecret.GetUserInputAsStringUsingConsole(
                ClientSecretPrompt,
                ClientSecretValidationResultErrorMessage
            );

            apiKeyCredential.MasterPassword.GetUserInputAsStringUsingConsole(
                MasterPasswordPrompt,
                MasterPasswordValidationResultErrorMessage,
                true
            );
        }

        private void GetEmailPasswordCredentials()
        {
            bitwardenCredentials.EmailPasswordCredential ??= new EmailPasswordCredential();

            var emailPasswordCredential = bitwardenCredentials.EmailPasswordCredential;

            emailPasswordCredential.Email.GetUserInputAsStringUsingConsole(
                EmailPrompt,
                EmailValidationResultErrorMessage
            );

            emailPasswordCredential.MasterPassword.GetUserInputAsStringUsingConsole(
                MasterPasswordPrompt,
                MasterPasswordValidationResultErrorMessage,
                true
            );

            if (emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.None)
            {
                emailPasswordCredential.TwoFactorMethod = emailPasswordCredential.TwoFactorMethod =
                    AnsiConsole.Prompt(
                        new SelectionPrompt<TwoFactorMethod>()
                            .Title(TwoFactorMethodPrompt)
                            .PageSize(5)
                            .MoreChoicesText(MoreChoicesText)
                            .AddChoices(
                                [
                                    TwoFactorMethod.Authenticator,
                                    TwoFactorMethod.Email,
                                    TwoFactorMethod.YubiKey,
                                    TwoFactorMethod.None,
                                    TwoFactorMethod.Cancel,
                                ]
                            )
                            .UseConverter(
                                twoFactorMethod =>
                                    twoFactorMethod switch
                                    {
                                        TwoFactorMethod.Authenticator => "Authenticator App",
                                        TwoFactorMethod.YubiKey => "YubiKey OTP Security Key",
                                        TwoFactorMethod.Email => "Email",
                                        TwoFactorMethod.None => "None",
                                        TwoFactorMethod.Cancel => "Cancel",
                                        _
                                            => throw new NotImplementedException(
                                                InvalidTwoFactorMethodErrorMessage
                                            )
                                    }
                            )
                    );
            }

            if (
                emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.Authenticator
                || emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.YubiKey
            )
            {
                emailPasswordCredential.TwoFactorCode.GetUserInputAsStringUsingConsole(
                    TwoFactorCodePrompt,
                    TwoFactorCodeValidationResultErrorMessage
                );
            }
        }
    }

    internal interface ICredentialService
    {
        public BitwardenCredentials GetBitwardenCredential();
    }
}
