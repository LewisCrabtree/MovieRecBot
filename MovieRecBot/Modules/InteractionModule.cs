using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using TMDbLib.Client;
using TMDbLib.Objects;
using TMDbLib.Utilities;
using System.Net.Http;

namespace MovieRecBot.Modules
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; } = null!;

        #region register

        [SlashCommand("register", "Enter Letterboxd user ID to register watchlist and ratings with this discord account.")]
        public async Task HandleRegisterCommand([Summary(description: "Letterboxd user ID")] string userID)
        {
            Console.WriteLine(userID);
        }

        #endregion

        #region recommend

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

        [ComponentInteraction("mnuGenre")]
        public async Task HandleMenuSelection()
        {
            var privateKey = File.ReadAllText("tmdbKey.txt");
            TMDbClient client = new(privateKey);
            var query = await client.SearchMovieAsync("The Whale");
            var result = query.Results.FirstOrDefault();
            if (result == null)
                return;

            var movie = await client.GetMovieAsync(result.Id);

            var embed = new EmbedBuilder()
                .WithTitle($"{movie.Title}")
                .WithFooter(footer => footer.Text = "Suggested by MovieRecBot")
                .WithColor(Color.Blue)
                .WithDescription(movie.Overview)
                .WithUrl($"https://www.themoviedb.org/movie/{movie.Id}")
                .WithCurrentTimestamp()
                .WithImageUrl($"https://www.themoviedb.org/t/p/w600_and_h900_bestv2/{movie.PosterPath}")
                .Build();

            await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x =>
            {
                x.Content = string.Empty;
                x.Embeds = new Embed[] { embed };
            });
        }

        #endregion
    }
}
