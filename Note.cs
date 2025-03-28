namespace HW1NoteKeeper;

/// <summary>
/// Represents a note entity.
/// </summary>
public class Note
{
    /// <summary>
    /// Unique identifier for the note.
    /// </summary>
    public Guid NoteId { get; set; }

    /// <summary>
    /// The summary/title of the note.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The detailed content of the note.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// The date and time the note was created, stored in UTC format.
    /// </summary>
    public DateTime CreatedDateUtc { get; set; }

    /// <summary>
    /// The date and time the note was last modified, stored in UTC format. Null if never modified.
    /// </summary>
    public DateTime? ModifiedDateUtc { get; set; }

    /// <summary>
    /// A list of tags generated for the note content.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents a request to create a new note.
/// </summary>
public class CreateNoteRequest
{
    /// <summary>
    /// The summary/title of the note. Required and must be between 1 and 60 characters.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The detailed content of the note. Required and must be between 1 and 1024 characters.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Represents a request to update an existing note.
/// </summary>
public class UpdateNoteRequest
{
    /// <summary>
    /// The new summary/title of the note. Optional, but must be between 1 and 60 characters if provided.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// The new detailed content of the note. Optional, but must be between 1 and 1024 characters if provided.
    /// </summary>
    public string? Details { get; set; }
}
