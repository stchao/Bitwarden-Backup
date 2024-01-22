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
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .ConfigureLogAndServices()
                .BuildServiceProvider();

            IBitwardenService? bitwardenService = null;
            var logger = serviceProvider.GetService<ILogger<Program>>();

            if (logger is null)
            {
                Console.WriteLine("Failed to get and/or initialize the logger.");
                return;
            }

            try
            {
                logger.LogDebug("Getting required service(s).");
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                bitwardenService = serviceProvider.GetRequiredService<IBitwardenService>();

                logger.LogDebug("Getting bitwarden configuration.");
                var bitwardenConfiguration = bitwardenService.GetBitwardenConfiguration();
                logger.LogInformation("Got bitwarden configuration.");

                logger.LogDebug("Getting bitwarden credentials.");
                var bitwardenCredentials = credentialService.GetBitwardenCredential(
                    bitwardenConfiguration
                );
                logger.LogInformation("Got bitwarden credentials.");

                logger.LogDebug("Logging in to Bitwarden vault.");
                var bitwardenLogInResponse = bitwardenConfiguration.LogInMethod switch
                {
                    LogInMethod.ApiKey
                        => bitwardenService.LogIn(bitwardenCredentials.ApiKeyCredential!),
                    LogInMethod.EmailPw
                        => bitwardenService.LogIn(bitwardenCredentials.EmailPasswordCredential!),
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

                logger.LogInformation(
                    "Logged in to Bitwarden vault with response: \n{@bitwardenResponse}",
                    bitwardenLogInResponse
                );
                logger.LogDebug("Exporting Bitwarden vault.");

                var bitwardenExportResponse = bitwardenService.ExportVault();

                if (!bitwardenExportResponse.Success)
                {
                    logger.LogError(
                        "Failed to export Bitwarden Vault. \nResponse: {@bitwardenResponse}",
                        bitwardenExportResponse
                    );
                }

                logger.LogInformation(
                    "Exported Bitwarden vault with response: \n{@bitwardenResponse}",
                    bitwardenExportResponse
                );
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
                bitwardenService?.LogOut();
                logger.LogInformation("Exiting program.\n");
            }
        }
    }
}
