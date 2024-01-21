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

        public BitwardenCredentials GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration
        )
        {
            var hasValidCredential =
                bitwardenCredentials.ApiKeyCredential is not null
                || bitwardenCredentials.EmailPasswordCredential is not null;

            if (!bitwardenConfiguration.EnableInteractiveLogIn && !hasValidCredential)
            {
                throw new Exception(ErrorMessages.NoCredentials);
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
                    throw new NotImplementedException(ErrorMessages.InvalidLogInMethod);
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

        private void GetApiKeyCredentials()
        {
            bitwardenCredentials.ApiKeyCredential ??= new ApiKeyCredential();

            var apiKeyCredential = bitwardenCredentials.ApiKeyCredential;

            apiKeyCredential.ClientId.GetUserInputAsStringUsingConsole(
                Prompts.ClientId,
                ErrorMessages.ClientIdValidationResult
            );

            apiKeyCredential.ClientSecret.GetUserInputAsStringUsingConsole(
                Prompts.ClientSecret,
                ErrorMessages.ClientSecretValidationResult
            );

            apiKeyCredential.MasterPassword.GetUserInputAsStringUsingConsole(
                Prompts.MasterPassword,
                ErrorMessages.MasterPasswordValidationResult,
                true
            );
        }

        private void GetEmailPasswordCredentials()
        {
            bitwardenCredentials.EmailPasswordCredential ??= new EmailPasswordCredential();

            var emailPasswordCredential = bitwardenCredentials.EmailPasswordCredential;

            emailPasswordCredential.Email.GetUserInputAsStringUsingConsole(
                Prompts.Email,
                ErrorMessages.EmailValidationResult
            );

            emailPasswordCredential.MasterPassword.GetUserInputAsStringUsingConsole(
                Prompts.MasterPassword,
                ErrorMessages.MasterPasswordValidationResult,
                true
            );

            if (emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.None)
            {
                emailPasswordCredential.TwoFactorMethod = emailPasswordCredential.TwoFactorMethod =
                    AnsiConsole.Prompt(
                        new SelectionPrompt<TwoFactorMethod>()
                            .Title(Prompts.TwoFactorMethod)
                            .PageSize(5)
                            .MoreChoicesText(Texts.MoreChoices)
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
                                                ErrorMessages.InvalidTwoFactorMethod
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
                    Prompts.TwoFactorCode,
                    ErrorMessages.TwoFactorCodeValidationResult
                );
            }
        }
    }

    internal interface ICredentialService
    {
        public BitwardenCredentials GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration
        );
    }
}
