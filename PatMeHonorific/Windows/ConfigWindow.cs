using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Ipc;
using ImGuiNET;
using PatMeHonorific.Utils;

namespace PatMeHonorific.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Config Config { get; init; }
    private ICallGateSubscriber<int, string> GetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }
    private ImGuiHelper ImGuiHelper { get; init; } = new();

    public ConfigWindow(Config configuration, ICallGateSubscriber<int, string> getCharacterTitle, ICallGateSubscriber<int, object> clearCharacterTitle) : base("Config Window##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(130, 250),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Config = configuration;
        GetCharacterTitle = getCharacterTitle;
        ClearCharacterTitle = clearCharacterTitle;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled##enabledCheckbox", ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
        }

        var titleTemplate = Config.TitleTemplate;
        if (ImGui.InputText("Title template##titleDataJsonInput", ref titleTemplate, 255))
        {
            Config.TitleTemplate = titleTemplate;
            Config.Save();
        }
        ImGuiComponents.HelpMarker("Use {0} as placeholder for count");

        var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());
        var color = Config.Color;
        if (ImGuiHelper.DrawColorPicker($"Color###color", ref color, checkboxSize))
        {
            Config.Color = color;
            Config.Save();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        var glow = Config.Glow;
        if (ImGuiHelper.DrawColorPicker($"Glow###glow", ref glow, checkboxSize))
        {
            Config.Glow = glow;
            Config.Save();
        }

        var isPrefix = Config.IsPrefix;
        if (ImGui.Checkbox($"Prefix###prefix", ref isPrefix))
        {
            Config.IsPrefix = isPrefix;
            Config.Save();
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
