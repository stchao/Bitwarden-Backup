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

        public async Task<BitwardenCredentials> GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration,
            CancellationToken cancellationToken = default
        )
        {
            var hasRequiredValues =
                (
                    bitwardenCredentials.ApiKeyCredential is not null
                    && bitwardenCredentials.ApiKeyCredential.HasRequiredValues()
                )
                || (
                    bitwardenCredentials.EmailPasswordCredential is not null
                    && bitwardenCredentials.EmailPasswordCredential.HasRequiredValues()
                );

            if (!bitwardenConfiguration.EnableInteractiveLogIn && !hasRequiredValues)
            {
                throw new Exception(ErrorMessages.NoCredentials);
            }

            switch (bitwardenConfiguration.LogInMethod)
            {
                case LogInMethod.ApiKey:
                    await GetApiKeyCredentials(cancellationToken);
                    break;
                case LogInMethod.EmailPw:
                    await GetEmailPasswordCredentials(cancellationToken);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.InvalidLogInMethod);
            }

            return bitwardenCredentials;
        }

        private async Task GetApiKeyCredentials(CancellationToken cancellationToken = default)
        {
            bitwardenCredentials.ApiKeyCredential ??= new ApiKeyCredential();

            var apiKeyCredential = bitwardenCredentials.ApiKeyCredential;

            logger.LogDebug("Getting client id for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientId = await SpectreConsoleExtension.GetStringInputWithConsole(
                apiKeyCredential.ClientId,
                Prompts.ClientId,
                SpectreConsoleExtension.DefaultStringValidator,
                ErrorMessages.ClientIdValidationResult,
                false,
                null,
                cancellationToken
            );

            logger.LogDebug("Getting client secret for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientSecret = await SpectreConsoleExtension.GetStringInputWithConsole(
                apiKeyCredential.ClientSecret,
                Prompts.ClientSecret,
                SpectreConsoleExtension.DefaultStringValidator,
                ErrorMessages.ClientSecretValidationResult,
                false,
                null,
                cancellationToken
            );

            logger.LogDebug(
                "Getting master password for api key credentials using Spectre.Console."
            );
            apiKeyCredential.MasterPassword =
                await SpectreConsoleExtension.GetStringInputWithConsole(
                    apiKeyCredential.MasterPassword,
                    Prompts.MasterPassword,
                    SpectreConsoleExtension.StringLengthValidator,
                    ErrorMessages.MasterPasswordValidationResult,
                    true,
                    null,
                    cancellationToken
                );
        }

        private async Task GetEmailPasswordCredentials(CancellationToken cancellationToken)
        {
            bitwardenCredentials.EmailPasswordCredential ??= new EmailPasswordCredential();

            var emailPasswordCredential = bitwardenCredentials.EmailPasswordCredential;

            logger.LogDebug(
                "Getting email address for email password credentials using Spectre.Console."
            );
            emailPasswordCredential.Email = await SpectreConsoleExtension.GetStringInputWithConsole(
                emailPasswordCredential.Email,
                Prompts.Email,
                SpectreConsoleExtension.EmailStringValidator,
                ErrorMessages.EmailValidationResult,
                false,
                null,
                cancellationToken
            );

            logger.LogDebug(
                "Getting master password for email password credentials using Spectre.Console."
            );
            emailPasswordCredential.MasterPassword =
                await SpectreConsoleExtension.GetStringInputWithConsole(
                    emailPasswordCredential.MasterPassword,
                    Prompts.MasterPassword,
                    SpectreConsoleExtension.StringLengthValidator,
                    ErrorMessages.MasterPasswordValidationResult,
                    true,
                    null,
                    cancellationToken
                );

            if (emailPasswordCredential.TwoFactorMethod == TwoFactorMethod.None)
            {
                logger.LogDebug(
                    "Getting two factor method for email password credentials using Spectre.Console."
                );
                emailPasswordCredential.TwoFactorMethod =
                    await new SelectionPrompt<TwoFactorMethod>()
                        .Title(Prompts.TwoFactorMethod)
                        .PageSize(5)
                        .MoreChoicesText(Texts.MoreChoices)
                        .AddChoices(
                            [
                                TwoFactorMethod.Authenticator,
                                TwoFactorMethod.Email,
                                TwoFactorMethod.YubiKey,
                                TwoFactorMethod.None,
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
                                    _
                                        => throw new NotImplementedException(
                                            ErrorMessages.InvalidTwoFactorMethod
                                        )
                                }
                        )
                        .ShowAsync(AnsiConsole.Console, cancellationToken);
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
                    await SpectreConsoleExtension.GetStringInputWithConsole(
                        emailPasswordCredential.TwoFactorCode,
                        Prompts.TwoFactorCode,
                        SpectreConsoleExtension.DefaultStringValidator,
                        ErrorMessages.TwoFactorCodeValidationResult,
                        false,
                        null,
                        cancellationToken
                    );
            }
        }
    }

    internal interface ICredentialService
    {
        public Task<BitwardenCredentials> GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration,
            CancellationToken cancellationToken = default
        );
    }
}
