using Dalamud.Plugin;
using System;

namespace PatMeHonorific.Configs;

public class ConfigMigrator(IDalamudPluginInterface pluginInterface)
{
    public void MaybeMigrate(Config config)
    {
        if (config.Version == Config.CURRENT_VERSION) return;

        if (config.Version < 4)
        {
            try
            {
                config.AutoClearDelayMs = Convert.ToUInt16(config.AutoClearTitleInterval * 1000);
            } 
            catch (OverflowException)
            {
                config.AutoClearDelayMs = ushort.MaxValue;
            }

            config.EmoteConfigs.ForEach(ec =>
            {
                ec.TitleTemplate = ec.TitleTemplate.Replace("{0}", "{{ total_count }}");
                ec.TitleDataConfig = new()
                {
                    IsPrefix = ec.IsPrefix,
                    Color = ec.Color,
                    Glow = ec.Glow
                };
            });
        }

        config.Version = Config.CURRENT_VERSION;
        pluginInterface.SavePluginConfig(config);   
    }
}
