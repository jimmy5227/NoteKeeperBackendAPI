namespace HW4NoteKeeper.DataTransferObjects
{
    /// <summary>
    /// Represents the data transfer object for file uploads.
    /// </summary>
    public class FileUploadDto
    {
        /// <summary>
        /// Gets or sets the file data to be uploaded.
        /// </summary>
        public IFormFile FileData { get; set; } = null!;
    }
}
