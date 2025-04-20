using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using PatMeHonorific.Emotes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PatMeHonorific;

public class Updater : IDisposable
{
    private IClientState ClientState { get; init; }
    private Config Config { get; init; }
    private EmoteHook EmoteHook { get; init; }
    private IFramework Framework { get; init; }
    private IObjectTable ObjectTable { get; init; }

    private ICallGateSubscriber<int, string, object> SetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }

    private DateTime? LastTitleUpdateAt { get; set; }

    public Updater(ICallGateSubscriber<int, object> clearCharacterTitle, IClientState clientState, Config config, EmoteHook emoteHook, IFramework framework, IObjectTable objectTable, ICallGateSubscriber<int, string, object> setCharacterTitle) {
        ClearCharacterTitle = clearCharacterTitle;
        ClientState = clientState;
        Config = config;
        EmoteHook = emoteHook;
        Framework = framework;
        ObjectTable = objectTable;
        SetCharacterTitle = setCharacterTitle;

        Framework.Update += OnFrameworkUpdate;
        EmoteHook.OnEmote += OnEmote;
    }

    public void Dispose()
    {
        EmoteHook.OnEmote -= OnEmote;
        Framework.Update -= OnFrameworkUpdate;
    }

    private bool TryUpdateCounter(ulong instigatorAddr, ushort emoteId, ulong targetId, out EmoteConfig? emoteConfig, out uint totalCounter)
    {
        var localPlayer = ClientState.LocalPlayer;
        if (localPlayer != null && ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr) is IPlayerCharacter instigator && instigator.GameObjectId != targetId)
        {
            EmoteDirection? maybeDirection = null;
            if (targetId == localPlayer.GameObjectId)
            {
                maybeDirection = EmoteDirection.Receiving;
            } 
            else if (instigator.GameObjectId == localPlayer.GameObjectId)
            {
                maybeDirection = EmoteDirection.Giving;
            }

            if (maybeDirection.HasValue)
            {
                var direction = maybeDirection.Value;
                var characterId = ClientState.LocalContentId;

                emoteConfig = Config.EmoteConfigs.Find(c => c.Enabled && c.EmoteIds.Contains(emoteId) && c.Direction == direction);
                if (emoteConfig != null)
                {
                    totalCounter = 0;
                    foreach (var configEmoteId in emoteConfig.EmoteIds) {
                        var key = new EmoteCounterKey() { CharacterId = characterId, Direction = direction, EmoteId = configEmoteId };
                        Config.Counters.TryAdd(key, 0);
                        if (configEmoteId == emoteId)
                        {
                            Config.Counters[key] += 1;
                            Config.Save();
                        }
                        totalCounter += Config.Counters[key];
                    }
                    return true;
                }
            }
        }
        emoteConfig = default;
        totalCounter = default;
        return false;
    }

    public void OnEmote(ulong instigatorAddr, ushort emoteId, ulong targetId)
    {
        if (Config.Enabled)
        {
            if (TryUpdateCounter(instigatorAddr, emoteId, targetId, out var emoteConfig, out var totalCounter))
            {
                if (emoteConfig != null)
                {
                    var titleData = new Dictionary<string, object>()
                    {
                        { "Title", string.Format(emoteConfig.TitleTemplate, totalCounter) },
                        { "IsPrefix", emoteConfig.IsPrefix },
                        { "Color", emoteConfig.Color! },
                        { "Glow", emoteConfig.Glow! }
                    };
                    var title = JsonConvert.SerializeObject(titleData);
                    SetCharacterTitle.InvokeAction(0, title);
                    LastTitleUpdateAt = DateTime.Now;
                }  
            }   
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
