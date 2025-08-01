﻿using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PlayerHighGravityConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("gravity_scale")] public float GravityScale { get; set; } = 4f;
    }
}
