namespace HW4NoteKeeper.Settings;

/// <summary>
/// Provides configuration settings related to note management.
/// </summary>
public class NoteSettings
{
    /// <summary>
    /// Gets or sets the maximum number of attachments allowed per note.
    /// </summary>
    public int MaxAttachments { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of notes allowed.
    /// </summary>
    public int MaxNotes { get; set; }
}
