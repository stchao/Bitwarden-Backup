using System.Text;
using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Bitwarden_Backup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                bitwardenService = serviceProvider.GetRequiredService<IBitwardenService>();

                var bitwardenConfiguration = bitwardenService.GetBitwardenConfiguration();
                var bitwardenCredentials = credentialService.GetBitwardenCredential(
                    bitwardenConfiguration
                );

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

                var bitwardenExportResponse = bitwardenService.ExportVault();

                if (!bitwardenExportResponse.Success)
                {
                    logger.LogError(
                        "Failed to export Bitwarden Vault. \nResponse: {@bitwardenResponse}",
                        bitwardenExportResponse
                    );
                }
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
