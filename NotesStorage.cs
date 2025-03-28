namespace HW1NoteKeeper;

/// <summary>
/// Static class responsible for managing in-memory storage of notes.
/// </summary>
public static class NotesStorage
{
    /// <summary>
    /// Internal storage for notes.
    /// </summary>
    private static readonly List<Note> Notes = new();

    /// <summary>
    /// Adds a new note to storage.
    /// </summary>
    /// <param name="note">The note object to add.</param>
    public static void AddNote(Note note) => Notes.Add(note);

    /// <summary>
    /// Retrieves a note by its unique identifier.
    /// </summary>
    /// <param name="id">The ID of the note to retrieve.</param>
    /// <returns>The note if found; otherwise, null.</returns>
    public static Note? GetNoteById(Guid id) => Notes.Find(n => n.NoteId == id);

    /// <summary>
    /// Updates an existing note in storage.
    /// </summary>
    /// <param name="updatedNote">The updated note object.</param>
    public static void UpdateNote(Note updatedNote)
    {
        var note = Notes.Find(n => n.NoteId == updatedNote.NoteId);
        if (note != null)
        {
            note.Summary = updatedNote.Summary;
            note.Details = updatedNote.Details;
            note.ModifiedDateUtc = updatedNote.ModifiedDateUtc;
            note.Tags = updatedNote.Tags;
        }
    }

    /// <summary>
    /// Deletes a note by its unique identifier.
    /// </summary>
    /// <param name="id">The ID of the note to delete.</param>
    /// <returns>True if the note was deleted; otherwise, false.</returns>
    public static bool DeleteNote(Guid id)
    {
        var note = Notes.Find(n => n.NoteId == id);
        if (note == null)
        {
            return false;
        }
        return Notes.Remove(note);
    }

    /// <summary>
    /// Retrieves all notes from storage.
    /// </summary>
    /// <returns>A list of all notes.</returns>
    public static List<Note> GetAllNotes() => Notes;

}
