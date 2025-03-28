namespace HW1NoteKeeper.Settings
{
    public class AISettings
    {
        public required string DeploymentUri { get; set; }
        public required string ApiKey { get; set; }
        public string DeploymentModelName { get; set; } = "gpt-4o-mini";
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 1.0f;
        public int MaxOutputTokens { get; set; } = 500;
    }
}
