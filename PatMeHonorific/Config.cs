using Dalamud.Configuration;
using Newtonsoft.Json;
using PatMeHonorific.Configs;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PatMeHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly Dictionary<ulong, Emote> EMOTE_ID_TO_EMOTE = new()
    {
        { 105, Emote.Pet },
        { 146, Emote.Dote },
        { 147, Emote.Dote },
        { 112, Emote.Hug },
        { 113, Emote.Hug }
    };

    public static readonly int LATEST = 2;

    public int Version { get; set; } = LATEST;

    public bool Enabled { get; set; } = true;

    public Dictionary<Emote, EmoteConfig> Emotes { get; init; } = [];

    #region deprecated
    [Obsolete("TitleDataJson split into multiple fields in version 1")]
    class TitleDataJsonObject
    {
        public string Title { get; set; } = string.Empty;
        public bool IsPrefix { get; set; }
        public Vector3 Color { get; set; }
        public Vector3 Glow { get; set; }
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
    #endregion

    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    public void MaybeMigrate()
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

            Version = LATEST;
            Save();
        }  
    }
}
