using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using HW1NoteKeeper.Settings;
using NJsonSchema;
using System.Text.Json;

namespace HW1NoteKeeper.Controllers
{
    /// <summary>
    /// Controller responsible for managing note-related API operations.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class NoteController : ControllerBase
    {
        private readonly ILogger<NoteController> _logger;
        private readonly IChatClient _chatClient;
        private readonly AISettings _aISettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteController"/> class.
        /// </summary>
        public NoteController(IChatClient chatClient,
                              AISettings aISettings,
                              ILogger<NoteController> logger)
        {
            _logger = logger;
            _chatClient = chatClient;
            _aISettings = aISettings;
        }

        public class TagsResponse
        {
            public List<string> Phrases { get; set; } = [];
        }

        /// <summary>
        /// Creates a new note and generates tags from the details.
        /// </summary>
        [HttpPost("/notes")]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Length > 60 ||
                string.IsNullOrWhiteSpace(request.Details) || request.Details.Length > 1024)
            {
                return BadRequest("Invalid summary or details. Check constraints.");
            }

            var note = new Note
            {
                NoteId = Guid.NewGuid(),
                Summary = request.Summary,
                Details = request.Details,
                CreatedDateUtc = DateTime.UtcNow,
                ModifiedDateUtc = null,
                Tags = await GenerateTags(request.Details)
            };

            NotesStorage.AddNote(note);
            _logger.LogInformation($"Note created with ID: {note.NoteId}");

            return CreatedAtAction(nameof(GetNoteById), new { id = note.NoteId }, note);
        }

        /// <summary>
        /// Updates an existing note. Tags regenerate only if details change.
        /// </summary>
        [HttpPatch("/notes/{id}")]
        public async Task<IActionResult> UpdateNote(Guid id, [FromBody] UpdateNoteRequest request)
        {
            var note = NotesStorage.GetNoteById(id);
            if (note == null) return NotFound();

            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(request.Summary) && request.Summary.Length <= 60)
            {
                note.Summary = request.Summary;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Details) && request.Details.Length <= 1024 && request.Details != note.Details)
            {
                note.Details = request.Details;
                note.Tags = await GenerateTags(request.Details);
                isUpdated = true;
            }

            if (isUpdated)
            {
                note.ModifiedDateUtc = DateTime.UtcNow;
                NotesStorage.UpdateNote(note);
                _logger.LogInformation($"Note with ID: {note.NoteId} updated");
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a note by ID.
        /// </summary>
        [HttpDelete("/notes/{id}")]
        public IActionResult DeleteNote(Guid id)
        {
            var note = NotesStorage.GetNoteById(id);
            if (note == null) return NotFound();

            NotesStorage.DeleteNote(id);
            _logger.LogInformation($"Note with ID: {id} deleted");

            return NoContent();
        }

        /// <summary>
        /// Retrieves a note by ID.
        /// </summary>
        [HttpGet("/notes/{id}")]
        public IActionResult GetNoteById(Guid id)
        {
            var note = NotesStorage.GetNoteById(id);
            if (note == null) return NotFound();

            var response = new
            {
                noteId = note.NoteId.ToString(),
                summary = note.Summary,
                details = note.Details,
                createdDateUtc = note.CreatedDateUtc.ToString("o"),
                modifiedDateUtc = note.ModifiedDateUtc?.ToString("o"),
                tags = note.Tags
            };

            return Ok(response);
        }

        /// <summary>
        /// Retrieves all notes.
        /// </summary>
        [HttpGet("/notes")]
        public IActionResult GetAllNotes()
        {
            var notes = NotesStorage.GetAllNotes();

            var response = notes.Select(note => new
            {
                noteId = note.NoteId,
                summary = note.Summary,
                details = note.Details,
                createdDateUtc = note.CreatedDateUtc.ToString("o"),
                modifiedDateUtc = note.ModifiedDateUtc?.ToString("o"),
                tags = note.Tags
            });

            return Ok(response);
        }

        /// <summary>
        /// Generates relevant tags for a note using AI.
        /// </summary>
        private async Task<List<string>> GenerateTags(string details)
        {
            JsonSchema schema = JsonSchema.FromType<TagsResponse>();
            string jsonSchemaString = schema.ToJson();

            JsonElement jsonSchemaElement = JsonDocument.Parse(jsonSchemaString).RootElement;
            ChatResponseFormatJson chatResponseFormatJson = ChatResponseFormat.ForJsonSchema(jsonSchemaElement, "ChatResponse", "Chat response schema");

            ChatOptions chatOptions = new ChatOptions()
            {
                Temperature = _aISettings.Temperature,
                TopP = _aISettings.TopP,
                MaxOutputTokens = _aISettings.MaxOutputTokens,
                ResponseFormat = chatResponseFormatJson
            };

            string enhancedPrompt = $"Generate a JSON output of only 2, one-word tag based on: {details}";

            try
            {
                ChatCompletion responseCompletion = await _chatClient.CompleteAsync(enhancedPrompt, options: chatOptions);
                var response = JsonSerializer.Deserialize<TagsResponse>(responseCompletion.Message.Text!);
                return response?.Phrases ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateTags");
                return new List<string>();
            }
        }
    }
}
