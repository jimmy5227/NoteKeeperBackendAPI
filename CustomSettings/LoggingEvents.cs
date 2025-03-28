using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace HW3NoteKeeper.CustomSettings
{
    /// <summary>
    /// Provides methods to track telemetry events and exceptions.
    /// </summary>
    public class LoggingEvents
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingEvents"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client used for sending telemetry data.</param>
        public LoggingEvents(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Tracks an event when an attachment is created.
        /// </summary>
        /// <param name="attachmentId">The identifier of the created attachment.</param>
        /// <param name="attachmentSize">The size of the created attachment.</param>
        public void TrackAttachmentCreated(string attachmentId, long attachmentSize)
        {
            var properties = new Dictionary<string, string>
            {
                { "attachmentid", attachmentId }
            };

            var metrics = new Dictionary<string, double>
            {
                { "AttachmentSize", attachmentSize }
            };

            _telemetryClient.TrackEvent("AttachmentCreated", properties, metrics);
        }

        /// <summary>
        /// Tracks an event when an attachment is updated.
        /// </summary>
        /// <param name="attachmentId">The identifier of the updated attachment.</param>
        /// <param name="attachmentSize">The size of the updated attachment.</param>
        public void TrackAttachmentUpdated(string attachmentId, long attachmentSize)
        {
            var properties = new Dictionary<string, string>
            {
                { "attachmentid", attachmentId }
            };

            var metrics = new Dictionary<string, double>
            {
                { "AttachmentSize", attachmentSize }
            };

            _telemetryClient.TrackEvent("AttachmentUpdated", properties, metrics);
        }

        /// <summary>
        /// Tracks a validation error as a trace.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <param name="inputPayload">The input payload that caused the validation error.</param>
        public void TrackValidationError(string message, string inputPayload)
        {
            var properties = new Dictionary<string, string>
            {
                { "InputPayload", inputPayload }
            };

            _telemetryClient.TrackTrace(message, SeverityLevel.Warning, properties);
        }

        /// <summary>
        /// Tracks an exception, including the input payload that caused it.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="inputPayload">The input payload associated with the exception.</param>
        public void TrackException(Exception ex, string inputPayload)
        {
            var properties = new Dictionary<string, string>
            {
                { "InputPayload", inputPayload }
            };

            _telemetryClient.TrackException(ex, properties);
        }
    }
}
