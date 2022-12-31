using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using TMDbLib.Client;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using TMDbLib.Objects.Movies;

namespace MovieRecBot.Modules
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; } = null!;

        [SlashCommand("recommend", "Get a movie recomendation.")]
        public async Task HandleRecommendCommand([Summary(description: "Prompt:")] string prompt)
        {
            await DeferAsync(ephemeral: true);

            (var title, var year) = await GetRecommendationFromGPT(prompt, Models.TextDavinciV3);
            var movie = await GetMovieFromTMDB(title, year);
            var embeds = BuildEmbeds(movie);
            var components = BuildMessageComponent(prompt);

            await FollowupAsync(embeds: embeds, components: components);
        }

        [ComponentInteraction("nextrec:*")]
        public async Task CheapRecHandler(string tag)
        { //Just get the prompt from the button ID and handle it like a new rec command
            string[] parts = tag.Split('~');
            var model = parts[0];
            var prompt = String.Join("", parts[1..]);

            await Context.Interaction.DeferAsync();
            (var title, var year) = await GetRecommendationFromGPT(prompt, Models.TextCurieV1);
            var movie = await GetMovieFromTMDB(title, year);
            var embeds = BuildEmbeds(movie);
            var components = BuildMessageComponent(prompt);
            await Context.Interaction.ModifyOriginalResponseAsync(x => 
            {
                x.Embeds = embeds;
                x.Components = components;
            });
        }

        private MessageComponent BuildMessageComponent(string prompt)
        {
            return new ComponentBuilder()
                .WithButton("Next (Ada $0.0001)", $"nextrec:{Models.TextAdaV1}~{prompt}")
                .WithButton("Next (Babbage $0.0002)", $"nextrec:{Models.TextBabbageV1}~{prompt}")
                .WithButton("Next (Curie $0.001)", $"nextrec:{Models.TextCurieV1}~{prompt}")
                .WithButton("Next (Davinci $0.01)", $"nextrec:{Models.TextDavinciV3}~{prompt}")
                .Build();
        }

        private async Task<Movie> GetMovieFromTMDB(string title, int year)
        {
            var privateKey = File.ReadAllText("tmdbKey.txt");
            TMDbClient client = new(privateKey);
            var query = await client.SearchMovieAsync(title, year: year);

            var result = query.Results.FirstOrDefault();
            if (result == null)
            { //try searching the title only, the year might be wrong from GPT
                query = await client.SearchMovieAsync(title);
                result = query.Results.FirstOrDefault();
            }

            return await client.GetMovieAsync(result?.Id ?? 785084);
        }

        private async Task<(string, int)> GetRecommendationFromGPT(string prompt, string model)
        {
            var apiKey = File.ReadAllText("openAiKey.txt");
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            var completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = $"Recommend a movie. {prompt}. Output format: Title (Year). Example: Movie Title (Year).",
                MaxTokens = 100,
                User = Context.User.Username
            }, model);

            if (!completionResult.Successful)
            {
                Console.WriteLine($"Error:\n {completionResult?.Error?.Code}: {completionResult?.Error?.Message}");
            }

            var result = completionResult?.Choices.FirstOrDefault()?.Text.Trim() ?? "The Whale (2022)";
            Console.WriteLine($"\n\n{result}\n\n");

            return GetTitleYear(result);
        }

        private Embed[] BuildEmbeds(Movie movie)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"{movie.Title} ({movie.ReleaseDate?.Year.ToString() ?? ""})")
                .WithFooter(footer => footer.Text = "Recommended by MovieRecBot")
                .WithColor(Color.Blue)
                .WithDescription($"{movie.Overview}")
                .WithUrl($"https://www.themoviedb.org/movie/{movie.Id}")
                .WithCurrentTimestamp()
                .WithImageUrl($"https://www.themoviedb.org/t/p/w600_and_h900_bestv2/{movie.PosterPath}")
                .Build();
            return new Embed[] { embed };
        }

        private static (string title, int year) GetTitleYear(string input)
        {
            var defaultMovie = ("The Whale", 2022);
            // Split the input string by space and parentheses
            string[] parts = input.Split(new char[] { '(', ')' });

            // Validate that the input has the correct number of parts
            if (parts.Length != 3)
                return defaultMovie;

            // The title is the first part
            string title = parts[0].Trim();

            if (title is null || title == String.Empty)
                return defaultMovie;

            // Validate that the year is a valid integer
            if (!int.TryParse(parts[1], out int year))
                return defaultMovie;

            return (title, year);
        }
    }
}
