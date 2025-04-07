using System.IO;
using Newtonsoft.Json;

namespace ScheduleISync
{
    public class GCloudConsoleConfig
    {
        public string ClientId { get; set; }
        public string ProjectId { get; set; }
        public string ClientSecret { get; set; }

        public static GCloudConsoleConfig LoadConfig()
        {
            string fileName = "GCloudConsoleConfig.json";
            if (!File.Exists(fileName))
            {
                // Создаем шаблон конфигурации, если файла нет
                var defaultConfig = new GCloudConsoleConfig
                {
                    ClientId = "",
                    ProjectId = "",
                    ClientSecret = ""
                };
                File.WriteAllText(fileName, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                return defaultConfig;
            }
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<GCloudConsoleConfig>(json);
        }
    }
}
