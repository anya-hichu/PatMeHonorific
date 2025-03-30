using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PatMeHonorific;

public class Updater : IDisposable
{
    private ICallGateSubscriber<int, string, object> SetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }

    private Config Config { get; init; }
    private IFramework Framework { get; init; }
    private Listener State { get; init; }
    private DateTime? LastTitleUpdateAt { get; set; }
    

    public Updater(Config configuration, IFramework framework, Listener state, ICallGateSubscriber<int, string, object> setCharacterTitle, ICallGateSubscriber<int, object> clearCharacterTitle) {
        Config = configuration;
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
        if (Config.Enabled && emoteId == 105)
        {
            var titleData = new Dictionary<string, object>()
            {
                { "Title", Config.TitleTemplate.Replace("{0}", count.ToString()) },
                { "IsPrefix", Config.IsPrefix },
                { "Color", Config.Color! },
                { "Glow", Config.Glow! }
            };

            var title = JsonConvert.SerializeObject(titleData);
            SetCharacterTitle.InvokeAction(0, title);
            LastTitleUpdateAt = DateTime.Now;
        }
    }

    public void OnFrameworkUpdate(IFramework framework)
    {
        if (LastTitleUpdateAt.HasValue)
        {
            var delta = DateTime.Now - LastTitleUpdateAt.Value;
            if (delta.TotalSeconds > Config.AutoClearTitleInterval)
            {
                ClearCharacterTitle.InvokeAction(0);
                LastTitleUpdateAt = null;
            }
        }
    }
}
