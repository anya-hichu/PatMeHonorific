using System;
using System.Numerics;

namespace PatMeHonorific.Configs;

[Serializable]
public class EmoteConfig
{
    public bool Enabled { get; set; } = true;
    public string TitleTemplate { get; set; } = string.Empty;
    public bool IsPrefix { get; set; } = false;
    public Vector3? Color { get; set; }
    public Vector3? Glow { get; set; }
}
