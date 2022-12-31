using System;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MovieRecBot
{
    public class Program
    {
        private DiscordSocketClient _client = null!;

        // Program entry point
        public static Task Main()
        {
            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            var config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).Build();

            using IHost host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
            {
                services.AddSingleton(new ConfigurationBuilder())
                .AddSingleton(__ => new DiscordSocketClient(new DiscordSocketConfig
                {// Add the DiscordSocketClient, along with specifying the GatewayIntents and user caching
                    GatewayIntents = GatewayIntents.AllUnprivileged,
                    LogGatewayIntentWarnings = false,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Warning
                }))
                // Used for slash commands and their registration with Discord
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                // Required to subscribe to the various client events used in conjunction with Interactions
                .AddSingleton<InteractionHandler>();
            })
            .Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var commands = provider.GetRequiredService<InteractionService>();
            _client = provider.GetRequiredService<DiscordSocketClient>();
            var config = provider.GetRequiredService<IConfigurationRoot>();

            await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

            // Subscribe to client log events
            _client.Log += (LogMessage msg) => {
                Console.WriteLine(msg.Message);
                return Task.CompletedTask;
            };

            _client.Ready += async () => await commands.RegisterCommandsGloballyAsync(true);

            var token = File.ReadAllText("token.txt");
            await _client.LoginAsync(TokenType.Bot, token);
            Console.WriteLine("Logged In");
            await _client.StartAsync();
            Console.WriteLine("Started");

            await Task.Delay(-1);
        }
    }
}