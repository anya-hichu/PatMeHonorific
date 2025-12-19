using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PatMeHonorific.Windows;
using Dalamud.Plugin.Services;
using PatMeHonorific.Interop;
using PatMeHonorific.Emotes;
using Dalamud.Game.Command;
using Emote = Lumina.Excel.Sheets.Emote;
using Dalamud.Utility;
using System.Linq;
using Lumina.Excel;

namespace PatMeHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

    private const string CommandName = "/patmehonorific";
    private const string CommandHelpMessage = $"Available subcommands for {CommandName} are info, config, enable and disable";

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("PatMeHonorific");
    private ConfigWindow ConfigWindow { get; init; }

    private EmoteHook EmoteHook { get; init; }
    private Updater Updater { get; init; }
    private ExcelSheet<Emote> EmoteSheet { get; init; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config()
        {
            EmoteConfigs = [
                new() { Name = "Receiving Pet", EmoteIds = [105], TitleTemplate = "Pet Counter {0}" },
                new() { Name = "Receiving Dote", EmoteIds = [146], TitleTemplate = "Dote Counter {0}" },
                new() { Name = "Receiving Hug",  EmoteIds = [112, 113], TitleTemplate = "Hug Counter {0}" }
            ]
        };

        var patMeConfig = new PatMeConfig(PluginInterface, PluginLog);

        var setCharacterTitle = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        var clearCharacterTitle = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");

        EmoteSheet = DataManager.GetExcelSheet<Emote>()!;
        ConfigWindow = new(PlayerState, Config, EmoteSheet, patMeConfig, PluginLog);
        EmoteHook = new(PluginLog, GameInteropProvider);

        Updater = new(clearCharacterTitle, Config, EmoteHook, Framework, ObjectTable, PlayerState, setCharacterTitle);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = CommandHelpMessage
        });

        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    }

    private void OnCommand(string command, string args)
    {
        var subcommand = args.Split(" ", 2)[0];
        if (subcommand == "config")
        {
            ToggleConfigUI();
        }
        else if (subcommand == "enable")
        {
            Config.Enabled = true;
            Config.Save();
        }
        else if (subcommand == "disable")
        {
            Config.Enabled = false;
            Config.Save();
        }
        else if (subcommand == "info")
        {
            PrintCounters();
        }
        else
        {
            ChatGui.Print(CommandHelpMessage);
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        EmoteHook.Dispose();
        Updater.Dispose();
    }

    private void PrintCounters()
    {
        ChatGui.Print("Counters:");
        foreach(var counter in Config.Counters.Where(c => c.Key.CharacterId == PlayerState.ContentId))
        {
            var key = counter.Key;
            ChatGui.Print($"     {EmoteSheet.GetRowAt(key.EmoteId).Name}: {counter.Value} ({key.Direction})");
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
