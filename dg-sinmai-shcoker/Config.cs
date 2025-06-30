using System.IO;
using Tomlet;
using Tomlet.Attributes;
using MelonLoader.Utils;
using MelonLoader;
namespace dg_sinmai_shcoker
{
    public class Config
    {
        public string ws_url { get; set; }
        public string type { get; set; }
        public string single_intensity { get; set; }
        public string single_channel { get; set; }
        public int single_ms { get; set; }
        public int ramp_ms { get; set; }
    }
    public static class ConfigManager
    {
        public static Config Instance { get; private set; }

        public static void Load()
        {
            string configPath = Path.Combine(MelonEnvironment.MelonBaseDirectory, "DGShockMod.toml");
            if (!File.Exists(configPath))
            {
                MelonLogger.Warning("配置文件 DGShockMod.toml 未找到！");
                Instance = new Config(); // 默认空配置防止 null
                return;
            }

            try
            {
                string toml = File.ReadAllText(configPath);
                Instance = Tomlet.TomletMain.To<Config>(toml);
                MelonLogger.Msg("配置文件加载成功！");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"配置文件解析失败: {ex.Message}");
                Instance = new Config(); // 出错时提供默认配置
            }
        }
    }
}
