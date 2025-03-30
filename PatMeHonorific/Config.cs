using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace PatMeHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    class TitleDataJsonObject
    {
        public string Title { get; set; } = string.Empty;
        public bool IsPrefix { get; set; }
        public Vector3 Color { get; set; }
        public Vector3 Glow { get; set; }
    }

    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    [Obsolete("TitleDataJson split into multiple fields in version 1")]
    public string TitleDataJson { get; set; } = """{"Title": "Pat Counter {0}", "IsPrefix": true,"IsOriginal": false,"Color": null,"Glow": null}""";
    
    public string TitleTemplate { get; set; } = "Pat Counter {0}";
    public bool IsPrefix { get; set; } = false;
    public Vector3? Color { get; set; }
    public Vector3? Glow { get; set; }

    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    public void MaybeMigrateToV1()
    {
        if (Version < 1)
        {
            var titleDataJsonObject = JsonConvert.DeserializeObject<TitleDataJsonObject>(TitleDataJson);
            TitleTemplate = titleDataJsonObject.Title;
            IsPrefix = titleDataJsonObject.IsPrefix;
            Color = titleDataJsonObject.Color;
            Glow = titleDataJsonObject.Glow;
            Version = 1;
            Save();
        }
    }
}
