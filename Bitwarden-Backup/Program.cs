using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Bitwarden_Backup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bitwarden_Backup
{
    public class Program
    {
        static async Task Main()
        {
            CancellationTokenSource cts = new();
            IBitwardenService? bitwardenService = null;
            ILogger<Program>? logger = null;

            try
            {
                ServiceProvider? serviceProvider = null;
                ICredentialService? credentialService = null;
                BitwardenConfiguration? bitwardenConfiguration = null;

                Console.CancelKeyPress += (sender, args) =>
                {
                    cts.Cancel();
                    args.Cancel = true;
                };

                serviceProvider = new ServiceCollection()
                    .ConfigureLogAndServices()
                    .BuildServiceProvider();

                logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                bitwardenService = serviceProvider.GetRequiredService<IBitwardenService>();
                logger.LogInformation("Got required service(s).");

                if (logger is null || bitwardenService is null)
                {
                    return;
                }

                bitwardenConfiguration = await bitwardenService.GetBitwardenConfiguration(
                    cts.Token
                );

                if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
                {
                    logger.LogInformation("Cancelled export of Bitwarden vault.");
                    return;
                }

                logger.LogInformation("Got Bitwarden configuration.");

                var bitwardenSetBitwardenServerResponse = await bitwardenService.SetBitwardenServer(
                    cts.Token
                );

                if (!bitwardenSetBitwardenServerResponse.Success)
                {
                    logger.LogError(
                        "Failed to set Bitwarden CLI's server. \nResponse: {@bitwardenResponse}",
                        bitwardenSetBitwardenServerResponse
                    );
                    return;
                }

                var attempts = 1;
                var validResponses = new HashSet<string>() { "Y", "N" };

                do
                {
                    logger.LogDebug("Getting credential(s).");
                    var bitwardenCredentials = await credentialService.GetBitwardenCredential(
                        bitwardenConfiguration,
                        cts.Token
                    );
                    logger.LogInformation("Got credential(s).");

                    logger.LogDebug("Logging in to Bitwarden vault.");
                    var bitwardenLogInResponse = bitwardenConfiguration.LogInMethod switch
                    {
                        LogInMethod.ApiKey
                            => await bitwardenService.LogIn(
                                bitwardenCredentials.ApiKeyCredential!,
                                cts.Token
                            ),
                        LogInMethod.EmailPw
                            => await bitwardenService.LogIn(
                                bitwardenCredentials.EmailPasswordCredential!,
                                cts.Token
                            ),
                        _
                            => new BitwardenResponse()
                            {
                                Success = false,
                                Message = ErrorMessages.InvalidLogInMethod
                            }
                    };

                    if (!bitwardenLogInResponse.Success)
                    {
                        logger.LogError(
                            "Failed to log in to Bitwarden. \nResponse: {@bitwardenResponse}",
                            bitwardenLogInResponse
                        );

                        Console.WriteLine(bitwardenLogInResponse.Message);

                        var continueResponse =
                            attempts > 2
                                ? "N"
                                : await SpectreConsoleExtension.GetStringInputWithConsole(
                                    string.Empty,
                                    Prompts.Retry,
                                    SpectreConsoleExtension.StringInHashValidator,
                                    new ValidatorParams
                                    {
                                        ValidationResultErrorMessage =
                                            ErrorMessages.YNValidationResult,
                                        ValidArgsHash = validResponses
                                    },
                                    false,
                                    null,
                                    cts.Token
                                );

                        if (continueResponse == "N")
                        {
                            return;
                        }

                        continue;
                    }

                    attempts = 4;
                    logger.LogInformation("Logged in to Bitwarden vault.");
                    logger.LogDebug(
                        "Bitwarden log in response: \n{@bitwardenResponse}",
                        bitwardenLogInResponse
                    );
                } while (attempts++ < 3);

                logger.LogDebug("Exporting Bitwarden vault.");

                var bitwardenExportResponse = await bitwardenService.ExportVault(cts.Token);

                if (!bitwardenExportResponse.Success)
                {
                    logger.LogError(
                        "Failed to export Bitwarden Vault. \nResponse: {@bitwardenResponse}",
                        bitwardenExportResponse
                    );
                }

                logger.LogInformation(
                    "Exported Bitwarden vault to '{path}'",
                    bitwardenExportResponse.Data?.Raw
                );
                logger.LogDebug(
                    "Bitwarden export response: \n{@bitwardenResponse}",
                    bitwardenExportResponse
                );
            }
            catch (TaskCanceledException)
            {
                logger?.LogInformation("Cancelled export of Bitwarden vault.");
            }
            catch (InvalidOperationException ex)
            {
                logger?.LogError(ex, "Failed to get the required service(s).");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to export Bitwarden vault.");
            }
            finally
            {
                cts.Dispose();
                await (bitwardenService?.LogOut() ?? Task.CompletedTask);
                logger?.LogInformation("Logged out and exiting program.\n");
            }
        }
    }
}
