using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PatMeHonorific.Windows;
using Dalamud.Plugin.Services;

namespace PatMeHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("PatMeHonorific");
    private ConfigWindow ConfigWindow { get; init; }
    private Updater Updater { get; init; }
    private Listener State { get; init; }
    private ParsedConfig ParsedConfig { get; init; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config();
        Config.MaybeMigrateToV1();

        var getCharacterTitle = PluginInterface.GetIpcSubscriber<int, string>("Honorific.GetCharacterTitle");
        var setCharacterTitle = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        var clearCharacterTitle = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");


        ConfigWindow = new(Config, getCharacterTitle, clearCharacterTitle);


        ParsedConfig = new(PluginInterface);
        State = new Listener(ClientState, Framework, ParsedConfig, PluginLog, GameInteropProvider);
        Updater = new(Config, Framework, State, setCharacterTitle, clearCharacterTitle);

        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;

        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += () => {};
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Updater.Dispose();
        ConfigWindow.Dispose();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
