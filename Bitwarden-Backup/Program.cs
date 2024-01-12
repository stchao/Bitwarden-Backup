using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Services;
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

            var logger = serviceProvider.GetService<ILogger<Program>>();

            if (logger is null)
            {
                Console.WriteLine("Failed to initialize logger.");
                return;
            }

            try
            {
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                var bitwardenService = serviceProvider.GetRequiredService<IBitwardenService>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get or run service(s).");
            }
        }
    }
}
