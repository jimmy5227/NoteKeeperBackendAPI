using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using HW2NoteKeeper.Settings;
using HW2NoteKeeper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using HW2NoteKeeper.Middleware;
using HW2NoteKeeper.Common;
using Microsoft.ApplicationInsights;

namespace HW2NoteKeeper
{
    /// <summary>
    /// Entry point for the HW2NoteKeeper application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method which configures and runs the web application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            // Create the web application builder.
            var builder = WebApplication.CreateBuilder(args);

            // Load the secrets.json file.
            builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            // Retrieve the Application Insights connection string from configuration.
            var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                Console.WriteLine("Application Insights is configured. Connection string found.");
                builder.Services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
            }
            else
            {
                Console.WriteLine("Warning: Application Insights Connection String is not configured.");
            }

            // Retrieve the connection string from configuration.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register the ApplicationDbContext with SQL Server using the connection string.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Bind the AISettings section from configuration to the AISettings class.
            builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));

            // Create a logger factory for logging.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Enable Cross-Origin Resource Sharing (CORS) for requests from localhost.
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

            // Configure controllers and JSON serialization options.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Customize JSON options here if needed.
                });

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
            });

            // Add endpoints API explorer (used by Swagger).
            builder.Services.AddEndpointsApiExplorer();

            // Retrieve and validate AISettings from configuration.
            AISettings? _aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>()!;
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

            logger.LogInformation("AISettings loaded successfully.");

            builder.Services.AddSingleton(_aiSettings);
            builder.Services.AddSingleton<TelemetryService>();

            // Register the OpenAI client with the provided settings.
            Uri openAIServiceEndpointUri = new Uri(_aiSettings.DeploymentUri);
            AzureKeyCredential apiKeyCredential = new AzureKeyCredential(_aiSettings.ApiKey);
            RegisterOpenAIClient(builder, openAIServiceEndpointUri, apiKeyCredential, _aiSettings.DeploymentModelName);

            // Build the application.
            var app = builder.Build();

            // Verify TelemetryClient initialization
            var telemetryClient = app.Services.GetService<TelemetryClient>();
            if (telemetryClient != null)
            {
                Console.WriteLine("TelemetryClient initialized successfully. Application Insights is connected.");
            }
            else
            {
                Console.WriteLine("TelemetryClient is null. Application Insights may not be connected.");
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

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Note Keeper API");
                options.RoutePrefix = string.Empty;
            });

            app.UseMiddleware<DatabaseInitializationMiddleware>();

            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

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
