using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;
using LetsEncrypt.SiteExtension.Models;

namespace LetsEncrypt.SiteExtension
{
    public class SettingsStore
    {
        private static readonly SettingsStore _instance = new SettingsStore();
        private string _settingsFilePath;

        public static SettingsStore Instance { get { return _instance; } }

        public SettingsStore()
        {
            string folder;

            if (Util.IsAzure)
            {
                // e.g. D:\home\SiteExtensions\SettingsAPISample -> 'SettingsAPISample'
                string extensionName = "letsencrypt";

                // e.g. d:\home\data\SettingsAPISample
                folder = Path.Combine(Environment.ExpandEnvironmentVariables(@"%HOME%\data"), extensionName);
            }
            else
            {
                // Use regular App_Data outside of Azure
                folder = HostingEnvironment.MapPath("~/App_Data") ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _settingsFilePath = Path.Combine(folder, "settings.json");
        }

        public List<SettingEntry> Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                return JsonConvert.DeserializeObject<List<SettingEntry>>(File.ReadAllText(_settingsFilePath));
            }
            else
            {
                return new List<SettingEntry>();
            }
        }

        public void Save(List<SettingEntry> settings)
        {
            File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(settings));
        }
    }
}