using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace MovieRecBot.Modules
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; } = null!;

        [SlashCommand("recping", "Recieve a ping message.")]
        public async Task HandlePingCommand()
        {
            Console.WriteLine("PING!");
            await RespondAsync("PING!", ephemeral: true);
        }

        [SlashCommand("recommend", "Get a movie recomendation.")]
        public async Task HandleRecommendCommand()
        {
            var menu = new SelectMenuBuilder()
            {
                CustomId = "mnuGenre",
                Placeholder = "Select a genre:",
            };

            menu.AddOption("Any", "any");
            menu.AddOption("Comedy", "comedy");
            menu.AddOption("Thriller", "thriller");

            var component = new ComponentBuilder();
            component.WithSelectMenu(menu);

            await RespondAsync("Select a genre", ephemeral: true, components: component.Build());
        }

        [SlashCommand("register", "Enter Letterboxd user ID to register watchlist and ratings with this discord account.")]
        public async Task HandleRegisterCommand([Summary(description: "Letterboxd user ID")] string userID)
        {
            Console.WriteLine(userID);
        }

        [ComponentInteraction("mnuGenre")]
        public async Task HandleMenuSelection()
        {
            await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x =>
            {
                x.Content = "Minions: The Rise of Gru";
            });
        }
    }

    public class Modal : IModal
    {
        public string Title => "Demo Modal";
        [InputLabel("Send a greeting!")]
        [ModalTextInput("greeting_input", TextInputStyle.Short, placeholder: "Be nice..", maxLength: 100)]
        public string Message { get; set; } = null!;
    }
}
