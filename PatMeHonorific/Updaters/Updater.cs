using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using PatMeHonorific.Configs;
using PatMeHonorific.Emotes;
using PatMeHonorific.Interop;
using Scriban;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PatMeHonorific.Updaters;

public class Updater : IDisposable
{
    private Config Config { get; init; }
    private EmoteHook EmoteHook { get; init; }
    private IFramework Framework { get; init; }
    private IObjectTable ObjectTable { get; init; }
    private IPlayerState PlayerState { get; init; }
    private IDalamudPluginInterface PluginInterface { get; init; }
    private IPluginLog PluginLog { get; init; }

    private ICallGateSubscriber<int, string, object> SetCharacterTitle { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitle { get; init; }

    private EmoteCounters<uint> SessionCounters { get; init; } = [];
    private EmoteCounters<EmoteComboCounter> ComboCounters { get; init; } = [];

    private DateTime? LastUpdateAt { get; set; }

    public Updater(ICallGateSubscriber<int, object> clearCharacterTitle, Config config, EmoteHook emoteHook, IFramework framework, IObjectTable objectTable, IPlayerState playerState, IPluginLog pluginLog, IDalamudPluginInterface pluginInterface, ICallGateSubscriber<int, string, object> setCharacterTitle) 
    {
        ClearCharacterTitle = clearCharacterTitle;
        Config = config;
        EmoteHook = emoteHook;
        Framework = framework;
        ObjectTable = objectTable;
        PlayerState = playerState;
        PluginInterface = pluginInterface;
        PluginLog = pluginLog;
        SetCharacterTitle = setCharacterTitle;

        Framework.Update += OnFrameworkUpdate;
        EmoteHook.OnEmote += OnEmote;
    }

    public void Dispose()
    {
        EmoteHook.OnEmote -= OnEmote;
        Framework.Update -= OnFrameworkUpdate;
    }

    private bool TryUpdateCounters(ulong instigatorAddr, ushort emoteId, ulong targetId, [NotNullWhen(true)] out EmoteConfig? emoteConfig, out uint totalCount, out uint sessionCount, [NotNullWhen(true)] out EmoteComboCounter? comboCounter)
    {
        emoteConfig = null;
        totalCount = default;
        sessionCount = default;
        comboCounter = null;

        var localPlayer = ObjectTable.LocalPlayer;
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
                var characterId = PlayerState.ContentId;

                emoteConfig = Config.EmoteConfigs.OrderByDescending(c => c.Priority).FirstOrDefault(c => c.Enabled && c.EmoteIds.Contains(emoteId) && c.Direction == direction && (c.CharacterIds.Count == 0 || c.CharacterIds.Contains(PlayerState.ContentId)));
                if (emoteConfig != null)
                {
                    var cumulativeComboCounter = new EmoteComboCounter();
                    foreach (var configEmoteId in emoteConfig.EmoteIds) 
                    {
                        var key = new EmoteCounterKey() { CharacterId = characterId, Direction = direction, EmoteId = configEmoteId };

                        Config.Counters.TryAdd(key, 0);
                        SessionCounters.TryAdd(key, 0);
                        ComboCounters.TryAdd(key, new());
                        if (configEmoteId == emoteId)
                        {
                            Config.Counters[key]++;
                            PluginInterface.SavePluginConfig(Config);

                            SessionCounters[key]++;
                            ComboCounters[key].Increment();
                        }

                        totalCount += Config.Counters[key];
                        sessionCount += SessionCounters[key];
                        cumulativeComboCounter.Add(ComboCounters[key]);
                    }

                    comboCounter = cumulativeComboCounter;
                    return true;
                }
            }
        }

        return false;
    }

    private void OnEmote(ulong instigatorAddr, ushort emoteId, ulong targetId)
    {
        if (Config.Enabled && TryUpdateCounters(instigatorAddr, emoteId, targetId, out var emoteConfig, out var totalCount, out var sessionCount, out var recentCounter) && emoteConfig != null && emoteConfig.TitleDataConfig != null)
        {
            var parsedTemplate = Template.Parse(emoteConfig.TitleTemplate);

            if (parsedTemplate.HasErrors)
            {
                PluginLog.Error($"Failed to parse scriban title template ({parsedTemplate.Messages})");
                return;
            }

            try
            {
                var title = parsedTemplate.Render(new UpdaterModel()
                {
                    TotalCount = totalCount,
                    SessionCount = sessionCount,
                    ComboCount = recentCounter.Get()
                });

                if (title.Length > Constraint.MaxTitleLength)
                {
                    PluginLog.Error($"Rendered title [{title}] is above limit of {Constraint.MaxTitleLength} characters, ignoring");
                    return;
                }

                var titleData = emoteConfig.TitleDataConfig.ToTitleData(title, Config.IsHonorificSupporter);

                var serializedData = JsonConvert.SerializeObject(titleData, Formatting.Indented);
                if (serializedData == null) return;

                PluginLog.Debug($"Call Honorific SetCharacterTitle IPC with:\n{serializedData}");

                SetCharacterTitle.InvokeAction(0, serializedData);
                LastUpdateAt = DateTime.Now;
            }
            catch (Exception e)
            {
                PluginLog.Error($"Failed to update honorific title ({e.Message})");
            }
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (LastUpdateAt.HasValue && DateTimeOffset.UtcNow.Subtract(LastUpdateAt.Value) > TimeSpan.FromMilliseconds(Config.AutoClearDelayMs))
        {
            PluginLog.Debug($"Call Honorific ClearCharacterTitle IPC after delay");
            ClearCharacterTitle.InvokeAction(0);
            LastUpdateAt = null;
        }
    }
}
