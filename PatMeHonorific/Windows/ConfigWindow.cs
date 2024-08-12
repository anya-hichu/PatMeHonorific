using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Ipc;
using ImGuiNET;

namespace PatMeHonorific.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration { get; init; }
    private ICallGateSubscriber<int, string> GetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }
    private string CharacterTitle { get; set; } = string.Empty;

    public ConfigWindow(Configuration configuration, ICallGateSubscriber<int, string> getCharacterTitle, ICallGateSubscriber<int, object> clearCharacterTitle) : base("Config Window##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(130, 250),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = configuration;
        GetCharacterTitle = getCharacterTitle;
        ClearCharacterTitle = clearCharacterTitle;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = Configuration.Enabled;
        if (ImGui.Checkbox("Enabled##enabledCheckbox", ref enabled))
        {
            Configuration.Enabled = enabled;
            Configuration.Save();
        }

        var characterIndex = Configuration.CharacterIndex;
        if (ImGui.InputInt("Character index##characterIndexInput", ref characterIndex))
        {
            Configuration.CharacterIndex = characterIndex;
            Configuration.Save();
        }

        ImGui.Text("Current title: ");
        ImGui.SameLine();
        ImGui.Text(CharacterTitle);
        if (ImGui.Button("Refresh##refreshButton"))
        {
            RefreshCharacterTitle();
        }

        var titleDataJson = Configuration.TitleDataJson;
        if (ImGui.InputText("Title data json##titleDataJsonInput",  ref titleDataJson, 255))
        {
            Configuration.TitleDataJson = titleDataJson;
            Configuration.Save();
        }

        if (!Configuration.TitleDataJsonValid())
        {
            ImGui.Text("Invalid Json Format");
        }

        if (ImGui.Button("Clear title##clearCharacterTitleButton"))
        {
            ClearCharacterTitle.InvokeAction(characterIndex);
        }

        var autoClearTitleInterval = Configuration.AutoClearTitleInterval;
        if (ImGui.InputInt("Auto clear title interval in secs##autoClearTitleIntervalInput", ref autoClearTitleInterval))
        {
            Configuration.AutoClearTitleInterval = autoClearTitleInterval;
            Configuration.Save();
        }
    }

    private void RefreshCharacterTitle()
    {
        CharacterTitle = GetCharacterTitle.InvokeFunc(Configuration.CharacterIndex);
    }
}
