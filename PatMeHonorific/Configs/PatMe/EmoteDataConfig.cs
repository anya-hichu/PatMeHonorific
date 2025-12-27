using Newtonsoft.Json;

namespace PatMeHonorific.Configs.PatMe;

public class EmoteDataConfig
{
    [JsonProperty(Required = Required.Always)]
    public ulong CID { get; set; }

    [JsonProperty(Required = Required.Always)]
    public CounterConfig[] Counters { get; set; } = [];

    public override string ToString() => $"{nameof(EmoteDataConfig)} {{ {nameof(CID)} = {CID}, {nameof(Counters)} = [\n\t{string.Join(",\n\t", [.. Counters])}\n] }}";
}
