using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PatMeHonorific.Windows;
using Dalamud.Plugin.Services;
using PatMeHonorific.Interop;
using PatMeHonorific.Emotes;
using Dalamud.Game.Command;
using Emote = Lumina.Excel.Sheets.Emote;
using System.Linq;
using Lumina.Excel;
using PatMeHonorific.Configs;
using PatMeHonorific.Updaters;

namespace PatMeHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] private static IChatGui ChatGui { get; set; } = null!;
    [PluginService] private static IPlayerState PlayerState { get; set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] private static IDataManager DataManager { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static IGameInteropProvider GameInteropProvider { get; set; } = null!;

    [PluginService] private static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static IPluginLog PluginLog { get; set; } = null!;

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
                new() { Name = "Receiving Pet", EmoteIds = [105], TitleTemplate = "Pet Counter {{ total_count }}" },
                new() { Name = "Receiving Dote", EmoteIds = [146], TitleTemplate = "Dote Counter {{ total_count }}" },
                new() { Name = "Receiving Hug",  EmoteIds = [112, 113], TitleTemplate = "Hug Counter {{ total_count }}" }
            ]
        };

        #region Deprecated
        new ConfigMigrator(PluginInterface).MaybeMigrate(Config);
        #endregion

        var patMeSynchronizer = new PatMeSynchronizer(PluginInterface, PluginLog);

        var setCharacterTitle = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        var clearCharacterTitle = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");

        EmoteSheet = DataManager.GetExcelSheet<Emote>()!;
        ConfigWindow = new(Config, EmoteSheet, patMeSynchronizer, PlayerState, PluginInterface, PluginLog);
        EmoteHook = new(PluginLog, GameInteropProvider);

        Updater = new(clearCharacterTitle, Config, EmoteHook, Framework, ObjectTable, PlayerState, PluginLog, PluginInterface, setCharacterTitle);

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
            SaveConfig();
        }
        else if (subcommand == "disable")
        {
            Config.Enabled = false;
            SaveConfig();
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

    private void SaveConfig() => PluginInterface.SavePluginConfig(Config);
}
