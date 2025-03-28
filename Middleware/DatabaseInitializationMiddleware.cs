using HW2NoteKeeper.Data;
using System.Text.Json;
using Microsoft.Extensions.AI;
using static HW2NoteKeeper.Controllers.NoteController; // for TagsResponse, if it's defined in NoteController

namespace HW2NoteKeeper.Middleware
{
    /// <summary>
    /// Middleware to ensure the database is created and seeded with initial data.
    /// </summary>
    public class DatabaseInitializationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DatabaseInitializationMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseInitializationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the HTTP request pipeline.</param>
        /// <param name="logger">The logger used to log informational and error messages.</param>
        public DatabaseInitializationMiddleware(RequestDelegate next, ILogger<DatabaseInitializationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to initialize and seed the database if necessary.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task that represents the completion of the middleware execution.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Bypass initialization for Swagger endpoints so that Swagger JSON loads correctly.
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            try
            {
                // Resolve the ApplicationDbContext and IChatClient from the current request's service provider.
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var chatClient = context.RequestServices.GetRequiredService<IChatClient>();

                // Ensure the database is created.
                // This bypasses Entity Framework migrations.
                dbContext.Database.EnsureCreated();

                // Seed the database if needed using your custom seeding logic.
                await dbContext.EnsureSeedDataAsync(async (details) =>
                {
                    // Set up the options for the OpenAI chat client.
                    var chatOptions = new ChatOptions()
                    {
                        Temperature = 0.7f,
                        TopP = 0.9f,
                        MaxOutputTokens = 50
                    };

                    // Create an enhanced prompt for generating tags using the provided details.
                    string enhancedPrompt = $"""
                    You are an AI assistant that returns valid JSON output only. Do not include backticks or extra formatting. 
                    Return an object with a `Phrases` array containing one-word tags.

                    Now generate relevant tags for this input:
                    "{details}"
                    """;

                    // Get a response from the OpenAI chat client.
                    var responseCompletion = await chatClient.CompleteAsync(enhancedPrompt, options: chatOptions);
                    if (responseCompletion?.Message == null)
                    {
                        throw new InvalidOperationException("The chat completion or its message is null.");
                    }

                    // Clean up the AI response text by trimming backticks and whitespace.
                    var rawResponse = responseCompletion.Message.Text!.Trim('`').Trim();

                    // Log the cleaned AI response at debug level.
                    _logger.LogDebug("AI raw response (cleaned): {Response}", rawResponse);

                    try
                    {
                        // Verify that the response is valid JSON.
                        if (!rawResponse.StartsWith("{") || !rawResponse.EndsWith("}"))
                        {
                            throw new JsonException("AI response is not valid JSON.");
                        }

                        // Deserialize the response into a TagsResponse object.
                        var response = JsonSerializer.Deserialize<TagsResponse>(rawResponse);
                        // Return the generated tags or an empty list if deserialization fails.
                        return response?.Phrases ?? new List<string>();
                    }
                    catch (JsonException ex)
                    {
                        // Log any JSON parsing errors.
                        _logger.LogError(ex, "JSON Parsing Error during AI tag generation.");
                        return new List<string>();
                    }
                });

                // Log that the database initialization and seeding has been completed.
                _logger.LogInformation("Database creation & seeding middleware completed.");
            }
            catch (Exception ex)
            {
                // Log any errors encountered during database creation or seeding.
                _logger.LogError(ex, "Error during database initialization and seeding.");
                // Optionally, handle or rethrow the exception as needed.
            }

            // Call the next middleware in the pipeline.
            await _next(context);
        }
    }
}
