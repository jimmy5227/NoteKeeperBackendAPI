using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Text.Json;

namespace HW2NoteKeeper.Common
{
    /// <summary>
    /// Provides methods to track telemetry events, traces, and exceptions using Application Insights.
    /// </summary>
    public class TelemetryService
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class.
        /// </summary>
        /// <param name="telemetryClient">The Application Insights telemetry client.</param>
        public TelemetryService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Tracks a "NoteCreated" event with the specified details.
        /// </summary>
        /// <param name="summary">The summary of the note.</param>
        /// <param name="summaryLength">The length of the note summary.</param>
        /// <param name="detailsLength">The length of the note details.</param>
        /// <param name="tagCount">The number of tags associated with the note.</param>
        public void TrackNoteCreatedEvent(string summary, int summaryLength, int detailsLength, int tagCount)
        {
            var properties = new Dictionary<string, string>
            {
                { "summary", summary },
                { "SummaryLength", summaryLength.ToString() },
                { "DetailsLength", detailsLength.ToString() },
                { "tagcount", tagCount.ToString() }
            };

            _telemetryClient.TrackEvent("NoteCreated", properties);
        }

        /// <summary>
        /// Tracks a validation error using Application Insights Trace.
        /// </summary>
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        /// <param name="inputPayload">The payload that caused the validation error.</param>
        public void TrackValidationError(string errorMessage, object inputPayload)
        {
            var properties = new Dictionary<string, string>
            {
                { "InputPayload", JsonSerializer.Serialize(inputPayload) }
            };

            _telemetryClient.TrackTrace($"Validation Error: {errorMessage}", SeverityLevel.Warning, properties);
        }

        /// <summary>
        /// Tracks an exception using Application Insights Exception telemetry.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="inputPayload">The input payload associated with the exception.</param>
        public void TrackException(Exception ex, object inputPayload)
        {
            var properties = new Dictionary<string, string>
            {
                { "InputPayload", JsonSerializer.Serialize(inputPayload) }
            };

            _telemetryClient.TrackException(ex, properties);
        }

        /// <summary>
        /// Tracks a "NoteUpdated" event with the specified details.
        /// </summary>
        /// <param name="summary">The updated note summary. Use empty string if not provided.</param>
        /// <param name="details">The updated note details. Use empty string if not provided.</param>
        /// <param name="summaryLength">The length of the updated summary (0 if not provided).</param>
        /// <param name="detailsLength">The length of the updated details (0 if not provided).</param>
        /// <param name="tagCount">The number of tags after update (0 if details not provided).</param>
        public void TrackNoteUpdatedEvent(string summary, string details, int summaryLength, int detailsLength, int tagCount)
        {
            var properties = new Dictionary<string, string>
            {
                { "summary", summary },
                { "details", details },
                { "SummaryLength", summaryLength.ToString() },
                { "DetailsLength", detailsLength.ToString() },
                { "TagCount", tagCount.ToString() }
            };

            _telemetryClient.TrackEvent("NoteUpdated", properties);
        }
    }
}
