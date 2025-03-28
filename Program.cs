using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using HW1NoteKeeper.Settings;

namespace HW1NoteKeeper
{
    /// <summary>
    /// The entry point for the Note Keeper API application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main method initializes and runs the web application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load sensitive configuration from secrets.json.
            // This file should be secured and excluded from source control.
            builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // This will negate CORS for localhost trying to access this API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5198")
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            // Configure Swagger generation for API documentation
            builder.Services.AddSwaggerGen();

            // Register controllers for handling API requests
            builder.Services.AddControllers();

            // Adds OpenAPI/Swagger support to the application
            builder.Services.AddOpenApi();

            // Add services required for API exploration (e.g., Swagger/OpenAPI documentation)
            builder.Services.AddEndpointsApiExplorer();

            // Bind AISettings from configuration (appsettings.json and secrets.json)
            AISettings? _aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>()!;

            // Validate AISettings to ensure they are not null or empty
            var logger = loggerFactory.CreateLogger("Program");

            if (_aiSettings is null
                || string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri)
                || string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            {
                if (_aiSettings == null)
                {
                    logger.LogCritical("AISettings is null. Please ensure the configuration is present.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri))
                    {
                        logger.LogCritical("AISettings.DeploymentUri is null or empty.");
                    }
                    if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
                    {
                        logger.LogCritical("AISettings.ApiKey is null or empty.");
                    }
                }
                throw new InvalidOperationException("AISettings validation failed. Check the logs for details.");
            }

            // If validation passes, log success
            logger.LogInformation("AISettings loaded successfully.");

            builder.Services.AddSingleton(_aiSettings);

            // Initialize OpenAI service endpoint and API key credential
            Uri openAIServiceEndpointUri = new Uri(_aiSettings.DeploymentUri);
            AzureKeyCredential apiKeyCredential = new AzureKeyCredential(_aiSettings.ApiKey);
            RegisterOpenAIClient(builder, openAIServiceEndpointUri, apiKeyCredential, _aiSettings.DeploymentModelName);

            var app = builder.Build();

            app.UseCors("AllowLocalhost");

            // Check if the application is running in a development environment
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Note Keeper API");
                options.RoutePrefix = string.Empty; // Makes Swagger UI the default page
            });

            // Map OpenAPI endpoints allowing the API documentation to be accessible
            app.MapOpenApi();

            app.UseRouting();

            // Enable authorization middleware to handle authentication and authorization
            app.UseAuthorization();

            // Map the controllers to their respective routes
            app.MapControllers();

            // Start the application and listen for incoming requests
            app.Run();
        }

        /// <summary>
        /// Registers the OpenAI client with the specified parameters.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="openAIServiceEndpointUri">The OpenAI service endpoint URI.</param>
        /// <param name="apiKeyCredential">The API key credential.</param>
        /// <param name="deploymentName">The deployment name.</param>
        private static void RegisterOpenAIClient(WebApplicationBuilder builder,
                                            Uri openAIServiceEndpointUri,
                                            AzureKeyCredential apiKeyCredential,
                                            string deploymentName)
        {
            // Register the OpenAI client as a singleton service.
            builder.Services.AddChatClient(services =>
                new AzureOpenAIClient(openAIServiceEndpointUri, apiKeyCredential)
                    .GetChatClient(deploymentName)
                    .AsChatClient()
            );
        }
    }
}
