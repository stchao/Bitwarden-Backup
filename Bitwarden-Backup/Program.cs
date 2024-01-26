using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Bitwarden_Backup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bitwarden_Backup
{
    internal class Program
    {
        static async Task Main()
        {
            var serviceProvider = new ServiceCollection()
                .ConfigureLogAndServices()
                .BuildServiceProvider();
            var cts = new CancellationTokenSource();

            IBitwardenService? bitwardenService = null;
            var logger = serviceProvider.GetService<ILogger<Program>>();

            if (logger is null)
            {
                Console.WriteLine("Failed to get and/or initialize the logger.");
                return;
            }

            try
            {
                Console.CancelKeyPress += (sender, args) =>
                {
                    cts.Cancel();
                    args.Cancel = true;
                };

                logger.LogDebug("Getting required service(s).");
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                bitwardenService = serviceProvider.GetRequiredService<IBitwardenService>();
                logger.LogInformation("Got required service(s).");

                logger.LogDebug("Getting bitwarden configuration.");
                var bitwardenConfiguration = await bitwardenService.GetBitwardenConfiguration(
                    cts.Token
                );

                if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
                {
                    logger.LogInformation("Cancelled export of Bitwarden vault.");
                    return;
                }

                logger.LogInformation("Got bitwarden configuration.");

                logger.LogDebug("Getting bitwarden credentials.");
                var bitwardenCredentials = await credentialService.GetBitwardenCredential(
                    bitwardenConfiguration,
                    cts.Token
                );
                logger.LogInformation("Got bitwarden credentials.");

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
                    return;
                }

                logger.LogInformation("Logged in to Bitwarden vault.");
                logger.LogDebug(
                    "Bitwarden log in response: \n{@bitwardenResponse}",
                    bitwardenLogInResponse
                );
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
                logger.LogInformation("Cancelled export of Bitwarden vault.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to get the required service(s).");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export Bitwarden vault.");
            }
            finally
            {
                cts.Dispose();
                await (bitwardenService?.LogOut() ?? Task.CompletedTask);
                logger.LogInformation("Logged out and exiting program.\n");
            }
        }
    }
}
