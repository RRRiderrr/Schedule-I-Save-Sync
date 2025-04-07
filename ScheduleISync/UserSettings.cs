using System.IO;
using Newtonsoft.Json;

namespace ScheduleISync
{
    public class UserSettings
    {
        public string SteamID { get; set; }
        public string FolderLink { get; set; }
        public string SheetLink { get; set; } // Ссылка на Google Таблицу
        public string SelectedSlot { get; set; }
        public string LastUsedAccount { get; set; }
    }

    public static class UserSettingsManager
    {
        private const string SettingsFileName = "user-settings.json";

        public static UserSettings LoadSettings()
        {
            if (!File.Exists(SettingsFileName))
            {
                var defaultSettings = new UserSettings
                {
                    SteamID = "",
                    FolderLink = "",
                    SheetLink = "",
                    SelectedSlot = "",
                    LastUsedAccount = ""
                };
                SaveSettings(defaultSettings);
                return defaultSettings;
            }
            else
            {
                string json = File.ReadAllText(SettingsFileName);
                var settings = JsonConvert.DeserializeObject<UserSettings>(json);
                return settings;
            }
        }

        public static void SaveSettings(UserSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFileName, json);
        }
    }
}
