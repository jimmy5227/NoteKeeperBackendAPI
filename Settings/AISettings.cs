namespace HW3NoteKeeper.Settings
{
    /// <summary>
    /// Represents configuration settings for integrating with the AI service.
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Gets or sets the deployment URI for the AI service.
        /// </summary>
        public required string DeploymentUri { get; set; }

        /// <summary>
        /// Gets or sets the API key used for authenticating with the AI service.
        /// </summary>
        public required string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the deployment model name for the AI service.
        /// Default value is "gpt-4o-mini".
        /// </summary>
        public string DeploymentModelName { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// Gets or sets the temperature for the AI responses.
        /// A higher value results in more random outputs.
        /// Default value is 0.7.
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets the probability threshold (TopP) for AI responses.
        /// A value of 1.0 uses the full probability distribution.
        /// Default value is 1.0.
        /// </summary>
        public float TopP { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the maximum number of tokens allowed in the AI response.
        /// Default value is 500.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 500;
    }
}
