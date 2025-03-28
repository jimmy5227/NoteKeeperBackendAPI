namespace HW2NoteKeeper.Common
{
    /// <summary>
    /// Contains constants representing event IDs used for logging telemetry events.
    /// </summary>
    public static class LoggingEvents
    {
        /// <summary>
        /// Event ID for note creation.
        /// </summary>
        public const int NoteCreated = 1000;

        /// <summary>
        /// Event ID for note updates.
        /// </summary>
        public const int NoteUpdated = 1001;

        /// <summary>
        /// Event ID for validation errors.
        /// </summary>
        public const int ValidationError = 4000;

        /// <summary>
        /// Event ID for exceptions.
        /// </summary>
        public const int ExceptionThrown = 5000;
    }
}
