using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HW3NoteKeeper.DataTransferObjects
{
    /// <summary>
    /// Represents a tag associated with a note.
    /// This class is used for both persistence (via EF Core) and as the API output,
    /// exposing only the tag name.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tag.
        /// This property is required for database persistence but is hidden from API responses.
        /// </summary>
        [Key]
        [JsonIgnore]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the foreign key that references the associated note.
        /// This is used internally and is hidden from API responses.
        /// </summary>
        [JsonIgnore]
        public Guid NoteId { get; set; }

        /// <summary>
        /// Gets or sets the tag name.
        /// This is the only property exposed in API responses.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
