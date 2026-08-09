using System;
using System.IO;
using Newtonsoft.Json;

namespace HSRivalPlugin
{
    public class PluginConfig
    {
        public string UserToken { get; set; } = "";
        public string ServerUrl { get; set; } = "https://hs-rival-meta.onrender.com";
        public bool AutoSyncCollection { get; set; } = true;
        public bool AutoSyncMatches { get; set; } = true;

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HearthstoneDeckTracker",
            "hs_rival_config.json"
        );

        public static PluginConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonConvert.DeserializeObject<PluginConfig>(json);
                    if (config != null)
                    {
                        if (string.IsNullOrWhiteSpace(config.ServerUrl))
                            config.ServerUrl = "https://hs-rival-meta.onrender.com";
                        return config;
                    }
                }
            }
            catch { }

            var defaultConfig = new PluginConfig();
            defaultConfig.Save();
            return defaultConfig;
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
