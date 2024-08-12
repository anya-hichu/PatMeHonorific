using Dalamud.Configuration;
using System;
using System.Text.Json;

namespace PatMeHonorific;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;
    public int CharacterIndex { get; set; } = 0;
    public string TitleDataJson { get; set; } = """{"Title": "Pat Counter {0}", "IsPrefix": true,"IsOriginal": false,"Color": null,"Glow": null}""";
    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    public bool TitleDataJsonValid()
    {
        try 
        { 
            return JsonDocument.Parse(TitleDataJson) != null; 
        } 
        catch (JsonException)
        { 
            return false; 
        }
    }
}
