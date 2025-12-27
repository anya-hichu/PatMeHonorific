using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel;
using PatMeHonorific.Configs;
using PatMeHonorific.Emotes;
using PatMeHonorific.Interop;
using PatMeHonorific.Utils;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Emote = Lumina.Excel.Sheets.Emote;

namespace PatMeHonorific.Windows;

public class ConfigWindow : Window
{
    private static readonly string CONFIRM_DELETE_HINT = "Press CTRL while clicking to confirm";

    private Config Config { get; init; }
    private PatMeSynchronizer PatMeSynchronizer { get; init; }
    private IPlayerState PlayerState { get; init; }
    private IDalamudPluginInterface PluginInterface { get; init; }
    private IPluginLog PluginLog { get; init; }

    private Dictionary<ushort, HashSet<string>> CommandsByEmoteId { get; init; }
    private CustomImGui CustomImGui { get; init; } = new();

    public ConfigWindow(Config config, ExcelSheet<Emote> emoteSheet, PatMeSynchronizer patMeSynchronizer, IPlayerState playerState, IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) : base("PatMeHonorific - Config##configWindow")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(850, 435),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };
     
        Config = config;
        PlayerState = playerState;
        PatMeSynchronizer = patMeSynchronizer;
        PluginInterface = pluginInterface;
        PluginLog = pluginLog;

        CommandsByEmoteId = emoteSheet.Where(s => s.TextCommand.IsValid).ToDictionary(s => Convert.ToUInt16(s.RowId), s => {
            var textCommand = s.TextCommand.Value;
            var commands = new HashSet<string>();
            if (!textCommand.Alias.IsEmpty)
            {
                commands.Add(textCommand.Alias.ToString());
            }
            commands.Add(textCommand.Command.ToString());
            return commands;
        });
    }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled###enabled", ref enabled))
        {
            Config.Enabled = enabled;
            SaveConfig();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 545);

        var autoClearDelayMs = Config.AutoClearDelayMs;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputUShort("Auto-Clear (ms)###autoClearDelayMs", ref autoClearDelayMs, 50))
        {
            Config.AutoClearDelayMs = autoClearDelayMs;
            SaveConfig();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var isHonorificSupporter = Config.IsHonorificSupporter;
        if (ImGui.Checkbox("Honorific Supporter##isHonorificSupporter", ref isHonorificSupporter))
        {
            Config.IsHonorificSupporter = isHonorificSupporter;
            SaveConfig();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Only check if supporting Honorific author via https://ko-fi.com/Caraxi, it gives access to extra features");

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        if (ImGui.Button("Sync###sync"))
        {
            if(PatMeSynchronizer.TryUpdate(Config))
            {
                SaveConfig();
                PluginLog.Info("Successfully synced with patme");
            } 
            else
            {
                PluginLog.Error("Failed to sync with patme");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Overrides internal counters with PatMe ones\nCounters can be displayed using: /patmehonorific info");
        }

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
        {
            if (ImGui.Button("Delete All###generatorConfigsDeleteAll") && ImGui.GetIO().KeyCtrl)
            {
                Config.EmoteConfigs.Clear();
                SaveConfig();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(CONFIRM_DELETE_HINT);
            }
        }


        if (ImGui.Button("+###emoteConfigsNew"))
        {
            Config.EmoteConfigs.Add(new());
            SaveConfig();
        }

        ImGui.SameLine(ImGui.GetStyle().IndentSpacing * 1.5f);

        using (ImRaii.TabBar("emoteConfigsTabBar", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.ListPopupButton | ImGuiTabBarFlags.FittingPolicyScroll))
        {
            foreach (var emoteConfig in Config.EmoteConfigs.OrderByDescending(c => c.Priority))
            {
                var baseId = $"emoteConfigs{emoteConfig.GetHashCode()}";

                var name = emoteConfig.Name;
                using (var tabItem = ImRaii.TabItem($"{(name.IsNullOrWhitespace() ? "(Blank)" : name)}###{baseId}TabItem"))
                {
                    if (tabItem)
                    {
                        using (ImRaii.PushIndent())
                        {
                            var emoteEnabled = emoteConfig.Enabled;
                            if (ImGui.Checkbox($"Enabled###{baseId}Enabled", ref emoteEnabled))
                            {
                                emoteConfig.Enabled = emoteEnabled;
                                SaveConfig();
                            }

                            ImGui.SameLine(ImGui.GetWindowWidth() - 145);
                            if (ImGui.Button($"Duplicate###{baseId}Duplicate"))
                            {
                                Config.EmoteConfigs.Add(emoteConfig.Clone());
                                SaveConfig();
                            }
                            ImGui.SameLine();
                            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
                            {
                                if (ImGui.Button($"Delete###{baseId}Delete") && ImGui.GetIO().KeyCtrl)
                                {
                                    Config.EmoteConfigs.Remove(emoteConfig);
                                    SaveConfig();
                                }
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip(CONFIRM_DELETE_HINT);
                                }
                            }

                            if (ImGui.InputText($"Name###{baseId}Name", ref name, 255))
                            {
                                emoteConfig.Name = name;
                                SaveConfig();
                            }

                            var priority = emoteConfig.Priority;
                            if (ImGui.InputInt($"Priority###{baseId}Priority", ref priority, 1))
                            {
                                emoteConfig.Priority = priority;
                                SaveConfig();
                            }

                            var direction = emoteConfig.Direction;

                            var directionName = Enum.GetName(direction)!;
                            var directionNames = Enum.GetNames<EmoteDirection>().ToList();
                            var directionNameIndex = directionNames.IndexOf(directionName);

                            if (ImGui.Combo($"Direction###{baseId}Direction", ref directionNameIndex, [.. directionNames], directionNames.Count))
                            {
                                var newDirectionName = directionNames.ElementAt(directionNameIndex);
                                emoteConfig.Direction = Enum.Parse<EmoteDirection>(newDirectionName);
                                SaveConfig();
                            }

                            var emoteIds = emoteConfig.EmoteIds;
                            var joinedCommands = string.Join(',', CommandsByEmoteId.Where(p => emoteIds.Contains(p.Key)).Select(c => c.Value.FirstOrDefault()));

                            if (ImGui.InputText($"Commands###{baseId}JoinedCommands", ref joinedCommands, 255))
                            {
                                var commands = joinedCommands.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                emoteConfig.EmoteIds = [.. CommandsByEmoteId.Where(c => c.Value.Intersect(commands).Any()).Select(c => c.Key)];
                                SaveConfig();
                            }
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Comma-separated list of emote commands to track");
                            }


                            var characterIds = emoteConfig.CharacterIds;
                            var joinedCharacterIds = string.Join(',', characterIds);
                            if (ImGui.InputText($"Characters###{baseId}joinedCharacterIds", ref joinedCharacterIds, 255))
                            {
                                var rawCharacterIds = joinedCharacterIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                emoteConfig.CharacterIds = [.. rawCharacterIds.Where(s => ulong.TryParse(s, out var _)).Select(s => ulong.Parse(s))];
                                SaveConfig();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Comma-separated list of character ids, empty means all characters");
                            }
                            ImGui.SameLine();
                            if (ImGui.Button($"Add Current"))
                            {
                                emoteConfig.CharacterIds.Add(PlayerState.ContentId);
                                SaveConfig();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip($"Current: {PlayerState.CharacterName} ({PlayerState.ContentId})");
                            }

                            var titleTemplate = emoteConfig.TitleTemplate;
                            if (ImGui.InputTextMultiline($"Title Template (scriban)###{baseId}TitleTemplate", ref titleTemplate, ushort.MaxValue))
                            {
                                emoteConfig.TitleTemplate = titleTemplate;
                                SaveConfig();
                            }
                            if (ImGui.IsItemHovered() && !TryParseTemplate(titleTemplate, out var logMessageBag))
                            {
                                using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                ImGui.SetTooltip(string.Join("\n", logMessageBag));
                            }
                            ImGuiComponents.HelpMarker($"Available variables: total_count, session_count and combo_count\nSyntax reference available on https://github.com/scriban/scriban\n\nRendered title above the maximum supported length will be ignored ({Constraint.MaxTitleLength} characters)");

                            emoteConfig.TitleDataConfig ??= new();
                            DrawSettings(baseId, emoteConfig.TitleDataConfig);
                        }
                    }
                }
            }
        }
    }


    private void DrawSettings(string baseId, TitleDataConfig titleDataConfig)
    {
        var nestedId = $"{baseId}TitleDataConfig";

        var isPrefix = titleDataConfig.IsPrefix;
        if (ImGui.Checkbox($"Prefix###{nestedId}Prefix", ref isPrefix))
        {
            titleDataConfig.IsPrefix = isPrefix;
            SaveConfig();
        }
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());

        var color = titleDataConfig.Color;
        if (CustomImGui.ColorPicker($"Color###{nestedId}Color", ref color, checkboxSize))
        {
            titleDataConfig.Color = color;
            SaveConfig();
        }

        var maybeGradientColourSet = titleDataConfig.GradientColourSet;
        if (!Config.IsHonorificSupporter || !maybeGradientColourSet.HasValue)
        {
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            var glow = titleDataConfig.Glow;
            if (CustomImGui.ColorPicker($"Glow###{nestedId}Glow", ref glow, checkboxSize))
            {
                titleDataConfig.Glow = glow;
                SaveConfig();
            }
        }

        if (!Config.IsHonorificSupporter) return;

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var gradientColourSets = Enum.GetValues<GradientColourSet>();
        var selectedGradientColourSetIndex = maybeGradientColourSet.HasValue ? gradientColourSets.IndexOf(maybeGradientColourSet.Value) + 1 : 0;

        var comboWidth = 140;
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo($"Gradient Color Set###{nestedId}GradientColorSet", ref selectedGradientColourSetIndex, gradientColourSets.Select(s => s.GetFancyName()).Prepend("None").ToArray()))
        {
            if (selectedGradientColourSetIndex == 0)
            {
                titleDataConfig.GradientColourSet = null;
                titleDataConfig.GradientAnimationStyle = null;
            }
            else
            {
                titleDataConfig.GradientColourSet = gradientColourSets.ElementAt(selectedGradientColourSetIndex - 1);
            }
            SaveConfig();
        }

        if (!titleDataConfig.GradientColourSet.HasValue) return;

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var maybeGradientAnimationStyle = titleDataConfig.GradientAnimationStyle;

        var gradientAnimationStyles = Enum.GetValues<GradientAnimationStyle>();
        var selectedGradientAnimationStyleIndex = maybeGradientAnimationStyle.HasValue ? gradientAnimationStyles.IndexOf(maybeGradientAnimationStyle.Value) + 1 : 0;

        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo($"Gradient Animation Style###{nestedId}GradientAnimationStyle", ref selectedGradientAnimationStyleIndex, gradientAnimationStyles.Select(s => s.ToString()).Prepend("None").ToArray()))
        {
            titleDataConfig.GradientAnimationStyle = selectedGradientAnimationStyleIndex == 0 ? null : gradientAnimationStyles.ElementAt(selectedGradientAnimationStyleIndex - 1);
            SaveConfig();
        }
    }

    private static bool TryParseTemplate(string template, out LogMessageBag logMessageBag)
    {
        var parsed = Template.Parse(template);
        logMessageBag = parsed.Messages;
        return !parsed.HasErrors;
    }

    private void SaveConfig() => PluginInterface.SavePluginConfig(Config);
}
