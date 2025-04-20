using System;

namespace PatMeHonorific.Emotes;

public class EmoteCounterKey : IEquatable<EmoteCounterKey>
{
    public ushort EmoteId { get; set; }
    public EmoteDirection Direction { get; set; }
    public ulong CharacterId { get; set; }

    public override int GetHashCode()
    {
        return EmoteId.GetHashCode() ^ Direction.GetHashCode() ^ CharacterId.GetHashCode();
    }

    public override bool Equals(object? other)
    {
        return other != null && other is EmoteCounterKey otherKey && Equals(otherKey);
    }

    public bool Equals(EmoteCounterKey? other)
    {
        return other != null && EmoteId == other.EmoteId && Direction == other.Direction && CharacterId == other.CharacterId;
    }
}
