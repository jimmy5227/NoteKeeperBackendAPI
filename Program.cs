using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using HW4NoteKeeper.Data;
using HW4NoteKeeper.Settings;
using HW4NoteKeeper.Services;
using System.Text.Json;
using HW4NoteKeeper.CustomSettings;

namespace HW4NoteKeeper
{
    /// <summary>
    /// Entry point for the HW4NoteKeeper application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures and runs the web application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static async Task Main(string[] args)
        {
            // Create the web application builder.
            var builder = WebApplication.CreateBuilder(args);

            // Load configuration files.
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            // Retrieve the connection string from configuration.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register the ApplicationDbContext with SQL Server.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Bind configuration sections to settings classes.
            builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));
            builder.Services.Configure<NoteSettings>(builder.Configuration.GetSection("NoteSettings"));

            // Create a logger factory.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Configure CORS to allow requests from localhost.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", policy =>
                {
                    policy.WithOrigins("http://localhost:5198")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add controllers.
            builder.Services.AddControllers();

            // Configure Swagger for API documentation.
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Note Keeper API",
                    Version = "v1",
                    Description = "An API to manage notes with AI-generated tags."
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                // Register custom file upload operation filter.
                options.OperationFilter<SwaggerUploadFileParametersFilter>();
            });

            // Add endpoints API explorer (used by Swagger).
            builder.Services.AddEndpointsApiExplorer();

            // Retrieve and validate AISettings from configuration.
            AISettings? aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>();
            var logger = loggerFactory.CreateLogger("Program");

            if (aiSettings is null ||
                string.IsNullOrWhiteSpace(aiSettings.DeploymentUri) ||
                string.IsNullOrWhiteSpace(aiSettings.ApiKey))
            {
                if (aiSettings == null)
                {
                    logger.LogCritical("AISettings is null. Please ensure the configuration is present.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(aiSettings.DeploymentUri))
                    {
                        logger.LogCritical("AISettings.DeploymentUri is null or empty.");
                    }
                    if (string.IsNullOrWhiteSpace(aiSettings.ApiKey))
                    {
                        logger.LogCritical("AISettings.ApiKey is null or empty.");
                    }
                }
                throw new InvalidOperationException("AISettings validation failed. Check the logs for details.");
            }

            logger.LogInformation("AISettings loaded successfully.");

            builder.Services.AddSingleton(aiSettings);

            // Register the OpenAI client.
            Uri openAIServiceEndpointUri = new Uri(aiSettings.DeploymentUri);
            AzureKeyCredential apiKeyCredential = new AzureKeyCredential(aiSettings.ApiKey);
            RegisterOpenAIClient(builder, openAIServiceEndpointUri, apiKeyCredential, aiSettings.DeploymentModelName);

            // Register the tag generator service.
            builder.Services.AddScoped<ITagGeneratorService, TagGeneratorService>();

            // Add Application Insights telemetry.
            builder.Services.AddApplicationInsightsTelemetry();

            // Register logging events.
            builder.Services.AddSingleton<LoggingEvents>();

            // Build the application.
            var app = builder.Build();

            // Seed the database at startup.
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var dbInitializerLogger = scope.ServiceProvider.GetRequiredService<ILogger<DBInitializer>>();
                    var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();

                    // Ensure the database is created.
                    dbContext.Database.EnsureCreated();

                    // Create a DBInitializer instance.
                    var dbInitializer = new DBInitializer(dbContext, configuration, dbInitializerLogger);

                    // Seed data with AI-generated tags.
                    await dbInitializer.EnsureSeedDataAsync(async (details) =>
                    {
                        var chatOptions = new ChatOptions
                        {
                            Temperature = 0.7f,
                            TopP = 0.9f,
                            MaxOutputTokens = 50
                        };

                        string enhancedPrompt = $@"
                            You are an AI assistant that returns valid JSON output only. Do not include extra formatting.
                            Return an object with a ""Phrases"" array containing one-word tags.
                            Now generate relevant tags for this input:
                            ""{details}""";

                        var responseCompletion = await chatClient.CompleteAsync(enhancedPrompt, options: chatOptions);
                        if (responseCompletion?.Message == null)
                        {
                            throw new InvalidOperationException("The chat completion or its message is null.");
                        }

                        var rawResponse = responseCompletion!.Message!.Text!.Trim('`').Trim();
                        dbInitializerLogger.LogDebug("AI raw response (cleaned): {Response}", rawResponse);

                        try
                        {
                            // Validate that the AI response is a valid JSON object.
                            if (!rawResponse.StartsWith("{") || !rawResponse.EndsWith("}"))
                            {
                                throw new JsonException("AI response is not valid JSON.");
                            }

                            var response = JsonSerializer.Deserialize<TagsResponse>(rawResponse);
                            return response?.Phrases ?? new List<string>();
                        }
                        catch (JsonException ex)
                        {
                            dbInitializerLogger.LogError(ex, "JSON Parsing Error during AI tag generation.");
                            return new List<string>();
                        }
                    });

                    dbInitializerLogger.LogInformation("Database seeding completed at startup.");
                }
                catch (Exception ex)
                {
                    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    startupLogger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Enable the defined CORS policy.
            app.UseCors("AllowLocalhost");

            // Redirect root ("/") requests to "/index.html".
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/index.html", permanent: false);
                    return;
                }
                await next();
            });

            // Serve default and static files.
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Enable Swagger middleware.
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Note Keeper API");
                options.RoutePrefix = string.Empty;
            });

            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            // Run the application.
            app.Run();
        }

        /// <summary>
        /// Registers the OpenAI client with the dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="WebApplicationBuilder"/> instance used for configuring services.</param>
        /// <param name="openAIServiceEndpointUri">The URI of the OpenAI service endpoint.</param>
        /// <param name="apiKeyCredential">The <see cref="AzureKeyCredential"/> containing the API key for the OpenAI service.</param>
        /// <param name="deploymentName">The deployment model name for the OpenAI service.</param>
        private static void RegisterOpenAIClient(
            WebApplicationBuilder builder,
            Uri openAIServiceEndpointUri,
            AzureKeyCredential apiKeyCredential,
            string deploymentName)
        {
            // Register the OpenAI chat client as a service.
            builder.Services.AddChatClient(services =>
                new AzureOpenAIClient(openAIServiceEndpointUri, apiKeyCredential)
                    .GetChatClient(deploymentName)
                    .AsChatClient()
            );
        }
    }
}
