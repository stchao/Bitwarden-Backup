using System.Text.Json.Serialization;

namespace Bitwarden_Backup.Models
{
    internal class BitwardenResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public Data? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    internal class Data
    {
        [JsonPropertyName("noColor")]
        public bool NoColor { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("raw")]
        public string Raw { get; set; } = string.Empty;
    }
}
