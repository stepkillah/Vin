using System.Text.Json.Serialization;

namespace DaleNewman
{
    public record CodeValue
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
