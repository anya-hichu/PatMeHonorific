using Dalamud.Configuration;
using PatMeHonorific.Emotes;
using System;
using System.Collections.Generic;

namespace PatMeHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly int LATEST = 3;

    public int Version { get; set; } = LATEST;

    public bool Enabled { get; set; } = true;

    public List<EmoteConfig> EmoteConfigs { get; init; } = [];

    public EmoteCounters Counters { get; set; } = [];
    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
