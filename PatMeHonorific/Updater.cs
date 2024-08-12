using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;

namespace PatMeHonorific;
public class Updater : IDisposable
{
    private ICallGateSubscriber<string, uint, object?> CounterChanged { get; init; }
    private ICallGateSubscriber<int, string, object> SetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }

    private Configuration Configuration { get; init; }
    private IFramework Framework { get; init; }
    private DateTime? LastTitleUpdateAt { get; set; }

    public Updater(Configuration configuration, IFramework framework, ICallGateSubscriber<string, uint, object?> counterChanged, ICallGateSubscriber<int, string, object> setCharacterTitle, ICallGateSubscriber<int, object> clearCharacterTitle) {
        Configuration = configuration;
        Framework = framework;
        CounterChanged = counterChanged;
        SetCharacterTitle = setCharacterTitle;
        ClearCharacterTitle = clearCharacterTitle;

        CounterChanged.Subscribe(OnCounterChange);
        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        CounterChanged.Unsubscribe(OnCounterChange);
        Framework.Update -= OnFrameworkUpdate;
    }

    public void OnCounterChange(string name, uint count)
    {
        if (Configuration.Enabled && name == "Pet" && Configuration.TitleDataJsonValid())
        {
            var title = Configuration.TitleDataJson.Replace("{0}", count.ToString());
            SetCharacterTitle.InvokeAction(Configuration.CharacterIndex, title);
            LastTitleUpdateAt = DateTime.Now;
        }
    }

    public void OnFrameworkUpdate(IFramework framework)
    {
        if (LastTitleUpdateAt.HasValue)
        {
            var delta = DateTime.Now - LastTitleUpdateAt.Value;
            if (delta.TotalSeconds > Configuration.AutoClearTitleInterval)
            {
                ClearCharacterTitle.InvokeAction(Configuration.CharacterIndex);
                LastTitleUpdateAt = null;
            }
        }
    }
}
