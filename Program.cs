using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System;
using System.Configuration;
using System.Threading.Tasks;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace TestConsoleApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration _configuration = null;
            IConfigurationRefresher _refresher = null;
            var RefreshInterval = TimeSpan.FromSeconds(Double.Parse(ConfigurationManager.AppSettings["RefreshInterval"]));
            var builder = new ConfigurationBuilder()
                .AddAzureAppConfiguration(options =>
                {
                    options
                           .Connect(ConfigurationManager.AppSettings["AzureApplicationConnectionString"])
                           .ConfigureRefresh(refresh =>
                           {
                               refresh
                               .Register("TestApp:Settings:Message")
                               .SetCacheExpiration(RefreshInterval);
                           })
                           .UseFeatureFlags(refresh =>
                           {
                               refresh.CacheExpirationTime = RefreshInterval;
                           });
                    _refresher = options.GetRefresher();
                });

            _configuration = builder.Build();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(_configuration).AddFeatureManagement();
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();
                if (await featureManager.IsEnabledAsync("Beta"))
                {
                    Console.WriteLine("Welcome to the beta!");
                }
            }
            PrintConfig(_configuration);
            await _refresher.RefreshAsync();
            PrintConfig(_configuration);
        }

        private static void PrintConfig(IConfiguration configuration)
        {
            Console.WriteLine("_________________________________________________________________");
            foreach (var configItem in configuration.AsEnumerable())
            {
                Console.WriteLine($"Key {configItem.Key} => Value {configItem.Value}");
            }
        }
    }
}
