using System;

namespace PatMeHonorific.Emotes;

public record EmoteCounterKey : IEquatable<EmoteCounterKey>
{
    public ushort EmoteId { get; set; }
    public EmoteDirection Direction { get; set; }
    public ulong CharacterId { get; set; }
}
