using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Ipc;
using ImGuiNET;
using PatMeHonorific.Utils;

namespace PatMeHonorific.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Config Config { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }
    private ImGuiHelper ImGuiHelper { get; init; } = new();

    public ConfigWindow(Config configuration, ICallGateSubscriber<int, object> clearCharacterTitle) : base("Config Window##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(554, 410),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Config = configuration;
        ClearCharacterTitle = clearCharacterTitle;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled###enabled", ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
        }

        foreach (var emote in Enum.GetValues<Emote>())
        {
            var emoteConfig = Config.Emotes[emote];

            if (ImGui.CollapsingHeader($"{emote}###{emote}Header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                using (ImRaii.PushIndent())
                {
                    var emoteEnabled = emoteConfig.Enabled;
                    if (ImGui.Checkbox($"Enabled###{emote}enabled", ref emoteEnabled))
                    {
                        emoteConfig.Enabled = emoteEnabled;
                        Config.Save();
                    }

                    ImGui.SameLine();
                    ImGui.PushItemWidth(300);

                    var titleTemplate = emoteConfig.TitleTemplate;
                    if (ImGui.InputText($"Title template###{emote}titleDataJsonInput", ref titleTemplate, 255))
                    {
                        emoteConfig.TitleTemplate = titleTemplate;
                        Config.Save();
                    }
                    ImGuiComponents.HelpMarker("Use {0} as placeholder for count");

                    var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());
                    var color = emoteConfig.Color;
                    if (ImGuiHelper.DrawColorPicker($"Color###{emote}color", ref color, checkboxSize))
                    {
                        emoteConfig.Color = color;
                        Config.Save();
                    }

                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();
                    var glow = emoteConfig.Glow;
                    if (ImGuiHelper.DrawColorPicker($"Glow###{emote}glow", ref glow, checkboxSize))
                    {
                        emoteConfig.Glow = glow;
                        Config.Save();
                    }

                    ImGui.SameLine();
                    var isPrefix = emoteConfig.IsPrefix;
                    if (ImGui.Checkbox($"Prefix###{emote}prefix", ref isPrefix))
                    {
                        emoteConfig.IsPrefix = isPrefix;
                        Config.Save();
                    }
                }  
            }
        }

        ImGui.NewLine();
        var autoClearTitleInterval = Config.AutoClearTitleInterval;
        if (ImGui.InputInt("Auto clear title interval in secs##autoClearTitleIntervalInput", ref autoClearTitleInterval))
        {
            Config.AutoClearTitleInterval = autoClearTitleInterval;
            Config.Save();
        }

        ImGui.NewLine();
        if (ImGui.Button("Clear title##clearCharacterTitleButton"))
        {
            ClearCharacterTitle.InvokeAction(0);
        }
    }
}
