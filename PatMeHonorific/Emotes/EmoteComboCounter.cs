using System;

namespace PatMeHonorific.Emotes;

public class EmoteComboCounter
{
    private static readonly TimeSpan DURATION = TimeSpan.FromSeconds(5);

    private DateTimeOffset? LastOccurrenceAt { get; set; }

    private uint Count { get; set; } = 0;

    public void Increment()
    {
        Count = HasExpired() ? 1 : Count + 1; 
        LastOccurrenceAt = DateTimeOffset.UtcNow;
    }

    public uint Get() => Count;

    public void Add(EmoteComboCounter other)
    {
        LastOccurrenceAt = LastOccurrenceAt > other.LastOccurrenceAt ? LastOccurrenceAt : other.LastOccurrenceAt;
        Count = HasExpired() ? 0 : Count + other.Count;
    }

    private bool HasExpired() => LastOccurrenceAt.HasValue && DateTimeOffset.UtcNow.Subtract(LastOccurrenceAt.Value) > DURATION;
}
