using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PatMeHonorific.Windows;
using Dalamud.Plugin.Services;

namespace PatMeHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("PatMeHonorific");
    private ConfigWindow ConfigWindow { get; init; }
    private Updater Updater { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var getCharacterTitle = PluginInterface.GetIpcSubscriber<int, string>("Honorific.GetCharacterTitle");
        var clearCharacterTitle = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");
        ConfigWindow = new(Configuration, getCharacterTitle, clearCharacterTitle);

        var counterChanged = PluginInterface.GetIpcSubscriber<string, uint, object?>("PatMe.CounterChanged");
        var setCharacterTitle = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        Updater = new(Configuration, Framework, counterChanged, setCharacterTitle, clearCharacterTitle);

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
