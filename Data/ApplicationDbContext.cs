using HW4NoteKeeper.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

namespace HW4NoteKeeper.Data
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
    }
}
