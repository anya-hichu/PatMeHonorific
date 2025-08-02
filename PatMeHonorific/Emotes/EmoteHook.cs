using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;

namespace PatMeHonorific.Emotes;

public class EmoteHook
{
    private IPluginLog PluginLog { get; init; }
    private IGameInteropProvider GameInteropProvider { get; init; }

    public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
    private Hook<OnEmoteFuncDelegate>? HookEmote { get; init; }

    public event Action<ulong, ushort, ulong>? OnEmote;

    public EmoteHook(IPluginLog pluginLog, IGameInteropProvider gameInteropProvider)
    {
        PluginLog = pluginLog;
        GameInteropProvider = gameInteropProvider;

        try
        {
            HookEmote = GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            HookEmote.Enable();
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Failed to hook emotes");
        }
    }

    public void Dispose()
    {
        HookEmote?.Dispose();
    }

    private void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        try
        {
            OnEmote?.Invoke(instigatorAddr, emoteId, targetId);
        } 
        catch (Exception e) 
        {
            PluginLog.Error(e.ToString());
        }
        HookEmote?.Original(unk, instigatorAddr, emoteId, targetId, unk2);
    }
}
