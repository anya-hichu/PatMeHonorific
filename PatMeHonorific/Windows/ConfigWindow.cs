using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel;
using PatMeHonorific.Emotes;
using PatMeHonorific.Interop;
using PatMeHonorific.Utils;
using Emote = Lumina.Excel.Sheets.Emote;

namespace PatMeHonorific.Windows;

public class ConfigWindow : Window
{
    private static readonly Dictionary<string, ushort> EMOTE_NAME_TO_ID = new()
    {
        { "Pet", 105 },
        { "Dote", 146 },
        { "Hug", 112 },
    };

    private static readonly string CONFIRM_DELETE_HINT = "Press CTRL while clicking to confirm";

    private Config Config { get; init; }
    private Dictionary<ushort, HashSet<string>> CommandsByEmoteId { get; init; }
    private ImGuiHelper ImGuiHelper { get; init; } = new();
    private PatMeConfig PatMeConfig { get; init; }

    public ConfigWindow(Config config, ExcelSheet<Emote> emoteSheet, PatMeConfig patMeConfig) : base("PatMeHonorific - Config##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Config = config;

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

        PatMeConfig = patMeConfig;
    }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled###enabled", ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 135);
        if (ImGui.Button("Sync###sync"))
        {
            Sync();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Overrides internal counter with PatMe ones if higher\nCounters can be displayed using: /patmehonorific info");
        }

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
        {
            if (ImGui.Button("Delete All###generatorConfigsDeleteAll") && ImGui.GetIO().KeyCtrl)
            {
                Config.EmoteConfigs.Clear();
                Config.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(CONFIRM_DELETE_HINT);
            }
        }


        if (ImGui.Button("+###emoteConfigsNew"))
        {
            Config.EmoteConfigs.Add(new());
            Config.Save();
        }

        ImGui.SameLine(ImGui.GetStyle().IndentSpacing * 1.5f);

        using (ImRaii.TabBar("emoteConfigsTabBar", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton | ImGuiTabBarFlags.FittingPolicyScroll))
        {
            foreach (var emoteConfig in Config.EmoteConfigs)
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
                                Config.Save();
                            }

                            ImGui.SameLine(ImGui.GetWindowWidth() - 75);
                            using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
                            {
                                if (ImGui.Button($"Delete###{baseId}Delete") && ImGui.GetIO().KeyCtrl)
                                {
                                    Config.EmoteConfigs.Remove(emoteConfig);
                                    Config.Save();
                                }
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip(CONFIRM_DELETE_HINT);
                                }
                            }

                            if (ImGui.InputText($"Name###{baseId}Name", ref name, 255))
                            {
                                emoteConfig.Name = name;
                                Config.Save();
                            }

                            var direction = emoteConfig.Direction;

                            var directionName = Enum.GetName(direction)!;
                            var directionNames = Enum.GetNames<EmoteDirection>().ToList();
                            var directionNameIndex = directionNames.IndexOf(directionName);

                            if (ImGui.Combo($"Direction###{baseId}Direction", ref directionNameIndex, [.. directionNames], directionNames.Count))
                            {
                                var newDirectionName = directionNames.ElementAt(directionNameIndex);
                                emoteConfig.Direction = Enum.Parse<EmoteDirection>(newDirectionName);
                                Config.Save();
                            }

                            var emoteIds = emoteConfig.EmoteIds;
                            var joinedCommands = string.Join(',', CommandsByEmoteId.Where(p => emoteIds.Contains(p.Key)).Select(c => c.Value.FirstOrDefault()));

                            if (ImGui.InputText($"Commands###{baseId}JoinedCommands", ref joinedCommands, 255))
                            {
                                var commands = joinedCommands.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                emoteConfig.EmoteIds = [.. CommandsByEmoteId.Where(c => c.Value.Intersect(commands).Any()).Select(c => c.Key)];
                                Config.Save();
                            }
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Comma-separated list of emote commands to track");
                            }



                            var titleTemplate = emoteConfig.TitleTemplate;
                            if (ImGui.InputText($"Title template###{baseId}TitleDataJsonInput", ref titleTemplate, 255))
                            {
                                emoteConfig.TitleTemplate = titleTemplate;
                                Config.Save();
                            }
                            ImGuiComponents.HelpMarker("Use {0} as placeholder for count");

                            var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());
                            var color = emoteConfig.Color;
                            if (ImGuiHelper.DrawColorPicker($"Color###{baseId}Color", ref color, checkboxSize))
                            {
                                emoteConfig.Color = color;
                                Config.Save();
                            }

                            ImGui.SameLine();
                            ImGui.Spacing();
                            ImGui.SameLine();
                            var glow = emoteConfig.Glow;
                            if (ImGuiHelper.DrawColorPicker($"Glow###{baseId}Glow", ref glow, checkboxSize))
                            {
                                emoteConfig.Glow = glow;
                                Config.Save();
                            }

                            ImGui.SameLine();
                            var isPrefix = emoteConfig.IsPrefix;
                            if (ImGui.Checkbox($"Prefix###{baseId}Prefix", ref isPrefix))
                            {
                                emoteConfig.IsPrefix = isPrefix;
                                Config.Save();
                            }
                        }
                    }
                }
            }
        }

        ImGui.NewLine();
        var autoClearTitleInterval = Config.AutoClearTitleInterval;
        if (ImGui.InputInt("Auto clear (secs)###autoClearTitleInterval", ref autoClearTitleInterval))
        {
            Config.AutoClearTitleInterval = autoClearTitleInterval;
            Config.Save();
        }
    }

    private void Sync()
    {
        var parsed = PatMeConfig.Parsed;
        if (parsed != null)
        {
            foreach (var emoteData in parsed.EmoteData)
            {
                var characterId = emoteData.CID;
                foreach (var counter in emoteData.Counters)
                {
                    var emoteId = EMOTE_NAME_TO_ID[counter.Name];
                    var key = new EmoteCounterKey() { CharacterId = characterId, EmoteId = emoteId, Direction = EmoteDirection.Receiving };

                    if (Config.Counters.ContainsKey(key) && Config.Counters[key] < counter.Value)
                    {
                        Config.Counters[key] = counter.Value;
                    }
                    else 
                    {
                        Config.Counters.Add(key, counter.Value);
                    }
                }
            }
            Config.Save();
        }
    }
}
