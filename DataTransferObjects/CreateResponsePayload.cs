namespace HW4NoteKeeper.DataTransferObjects
{
    /// <summary>
    /// Defines the response structure when creating a note
    /// </summary>
    public class CreateResponsePayload
    {
        /// <summary>
        /// The Id assigned to the note
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string? id { get; set; }

        /// <summary>
        /// The message contents of the note
        /// </summary>
        /// <value>The message contents of the note.</value>
        public string? message { get; set; }
    }
}