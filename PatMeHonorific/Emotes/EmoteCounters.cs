using Newtonsoft.Json;
using System.Collections.Generic;

namespace PatMeHonorific.Emotes;

[JsonArray]
public class EmoteCounters<T> : Dictionary<EmoteCounterKey, T>
{
}
