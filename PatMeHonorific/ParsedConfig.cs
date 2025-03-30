using Dalamud.Plugin;
using Newtonsoft.Json;
using System.IO;

namespace PatMeHonorific;

public class ParsedConfig
{
    public class PatMeConfig
    {
        [JsonProperty(Required = Required.Always)]
        public EmoteDataConfig[] EmoteData { get; set; } = [];
    }

    public class EmoteDataConfig
    {
        [JsonProperty(Required = Required.Always)]
        public ulong CID { get; set; }

        [JsonProperty(Required = Required.Always)]
        public CounterConfig[] Counters { get; set; } = [];
    }

    public class CounterConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(Required = Required.Always)]
        public uint Value { get; set; }
    }

    private string PluginConfigsDirectory { get; init; }
    public PatMeConfig Data { get; init; } = new();

    public ParsedConfig(IDalamudPluginInterface pluginInterface)
    {
        PluginConfigsDirectory = Path.GetFullPath(Path.Combine(pluginInterface.GetPluginConfigDirectory(), ".."));

        // %appdata%\xivlauncher\pluginConfigs\PatMe.json
        var patMeConfigPath = Path.Combine(PluginConfigsDirectory, "PatMe.json");

        if (Path.Exists(patMeConfigPath))
        {
            using StreamReader patMeConfigFile = new(patMeConfigPath);
            var patMeConfigJson = patMeConfigFile.ReadToEnd();
            Data = JsonConvert.DeserializeObject<PatMeConfig>(patMeConfigJson)!;
        }
    }
}
