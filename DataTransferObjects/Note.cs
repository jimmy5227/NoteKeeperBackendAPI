using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace HW4NoteKeeper.DataTransferObjects
{
    /// <summary>
    /// Represents a note entity stored in the database and used for API responses.
    /// </summary>
    public class Note
    {
        /// <summary>
        /// Gets or sets the unique identifier for the note.
        /// </summary>
        [Key]
        [JsonPropertyName("noteId")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the summary of the note.
        /// </summary>
        [Required]
        [StringLength(60, MinimumLength = 1)]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed content of the note.
        /// </summary>
        [Required]
        [StringLength(1024, MinimumLength = 1)]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date of the note in UTC.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedDateUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the last modified date of the note in UTC.
        /// </summary>
        public DateTimeOffset? ModifiedDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the collection of tags associated with the note.
        /// This property is used for persistence and is not directly exposed in JSON.
        /// </summary>
        [JsonIgnore]
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        /// <summary>
        /// Gets a list of tag names for this note.
        /// This computed property is not mapped to the database.
        /// It is serialized as "tags" in the API response.
        /// </summary>
        [NotMapped]
        [JsonPropertyName("tags")]
        public List<string> TagNames => Tags.Select(t => t.Name).ToList();
    }

    /// <summary>
    /// Represents a request to create a new note.
    /// </summary>
    public class CreateNoteRequest
    {
        /// <summary>
        /// Gets or sets the summary of the note.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed content of the note.
        /// </summary>
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a request to update an existing note.
    /// </summary>
    public class UpdateNoteRequest
    {
        /// <summary>
        /// Gets or sets the updated summary of the note.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the updated detailed content of the note.
        /// </summary>
        public string? Details { get; set; }
    }
}
