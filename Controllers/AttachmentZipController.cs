using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using HW4NoteKeeper.Data;
using HW4NoteKeeper.Services;

namespace HW4NoteKeeper.Controllers
{
    /// <summary>
    /// Controller for handling zip archive creation of all attachments for a note.
    /// </summary>
    [ApiController]
    [Route("notes/{noteId}/attachmentzipfiles")]
    public class NoteAttachmentZipFilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NoteAttachmentZipFilesController> _logger;
        private readonly IQueueService _queueService;

        public NoteAttachmentZipFilesController(ApplicationDbContext context,
                                                IConfiguration configuration,
                                                ILogger<NoteAttachmentZipFilesController> logger,
                                                IQueueService queueService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _queueService = queueService;
        }

        /// <summary>
        /// Requests the creation of a zip archive containing all attachments associated with a note.
        /// </summary>
        /// <param name="noteId">The note identifier.</param>
        /// <returns>
        /// 202 Accepted with a Location header if the zip creation request is successfully enqueued,
        /// 204 No Content if there are no attachments,
        /// 404 Not Found if the note does not exist,
        /// or 400 Bad Request for invalid parameters.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAttachmentZip(string noteId)
        {
            // Validate noteId parameter.
            if (string.IsNullOrWhiteSpace(noteId))
            {
                return BadRequest("NoteId must be provided.");
            }

            // Validate that noteId is a valid GUID.
            if (!Guid.TryParse(noteId, out Guid noteGuid))
            {
                return BadRequest("Invalid noteId format.");
            }

            // Check if the note exists.
            var note = await _context.Notes.FindAsync(noteGuid);
            if (note == null)
            {
                _logger.LogWarning("Note not found: {NoteId}", noteId);
                return NotFound();
            }

            // Retrieve the storage connection string from configuration.
            string connectionString = _configuration.GetConnectionString("DefaultStorageConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Storage connection string is not configured.");
            }

            // Use the noteId (in lowercase) as the container name.
            string containerName = noteId.ToLowerInvariant();
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            // Ensure the container exists and check for attachments.
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            bool hasAttachments = false;
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                hasAttachments = true;
                break;
            }

            // If no attachments are found, return 204 No Content.
            if (!hasAttachments)
            {
                return NoContent();
            }

            // Generate a new zipFileId: a new GUID with a ".zip" suffix.
            var zipFileId = $"{Guid.NewGuid()}.zip";

            // Create the message payload to enqueue.
            var message = new
            {
                noteId = noteId,
                zipFileId = zipFileId
            };

            // Enqueue the message to the "attachment-zip-requests" queue.
            // Note: Race conditions where a note is deleted after this check should be handled
            // in the background processing logic.
            bool isEnqueued = await _queueService.EnqueueMessageAsync("attachment-zip-requests", message);
            if (!isEnqueued)
            {
                _logger.LogError("Failed to enqueue zip creation request for note {NoteId}", noteId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to enqueue zip creation request.");
            }

            // Construct the URL to retrieve the zip file blob.
            // The app service name can be stored in configuration; here we use "noteswithattachments" as fallback.
            string appServiceName = _configuration["AppServiceName"] ?? "noteswithattachments";
            var blobUrl = $"https://{appServiceName}.azurewebsites.net/blobs/{zipFileId}";

            // Return HTTP 202 Accepted with the Location header pointing to the blob URL.
            return Accepted(blobUrl);
        }
    }
}
