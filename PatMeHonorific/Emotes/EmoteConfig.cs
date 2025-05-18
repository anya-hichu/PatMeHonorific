using System;
using System.Collections.Generic;
using System.Numerics;

namespace PatMeHonorific.Emotes;

[Serializable]
public class EmoteConfig
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = string.Empty;

    public int Priority { get; set; } = 0;

    public string TitleTemplate { get; set; } = string.Empty;
    public bool IsPrefix { get; set; } = false;
    public Vector3? Color { get; set; }
    public Vector3? Glow { get; set; }

    public HashSet<ushort> EmoteIds { get; set; } = [];
    
    public EmoteDirection Direction { get; set; } = EmoteDirection.Receiving;

    public HashSet<ulong> CharacterIds { get; set; } = [];

    public EmoteConfig Clone()
    {
        return (EmoteConfig)MemberwiseClone();
    }
}
