using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HW4NoteKeeper.Data;
using HW4NoteKeeper.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

namespace HW4NoteKeeper
{
    /// <summary>
    /// Encapsulates logic to seed the database with initial notes, AI-generated tags, and attachments.
    /// </summary>
    public class DBInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DBInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DBInitializer"/> class.
        /// </summary>
        /// <param name="context">The application's database context.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public DBInitializer(ApplicationDbContext context, IConfiguration configuration, ILogger<DBInitializer> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Ensures that the database is seeded with initial data and attachments.
        /// </summary>
        /// <param name="generateTagsAsync">
        /// A delegate that generates a list of tags asynchronously for the given note details.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureSeedDataAsync(Func<string, Task<List<string>>> generateTagsAsync)
        {
            // Map note summaries to their expected attachment file names.
            var attachmentsMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Running grocery list", new List<string>{ "MilkAndEggs.png", "Oranges.png" } },
                { "Gift supplies notes", new List<string>{ "WrappingPaper.png", "Tape.png" } },
                { "Valentine's Day gift ideas", new List<string>{ "Chocolate.png", "Diamonds.png", "NewCar.png" } },
                { "Azure tips", new List<string>{ "AzureLogo.png", "AzureTipsAndTricks.pdf" } }
            };

            // Skip seeding if the database already contains notes.
            if (await _context.Notes.AnyAsync())
            {
                Console.WriteLine("Database already seeded. Skipping SQL and attachments seeding...");
                return;
            }

            // Create seed notes.
            var seededNotes = new List<Note>
            {
                new Note
                {
                    Id = Guid.NewGuid(),
                    Summary = "Running grocery list",
                    Details = "Milk, Eggs, Oranges",
                    CreatedDateUtc = DateTime.UtcNow,
                    ModifiedDateUtc = null
                },
                new Note
                {
                    Id = Guid.NewGuid(),
                    Summary = "Gift supplies notes",
                    Details = "Tape & Wrapping Paper",
                    CreatedDateUtc = DateTime.UtcNow,
                    ModifiedDateUtc = null
                },
                new Note
                {
                    Id = Guid.NewGuid(),
                    Summary = "Valentine's Day gift ideas",
                    Details = "Chocolate, Diamonds, New Car",
                    CreatedDateUtc = DateTime.UtcNow,
                    ModifiedDateUtc = null
                },
                new Note
                {
                    Id = Guid.NewGuid(),
                    Summary = "Azure tips",
                    Details = "portal.azure.com is a quick way to get to the portal. Remember double underscore for Linux and colon for Windows.",
                    CreatedDateUtc = DateTime.UtcNow,
                    ModifiedDateUtc = null
                }
            };

            // Seed notes with AI-generated tags.
            foreach (var note in seededNotes)
            {
                var generatedTags = await generateTagsAsync(note.Details);
                note.Tags = generatedTags.Select(tagName => new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName,
                    NoteId = note.Id
                }).ToList();

                _context.Notes.Add(note);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("SQL seeding completed with AI-generated tags.");

            // Seed attachments in Azure Blob Storage.
            string storageConnectionString = _configuration.GetConnectionString("DefaultStorageConnection")!;
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }

            // Assume the "SampleAttachments" folder is located in the project root.
            string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
            string attachmentsFolder = Path.Combine(projectRoot, "SampleAttachments");

            _logger.LogInformation("Looking for attachments in: {Path}", attachmentsFolder);
            if (!Directory.Exists(attachmentsFolder))
            {
                _logger.LogWarning("Attachments folder not found: {Path}", attachmentsFolder);
            }
            else
            {
                var files = Directory.GetFiles(attachmentsFolder);
                _logger.LogInformation("Found the following files in SampleAttachments: {Files}", string.Join(", ", files));
            }

            // Seed attachments for each note.
            foreach (var note in seededNotes)
            {
                if (!attachmentsMap.TryGetValue(note.Summary, out var fileNames))
                {
                    continue;
                }

                // Use the note's Id (in lower-case) as the container name.
                string containerName = note.Id.ToString().ToLowerInvariant();
                BlobContainerClient containerClient = new BlobContainerClient(storageConnectionString, containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                foreach (var fileName in fileNames)
                {
                    string filePath = Path.Combine(attachmentsFolder, fileName);
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("Attachment file not found: {FilePath}", filePath);
                        continue;
                    }

                    BlobClient blobClient = containerClient.GetBlobClient(fileName);
                    if (await blobClient.ExistsAsync())
                    {
                        _logger.LogInformation("Blob {FileName} already exists in container {ContainerName}. Skipping upload.", fileName, containerName);
                        continue;
                    }

                    using (var fileStream = File.OpenRead(filePath))
                    {
                        await blobClient.UploadAsync(fileStream, overwrite: false);
                    }

                    var metadata = new Dictionary<string, string>
                    {
                        { "NoteId", note.Id.ToString() }
                    };
                    await blobClient.SetMetadataAsync(metadata);
                }
            }

            Console.WriteLine("Attachments seeding completed.");
        }
    }
}
