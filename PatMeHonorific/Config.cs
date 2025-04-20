using Dalamud.Configuration;
using Newtonsoft.Json;
using PatMeHonorific.Emotes;
using PatMeHonorific.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PatMeHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly int LATEST = 3;

    public int Version { get; set; } = LATEST;

    public bool Enabled { get; set; } = true;

    public List<EmoteConfig> EmoteConfigs { get; init; } = [];

    public EmoteCounters Counters { get; set; } = [];

    #region deprecated
    [Obsolete("Configurable ids in config version 3")]
    public static readonly Dictionary<ushort, Emote> EMOTE_ID_TO_EMOTE = new()
    {
        { 105, Emote.Pet },
        { 146, Emote.Dote },
        { 147, Emote.Dote },
        { 112, Emote.Hug },
        { 113, Emote.Hug }
    };

    [Obsolete("Configurable ids in config version 3")]
    public Dictionary<Emote, EmoteConfig> Emotes { get; init; } = [];

    [Obsolete("TitleDataJson split into multiple fields in version 1")]
    class TitleDataJsonObject
    {
        public string Title { get; set; } = string.Empty;
        public bool IsPrefix { get; set; }
        public Vector3? Color { get; set; }
        public Vector3? Glow { get; set; }
    }

    [Obsolete("TitleDataJson split into multiple fields in version 1")]
    public string TitleDataJson { get; set; } = """{"Title": "Pat Counter {0}", "IsPrefix": true,"IsOriginal": false,"Color": null,"Glow": null}""";

    [Obsolete("Add support for multiple emotes in version 2")]
    public string TitleTemplate { get; set; } = "Pat Counter {0}";

    [Obsolete("Add support for multiple emotes in version 2")]
    public bool IsPrefix { get; set; } = false;

    [Obsolete("Add support for multiple emotes in version 2")]
    public Vector3? Color { get; set; }

    [Obsolete("Add support for multiple emotes in version 2")]
    public Vector3? Glow { get; set; }

    public void MaybeMigrate(PatMeConfig patMeConfig)
    {
        if (Version < LATEST)
        {
            if (Version < 1)
            {
                var titleDataJsonObject = JsonConvert.DeserializeObject<TitleDataJsonObject>(TitleDataJson);
                TitleTemplate = titleDataJsonObject.Title;
                IsPrefix = titleDataJsonObject.IsPrefix;
                Color = titleDataJsonObject.Color;
                Glow = titleDataJsonObject.Glow;
                TitleDataJson = string.Empty;
            }

            if (Version < 2)
            {
                Emotes.Add(Emote.Pet, new()
                {
                    Enabled = true,
                    TitleTemplate = TitleTemplate,
                    IsPrefix = IsPrefix,
                    Color = Color,
                    Glow = Glow
                });
                Emotes.Add(Emote.Dote, new()
                {
                    Enabled = true,
                    TitleTemplate = "Dote Counter {0}",
                    IsPrefix = IsPrefix,
                    Color = Color,
                    Glow = Glow
                });
                Emotes.Add(Emote.Hug, new()
                {
                    Enabled = true,
                    TitleTemplate = "Hug Counter {0}",
                    IsPrefix = IsPrefix,
                    Color = Color,
                    Glow = Glow
                });

                TitleTemplate = string.Empty;
                IsPrefix = false;
                Color = null;
                Glow = null;
            }

            if (Version < 3)
            {
                var parsed = patMeConfig.Parsed;
                if (parsed != null)
                {
                    foreach (var emoteData in parsed.EmoteData)
                    {
                        var characterId = emoteData.CID;
                        foreach (var counter in emoteData.Counters)
                        {
                            // Register to first match only
                            var pair = EMOTE_ID_TO_EMOTE.First(e => e.Value.ToString() == counter.Name);
                            Counters.Add(new() { CharacterId = characterId, EmoteId = pair.Key, Direction = EmoteDirection.Receiving }, counter.Value);
                        }
                    }
                }

                foreach (var emotePair in Emotes)
                {
                    var emoteConfig = emotePair.Value;
                    EmoteConfigs.Add(new()
                    {
                        Enabled = emoteConfig.Enabled,
                        Name = $"Receiving {emotePair.Key}",
                        TitleTemplate = emoteConfig.TitleTemplate,
                        IsPrefix = emoteConfig.IsPrefix,
                        Color = emoteConfig.Color,
                        Glow = emoteConfig.Glow,
                        EmoteIds = [.. EMOTE_ID_TO_EMOTE.Where(pair => pair.Value == emotePair.Key).Select(p => p.Key)],
                        Direction = EmoteDirection.Receiving
                    });
                }
                Emotes.Clear();
                TitleDataJson = string.Empty;
            }

            Version = LATEST;
            Save();
        }
    }
    #endregion

    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
