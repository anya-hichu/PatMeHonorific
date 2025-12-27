using Newtonsoft.Json;

namespace PatMeHonorific.Configs.PatMe;

public class PatMeConfig
{
    [JsonProperty(Required = Required.Always)]
    public EmoteDataConfig[] EmoteData { get; set; } = [];
}
