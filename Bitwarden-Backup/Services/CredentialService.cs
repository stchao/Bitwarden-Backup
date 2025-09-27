using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Bitwarden_Backup.Services
{
    public class CredentialService(ILogger<CredentialService> logger, IConfiguration configuration)
        : ICredentialService
    {
        private readonly BitwardenCredentials _bitwardenCredentials =
            configuration.GetSection(BitwardenCredentials.Key).Get<BitwardenCredentials>()
            ?? new BitwardenCredentials();

        public async Task<BitwardenCredentials> GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration,
            CancellationToken cancellationToken = default
        )
        {
            var hasRequiredValues =
                (
                    _bitwardenCredentials.ApiKeyCredential is not null
                    && _bitwardenCredentials.ApiKeyCredential.HasRequiredValues()
                )
                || (
                    _bitwardenCredentials.EmailPasswordCredential is not null
                    && _bitwardenCredentials.EmailPasswordCredential.HasRequiredValues()
                );

            if (!bitwardenConfiguration.EnableInteractiveLogIn && !hasRequiredValues)
            {
                throw new Exception(ErrorMessages.NoCredentials);
            }

            var bitwardenCredentials = new BitwardenCredentials();

            switch (bitwardenConfiguration.LogInMethod)
            {
                case LogInMethod.ApiKey:
                    bitwardenCredentials.ApiKeyCredential = await GetApiKeyCredentials(
                        cancellationToken
                    );
                    break;
                case LogInMethod.EmailPw:
                    bitwardenCredentials.EmailPasswordCredential =
                        await GetEmailPasswordCredentials(cancellationToken);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.InvalidLogInMethod);
            }

            return bitwardenCredentials;
        }

        private async Task<ApiKeyCredential> GetApiKeyCredentials(
            CancellationToken cancellationToken = default
        )
        {
            _bitwardenCredentials.ApiKeyCredential ??= new ApiKeyCredential();

            var apiKeyCredential = new ApiKeyCredential
            {
                ClientId = _bitwardenCredentials.ApiKeyCredential.ClientId,
                ClientSecret = _bitwardenCredentials.ApiKeyCredential.ClientSecret,
                MasterPassword = _bitwardenCredentials.ApiKeyCredential.MasterPassword
            };

            logger.LogDebug("Getting client id for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientId = await SpectreConsoleExtension.GetStringInputWithConsole(
                apiKeyCredential.ClientId,
                Prompts.ClientId,
                SpectreConsoleExtension.DefaultStringValidator,
                new ValidatorParams
                {
                    ValidationResultErrorMessage = ErrorMessages.ClientIdValidationResult
                },
                false,
                null,
                cancellationToken
            );

            logger.LogDebug("Getting client secret for api key credentials using Spectre.Console.");
            apiKeyCredential.ClientSecret = await SpectreConsoleExtension.GetStringInputWithConsole(
                apiKeyCredential.ClientSecret,
                Prompts.ClientSecret,
                SpectreConsoleExtension.DefaultStringValidator,
                new ValidatorParams
                {
                    ValidationResultErrorMessage = ErrorMessages.ClientSecretValidationResult
                },
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
                    new ValidatorParams
                    {
                        ValidationResultErrorMessage = ErrorMessages.MasterPasswordValidationResult,
                        MinLength = 12
                    },
                    true,
                    null,
                    cancellationToken
                );

            return apiKeyCredential;
        }

        private async Task<EmailPasswordCredential> GetEmailPasswordCredentials(
            CancellationToken cancellationToken
        )
        {
            _bitwardenCredentials.EmailPasswordCredential ??= new EmailPasswordCredential();

            var emailPasswordCredential = new EmailPasswordCredential()
            {
                Email = _bitwardenCredentials.EmailPasswordCredential.Email,
                MasterPassword = _bitwardenCredentials.EmailPasswordCredential.MasterPassword,
                TwoFactorMethod = _bitwardenCredentials.EmailPasswordCredential.TwoFactorMethod,
                TwoFactorCode = _bitwardenCredentials.EmailPasswordCredential.TwoFactorCode,
                ClientSecret = _bitwardenCredentials.EmailPasswordCredential.ClientSecret,
                UserTwoFactorMethod = _bitwardenCredentials
                    .EmailPasswordCredential
                    .UserTwoFactorMethod
            };

            logger.LogDebug(
                "Getting email address for email password credentials using Spectre.Console."
            );
            emailPasswordCredential.Email = await SpectreConsoleExtension.GetStringInputWithConsole(
                emailPasswordCredential.Email,
                Prompts.Email,
                SpectreConsoleExtension.EmailStringValidator,
                new ValidatorParams
                {
                    ValidationResultErrorMessage = ErrorMessages.EmailValidationResult,
                    MinLength = 12
                },
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
                    new ValidatorParams
                    {
                        ValidationResultErrorMessage = ErrorMessages.MasterPasswordValidationResult,
                        MinLength = 12
                    },
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
                        new ValidatorParams
                        {
                            ValidationResultErrorMessage =
                                ErrorMessages.TwoFactorCodeValidationResult
                        },
                        false,
                        null,
                        cancellationToken
                    );
            }

            return emailPasswordCredential;
        }
    }

    public interface ICredentialService
    {
        public Task<BitwardenCredentials> GetBitwardenCredential(
            BitwardenConfiguration bitwardenConfiguration,
            CancellationToken cancellationToken = default
        );
    }
}
