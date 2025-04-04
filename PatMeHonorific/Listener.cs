using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PatMeHonorific;

public class Listener
{
    private IClientState ClientState { get; init; }
    private IFramework Framework { get; init; }
    private ParsedConfig ParsedConfig { get; init; }
    private IPluginLog PluginLog { get; init; }
    private IGameInteropProvider GameInteropProvider { get; init; }

    public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
    private readonly Hook<OnEmoteFuncDelegate>? hookEmote;

    public event Action<ushort, uint>? OnCounterChanged;

    private Dictionary<Emote, uint> Counters { get; init; } = new()
    {
        { Emote.Pet, 0 },
        { Emote.Dote, 0 },
        { Emote.Hug, 0 }
    };

    public Listener(IClientState clientState, IFramework framework, ParsedConfig parsedConfig, IPluginLog pluginLog, IGameInteropProvider gameInteropProvider)
    {
        ClientState = clientState;
        Framework = framework;
        ParsedConfig = parsedConfig;
        PluginLog = pluginLog;
        GameInteropProvider = gameInteropProvider;

        try
        {
            hookEmote = GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            hookEmote.Enable();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "failed to hook emotes!");
        }

        clientState.Login += LoadCounters;
        LoadCounters();
    }

    public void Dispose()
    {
        hookEmote?.Dispose();
        ClientState.Login -= LoadCounters;
    }

    private void LoadCounters()
    {
        Framework.RunOnFrameworkThread(() =>
        {
            if (ClientState.LocalPlayer != null)
            {
                var emoteData = ParsedConfig.Data.EmoteData.FirstOrDefault(d => d.CID == ClientState.LocalContentId);
                if (emoteData != null)
                {
                    foreach (var counter in Counters)
                    {
                        var dataCounter = emoteData.Counters.FirstOrDefault(c => c.Name == counter.Key.ToString());
                        if (dataCounter != null)
                        {
                            Counters[counter.Key] = dataCounter.Value;
                        }
                    }
                }
            }
        });
    }

    private void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        if (ClientState.LocalPlayer != null)
        {
            if (targetId == ClientState.LocalPlayer.GameObjectId)
            {
                if (Config.EMOTE_ID_TO_EMOTE.TryGetValue(emoteId, out var emote))
                {
                    Counters[emote]++;
                    OnCounterChanged?.Invoke(emoteId, Counters[emote]);
                }      
            }
        }

        hookEmote?.Original(unk, instigatorAddr, emoteId, targetId, unk2);
    }
}
