using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShoeStore.Identity.IdentityServer
{
    public class Program
    {
        private static string _environment;

        public static void Main(string[] args)
        {
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (_environment != Environments.Development)
                    {
                        var builtConfig = config.Build();
                        config.AddAzureKeyVault(
                                            $"https://{builtConfig["KeyVault:Vault"]}.vault.azure.net/",
                                            builtConfig["KeyVault:ClientId"],
                                            builtConfig["KeyVault:ClientSecret"],
                                            new DefaultKeyVaultSecretManager()
                                          );
                    }
                })
                .UseStartup<Startup>(); 
    }
}
