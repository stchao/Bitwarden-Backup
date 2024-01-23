using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Bitwarden_Backup.Services
{
    internal class CredentialService(
        ILogger<CredentialService> logger,
        IConfiguration configuration
    ) : ICredentialService
    {
        private readonly BitwardenCredentials bitwardenCredentials =
            configuration.GetSection(BitwardenCredentials.Key).Get<BitwardenCredentials>()
            ?? new BitwardenCredentials();

        public BitwardenCredentials GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration
        )
        {
            var hasValidCredential =
                (
                    bitwardenCredentials.ApiKeyCredential is not null
                    && bitwardenCredentials.ApiKeyCredential.HasNonEmptyValues()
                )
                || (
                    bitwardenCredentials.EmailPasswordCredential is not null
                    && bitwardenCredentials.EmailPasswordCredential.HasNonEmptyValues()
                );

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

            logger.LogDebug("Getting client id for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientId = SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                apiKeyCredential.ClientId,
                Prompts.ClientId,
                ErrorMessages.ClientIdValidationResult
            );

            logger.LogDebug("Getting client secret for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientSecret =
                SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                    apiKeyCredential.ClientSecret,
                    Prompts.ClientSecret,
                    ErrorMessages.ClientSecretValidationResult
                );

            logger.LogDebug(
                "Getting master password for api key credentials using Spectre.Console."
            );
            apiKeyCredential.MasterPassword =
                SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                    apiKeyCredential.MasterPassword,
                    Prompts.MasterPassword,
                    ErrorMessages.MasterPasswordValidationResult,
                    true
                );
        }

        private void GetEmailPasswordCredentials()
        {
            bitwardenCredentials.EmailPasswordCredential ??= new EmailPasswordCredential();

            var emailPasswordCredential = bitwardenCredentials.EmailPasswordCredential;

            logger.LogDebug(
                "Getting email address for email password credentials using Spectre.Console."
            );
            emailPasswordCredential.Email =
                SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                    emailPasswordCredential.Email,
                    Prompts.Email,
                    ErrorMessages.EmailValidationResult
                );

            logger.LogDebug(
                "Getting master password for email password credentials using Spectre.Console."
            );
            emailPasswordCredential.MasterPassword =
                SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                    emailPasswordCredential.MasterPassword,
                    Prompts.MasterPassword,
                    ErrorMessages.MasterPasswordValidationResult,
                    true
                );

            if (emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.None)
            {
                logger.LogDebug(
                    "Getting two factor method for email password credentials using Spectre.Console."
                );
                emailPasswordCredential.TwoFactorMethod = AnsiConsole.Prompt(
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
                logger.LogDebug(
                    "Getting two factor code for email password credentials using Spectre.Console."
                );
                emailPasswordCredential.TwoFactorCode =
                    SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                        emailPasswordCredential.TwoFactorCode,
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
