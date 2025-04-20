using Dalamud.Plugin;
using Newtonsoft.Json;
using System.IO;

namespace PatMeHonorific.Interop;

public class PatMeConfig
{
    public class Config
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

    public Config? Parsed { get; init; }

    public PatMeConfig(IDalamudPluginInterface pluginInterface)
    {
        var pluginConfigsDirectory = Path.GetFullPath(Path.Combine(pluginInterface.GetPluginConfigDirectory(), ".."));

        // %appdata%\xivlauncher\pluginConfigs\PatMe.json
        var patMeConfigPath = Path.Combine(pluginConfigsDirectory, "PatMe.json");

        if (Path.Exists(patMeConfigPath))
        {
            using StreamReader patMeConfigFile = new(patMeConfigPath);
            var patMeConfigJson = patMeConfigFile.ReadToEnd();
            Parsed = JsonConvert.DeserializeObject<Config>(patMeConfigJson)!;
        }
    }
}
