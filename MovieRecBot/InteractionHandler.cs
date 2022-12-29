using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MovieRecBot.Modules;
using System.Reflection;

namespace MovieRecBot
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
            _client.SelectMenuExecuted += HandleMenuSelection;
        }

        private async Task HandleInteraction(SocketInteraction socketInteraction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, socketInteraction);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (socketInteraction.Type == InteractionType.ApplicationCommand)
                    await socketInteraction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        public static async Task HandleMenuSelection(SocketMessageComponent arg)
        {
            await arg.UpdateAsync(x =>
            {
                x.Content = "\nMinions: The Rise of Gru";
            });
        }
    }
}
