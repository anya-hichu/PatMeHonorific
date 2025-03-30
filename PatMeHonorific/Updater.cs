using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;

namespace PatMeHonorific;

public class Updater : IDisposable
{
    private ICallGateSubscriber<int, string, object> SetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }

    private Configuration Configuration { get; init; }
    private IFramework Framework { get; init; }
    private Listener State { get; init; }
    private DateTime? LastTitleUpdateAt { get; set; }
    

    public Updater(Configuration configuration, IFramework framework, Listener state, ICallGateSubscriber<int, string, object> setCharacterTitle, ICallGateSubscriber<int, object> clearCharacterTitle) {
        Configuration = configuration;
        Framework = framework;
        State = state;

        State.OnCounterChanged += OnCounterChange;
        SetCharacterTitle = setCharacterTitle;
        ClearCharacterTitle = clearCharacterTitle;
        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        State.OnCounterChanged -= OnCounterChange;
        Framework.Update -= OnFrameworkUpdate;
    }

    public void OnCounterChange(ushort emoteId, uint count)
    {
        // https://github.com/MgAl2O4/PatMeDalamud/blob/main/plugin/data/EmoteConstants.cs#L10
        if (Configuration.Enabled && emoteId == 105 && Configuration.TitleDataJsonValid())
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
