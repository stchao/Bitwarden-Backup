using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Bitwarden_Backup.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection ConfigureLogAndServices(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()             
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            services.AddSingleton<IConfiguration>(configuration)
                .AddLogging(configure => configure.AddSerilog());

            return services;
        }
    }
}
