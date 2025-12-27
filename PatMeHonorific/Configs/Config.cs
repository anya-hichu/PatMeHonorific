using Dalamud.Configuration;
using PatMeHonorific.Emotes;
using System;
using System.Collections.Generic;

namespace PatMeHonorific.Configs;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly int CURRENT_VERSION = 4;

    public int Version { get; set; } = CURRENT_VERSION;

    public bool Enabled { get; set; } = true;

    public List<EmoteConfig> EmoteConfigs { get; init; } = [];

    public EmoteCounters<uint> Counters { get; set; } = [];

    [Obsolete("Changed to AutoClearDelayMs in version 4")]
    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public ushort AutoClearDelayMs { get; set; } = 5000;

    public bool IsHonorificSupporter { get; set; } = false;
}
