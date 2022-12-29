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
                CustomId = "menu",
                Placeholder = "Select a genre:",
            };

            menu.AddOption("Any", "any");
            menu.AddOption("Comedy", "comedy");
            menu.AddOption("Thriller", "thriller");

            var component = new ComponentBuilder();
            component.WithSelectMenu(menu);

            await RespondAsync("Select a genre", ephemeral: true, components: component.Build());
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
