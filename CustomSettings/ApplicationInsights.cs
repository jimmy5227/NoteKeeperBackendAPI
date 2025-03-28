namespace HW2NoteKeeper.CustomSettings
{
    /// <summary>
    /// Represents configuration settings for Application Insights.
    /// </summary>
    public class ApplicationInsights
    {
        /// <summary>
        /// Gets or sets the connection string for Application Insights.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the instrumentation key for Application Insights.
        /// </summary>
        public string InstrumentationKey { get; set; } = string.Empty;

        // Additional Application Insights-related settings can be added here.
    }
}
