using Discord;
using Discord.Interactions;
using TMDbLib.Client;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels;
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

            (var title, var year) = await GetRecommendationFromGPT(prompt, Models.ChatGpt3_5Turbo);
            var movie = await GetMovieFromTMDB(title, year);
            var embeds = BuildEmbeds(movie);
            var components = BuildMessageComponent(prompt);

            await FollowupAsync(embeds: embeds, components: components);
        }

        [ComponentInteraction("nextrec:*")]
        public async Task NextRecHandler(string tag)
        { //Just get the prompt from the button ID and handle it like a new rec command
            string[] parts = tag.Split('~');
            var model = parts[0];
            var prompt = string.Concat(parts[1..]);

            await Context.Interaction.DeferAsync();
            (string title, int year) = await GetRecommendationFromGPT(prompt, model);
            Movie movie = await GetMovieFromTMDB(title, year);
            var embeds = BuildEmbeds(movie);
            var components = BuildMessageComponent(prompt);
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embeds = embeds;
                x.Components = components;
            });
        }

        private static MessageComponent BuildMessageComponent(string prompt)
        {
            return new ComponentBuilder()
                .WithButton("Next Recommendation", $"nextrec:{Models.ChatGpt3_5Turbo}~{prompt}")
                .Build();
        }

        private static async Task<Movie> GetMovieFromTMDB(string title, int year)
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

        private static async Task<(string, int)> GetRecommendationFromGPT(string prompt, string model)
        {
            var apiKey = File.ReadAllText("openAiKey.txt");
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromUser($"Recommend a movie. The only output provided should be the title and year in the format: Title (Year).  No other text. The prompt is: {prompt}. Output format: Title (Year). Example: Movie Title (Year)."),
                },
                Model = Models.ChatGpt3_5Turbo
            });

            if (completionResult.Successful)
            {
                Console.WriteLine(completionResult.Choices.First().Message.Content);
            }
            else
            {
                Console.WriteLine($"Error:\n {completionResult?.Error?.Code}: {completionResult?.Error?.Message}");
            }

            var result = completionResult?.Choices[0].Message.Content ?? "The Whale (2022)";

            return GetTitleYear(result);
        }

        private static Embed[] BuildEmbeds(Movie movie)
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

            if (string.IsNullOrEmpty(title))
                return defaultMovie;

            // Validate that the year is a valid integer
            if (!int.TryParse(parts[1], out int year))
                return defaultMovie;

            return (title, year);
        }
    }
}
