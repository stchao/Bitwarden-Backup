using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bitwarden_Backup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .ConfigureLogAndServices()
                .BuildServiceProvider();

            Console.WriteLine("Hello, World!");
        }
    }
}
