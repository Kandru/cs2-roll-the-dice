using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class BankruptConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
