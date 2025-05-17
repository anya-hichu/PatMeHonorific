using Dalamud.Plugin;
using Newtonsoft.Json;
using PatMeHonorific.Emotes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PatMeHonorific.Interop;

public class PatMeConfig
{
    public static readonly Dictionary<string, HashSet<ushort>> EMOTE_NAME_TO_IDS = new()
    {
        { "Pet", [105] },
        { "Dote", [146] },
        { "Hug", [112, 113] },
    };

    public class JsonConfig
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

    public JsonConfig? Parsed { get; init; }

    public PatMeConfig(IDalamudPluginInterface pluginInterface)
    {
        var pluginConfigsDirectory = Path.GetFullPath(Path.Combine(pluginInterface.GetPluginConfigDirectory(), ".."));

        // %appdata%\xivlauncher\pluginConfigs\PatMe.json
        var patMeConfigPath = Path.Combine(pluginConfigsDirectory, "PatMe.json");

        if (Path.Exists(patMeConfigPath))
        {
            using StreamReader patMeConfigFile = new(patMeConfigPath);
            var patMeConfigJson = patMeConfigFile.ReadToEnd();
            Parsed = JsonConvert.DeserializeObject<JsonConfig>(patMeConfigJson)!;
        }
    }

    public bool TrySync(Config config)
    {
        if (Parsed == null)
        {
            return false;
        }

        foreach (var emoteData in Parsed.EmoteData)
        {
            var characterId = emoteData.CID;
            foreach (var counter in emoteData.Counters)
            {
                var emoteIds = EMOTE_NAME_TO_IDS[counter.Name];
                var totalCounter = 0u;
                for (var i = emoteIds.Count - 1; i >= 0; i--)
                {
                    var emoteId = emoteIds.ElementAt(i);
                    var key = new EmoteCounterKey() { CharacterId = characterId, EmoteId = emoteId, Direction = EmoteDirection.Receiving };

                    if (config.Counters.TryGetValue(key, out var internalCounter))
                    {
                        totalCounter += internalCounter;
                    }

                    // Update the count of the first emote only since patme doesn't differentiate
                    if (i == 0)
                    {
                        var value = counter.Value - totalCounter;
                        if (!config.Counters.TryAdd(key, value))
                        {
                            config.Counters[key] += value;
                        }
                    }
                }
            }
        }
        return true;
    }
}
