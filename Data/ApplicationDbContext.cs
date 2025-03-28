using HW2NoteKeeper.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

namespace HW2NoteKeeper.Data
{
    /// <summary>
    /// Represents the application's database context for managing notes and tags.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Gets or sets the notes stored in the database.
        /// </summary>
        public DbSet<Note> Notes { get; set; }

        /// <summary>
        /// Gets or sets the tags stored in the database.
        /// </summary>
        public DbSet<Tag> Tags { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class using the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        /// <summary>
        /// Ensures that the database is seeded with initial notes and AI-generated tags.
        /// If notes already exist, seeding is skipped.
        /// </summary>
        /// <param name="generateTagsAsync">
        /// A delegate that generates a list of tags asynchronously for the given note details.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureSeedDataAsync(Func<string, Task<List<string>>> generateTagsAsync)
        {
            // If any notes exist in the database, skip seeding.
            if (await Notes.AnyAsync())
            {
                Console.WriteLine("Database already seeded. Skipping...");
                return;
            }

            // Define a list of seed notes with basic details.
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

            // For each seed note, generate AI-based tags and add the note with tags to the context.
            foreach (var note in seededNotes)
            {
                // Generate AI-based tags for the note's details.
                var generatedTags = await generateTagsAsync(note.Details);

                // Assign generated tags to the note.
                note.Tags = generatedTags.Select(tagName => new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName,
                    NoteId = note.Id
                }).ToList();

                // Add the note (with its associated tags) to the database context.
                Notes.Add(note);
            }

            // Save all changes to the database.
            await SaveChangesAsync();
            Console.WriteLine("Database seeding completed with AI-generated tags.");
        }
    }
}
