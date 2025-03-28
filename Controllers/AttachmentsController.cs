using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HW4NoteKeeper.Data;
using HW4NoteKeeper.DataTransferObjects;
using HW4NoteKeeper.CustomSettings;
using System.Text.Json;

namespace HW4NoteKeeper.AttachmentsController
{
    /// <summary>
    /// Controller for handling blob operations related to note attachments.
    /// </summary>
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class NoteAttachmentsController : ControllerBase
    {
        private const string GetPublicFileByIdRouteName = nameof(GetPublicFileByIdRouteName);
        private const string CustomMetadata = nameof(CustomMetadata);
        private const string PUBLIC_FILES_CONTAINER = "publicfilescontainer";

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        public IConfiguration Configuration { get; set; }

        private readonly ILogger<NoteAttachmentsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly LoggingEvents _loggingEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteAttachmentsController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="context">Database context.</param>
        /// <param name="loggingEvents">Logging events instance.</param>
        public NoteAttachmentsController(IConfiguration configuration,
                                               ILogger<NoteAttachmentsController> logger,
                                               ApplicationDbContext context,
                                               LoggingEvents loggingEvents)
        {
            Configuration = configuration;
            _logger = logger;
            _context = context;
            _loggingEvents = loggingEvents;
        }

        /// <summary>
        /// Uploads or updates an attachment blob for a note.
        /// </summary>
        /// <param name="noteId">The note identifier.</param>
        /// <param name="attachmentId">The attachment identifier.</param>
        /// <param name="upload">The file upload DTO containing file data.</param>
        /// <returns>A status response indicating the outcome.</returns>
        [HttpPut("/notes/{noteId}/attachments/{attachmentId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutAttachment(string noteId, string attachmentId, [FromForm] FileUploadDto upload)
        {
            // Validate inputs.
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(attachmentId))
            {
                string payload = JsonSerializer.Serialize(new { noteId, attachmentId });
                _loggingEvents.TrackValidationError("NoteId and attachmentId must be provided.", payload);
                return BadRequest("NoteId and attachmentId must be provided.");
            }
            if (upload?.FileData == null || upload.FileData.Length == 0)
            {
                string payload = JsonSerializer.Serialize(new { noteId, attachmentId });
                _loggingEvents.TrackValidationError("File data must be provided and not empty.", payload);
                return BadRequest("File data must be provided and not empty.");
            }

            // Convert noteId to Guid and verify the note exists.
            if (!Guid.TryParse(noteId, out Guid noteGuid))
            {
                string payload = JsonSerializer.Serialize(new { noteId });
                _loggingEvents.TrackValidationError("Invalid noteId format.", payload);
                return BadRequest("Invalid noteId format.");
            }
            var note = await _context.Notes.FindAsync(noteGuid);
            if (note == null)
            {
                _logger.LogWarning("Note not found: {NoteId}", noteId);
                return NotFound();
            }

            // Retrieve storage connection string and max attachments setting.
            string connectionString = Configuration.GetConnectionString("DefaultStorageConnection")!;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }
            int maxAttachments = Configuration.GetValue<int>("NoteSettings:MaxAttachments", 3);

            // Use noteId as the container name.
            string containerName = noteId.ToLowerInvariant();
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Get the blob client for the attachment.
            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);
            bool blobExists = await blobClient.ExistsAsync();

            // For new attachments, check the current attachment count.
            if (!blobExists)
            {
                int blobCount = 0;
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    blobCount++;
                }
                if (blobCount >= maxAttachments)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Attachment limit reached",
                        Detail = $"Attachment limit reached MaxAttachments [{maxAttachments}]"
                    });
                }
            }

            // Upload or update the blob.
            try
            {
                using (var stream = upload.FileData.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = upload.FileData.ContentType });

                // Set custom metadata.
                var metadata = new Dictionary<string, string>
                {
                    { "NoteId", noteId }
                };
                await blobClient.SetMetadataAsync(metadata);
            }
            catch (Exception ex)
            {
                string payload = JsonSerializer.Serialize(new { noteId, attachmentId });
                _loggingEvents.TrackException(ex, payload);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error uploading attachment.");
            }

            // Track telemetry and return appropriate response.
            if (blobExists)
            {
                _loggingEvents.TrackAttachmentUpdated(attachmentId, upload.FileData.Length);
                return NoContent();
            }
            else
            {
                _loggingEvents.TrackAttachmentCreated(attachmentId, upload.FileData.Length);
                string resourceUrl = $"{Request.Scheme}://{Request.Host}/notes/{noteId}/attachments/{attachmentId}";
                return Created(resourceUrl, null);
            }
        }

        /// <summary>
        /// Deletes an attachment blob for a note.
        /// </summary>
        /// <param name="noteId">The note identifier.</param>
        /// <param name="attachmentId">The attachment identifier.</param>
        /// <returns>A status response indicating the outcome.</returns>
        [HttpDelete("/notes/{noteId}/attachments/{attachmentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAttachment(string noteId, string attachmentId)
        {
            // Validate inputs.
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(attachmentId))
            {
                string payload = JsonSerializer.Serialize(new { noteId, attachmentId });
                _loggingEvents.TrackValidationError("NoteId and attachmentId must be provided.", payload);
                return BadRequest("NoteId and attachmentId must be provided.");
            }

            // Convert noteId to Guid and verify the note exists.
            if (!Guid.TryParse(noteId, out Guid noteGuid))
            {
                string payload = JsonSerializer.Serialize(new { noteId });
                _loggingEvents.TrackValidationError("Invalid noteId format.", payload);
                return BadRequest("Invalid noteId format.");
            }
            var note = await _context.Notes.FindAsync(noteGuid);
            if (note == null)
            {
                _logger.LogWarning("Note not found: {NoteId}", noteId);
                return NotFound();
            }

            // Retrieve storage connection string.
            string connectionString = Configuration.GetConnectionString("DefaultStorageConnection")!;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }

            // Use noteId as the container name.
            string containerName = noteId.ToLowerInvariant();
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            // Get the blob client for the attachment.
            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);
            bool blobExists = await blobClient.ExistsAsync();

            if (!blobExists)
            {
                _logger.LogWarning("Attachment not found (already missing): {AttachmentId} in note {NoteId}", attachmentId, noteId);
                return NoContent();
            }

            try
            {
                await blobClient.DeleteAsync();
                _logger.LogInformation("Attachment {AttachmentId} for note {NoteId} deleted successfully.", attachmentId, noteId);
                return NoContent();
            }
            catch (Exception ex)
            {
                string payload = JsonSerializer.Serialize(new { noteId, attachmentId });
                _loggingEvents.TrackException(ex, payload);
                _logger.LogError(ex, "Failed to delete attachment {AttachmentId} for note {NoteId}.", attachmentId, noteId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete attachment blob.");
            }
        }

        /// <summary>
        /// Retrieves an attachment blob by its identifier.
        /// </summary>
        /// <param name="noteId">The note identifier.</param>
        /// <param name="attachmentId">The attachment identifier.</param>
        /// <returns>A <see cref="FileStreamResult"/> if found; otherwise, a not found response.</returns>
        [HttpGet("/notes/{noteId}/attachments/{attachmentId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAttachmentById(string noteId, string attachmentId)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(attachmentId))
            {
                return BadRequest("NoteId and attachmentId must be provided.");
            }

            if (!Guid.TryParse(noteId, out Guid noteGuid))
            {
                return BadRequest("Invalid noteId format.");
            }
            var note = await _context.Notes.FindAsync(noteGuid);
            if (note == null)
            {
                _logger.LogWarning("Note not found: {NoteId}", noteId);
                return NotFound();
            }

            string connectionString = Configuration.GetConnectionString("DefaultStorageConnection")!;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }

            string containerName = noteId.ToLowerInvariant();
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);
            bool blobExists = await blobClient.ExistsAsync();
            if (!blobExists)
            {
                _logger.LogWarning("Attachment not found: {AttachmentId} in note {NoteId}", attachmentId, noteId);
                return NotFound();
            }

            BlobDownloadInfo downloadInfo = await blobClient.DownloadAsync();
            return File(downloadInfo.Content, downloadInfo.ContentType, attachmentId);
        }

        /// <summary>
        /// Retrieves all attachment blobs for a specific note.
        /// </summary>
        /// <param name="noteId">The note identifier.</param>
        /// <returns>A list of attachment metadata objects.</returns>
        [HttpGet("/notes/{noteId}/attachments")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllAttachments(string noteId)
        {
            if (string.IsNullOrWhiteSpace(noteId))
            {
                return BadRequest("NoteId must be provided.");
            }

            if (!Guid.TryParse(noteId, out Guid noteGuid))
            {
                return BadRequest("Invalid noteId format.");
            }
            var note = await _context.Notes.FindAsync(noteGuid);
            if (note == null)
            {
                _logger.LogWarning("Note not found: {NoteId}", noteId);
                return NotFound();
            }

            string connectionString = Configuration.GetConnectionString("DefaultStorageConnection")!;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }

            string containerName = noteId.ToLowerInvariant();
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            List<object> attachments = new List<object>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                attachments.Add(new
                {
                    attachmentId = blobItem.Name,
                    contentType = blobItem.Properties.ContentType,
                    createdDate = blobItem.Properties.CreatedOn,
                    lastModifiedDate = blobItem.Properties.LastModified,
                    length = blobItem.Properties.ContentLength
                });
            }

            return Ok(attachments);
        }
    }
}
