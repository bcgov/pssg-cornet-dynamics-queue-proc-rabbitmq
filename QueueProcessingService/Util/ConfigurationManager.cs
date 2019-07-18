using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace QueueProcessingService.Util
{
    public static class ConfigurationManager
    {
        public static IConfiguration AppSetting { get; }
        public static IConfiguration LocalAppSetting { get; }
        public static IConfiguration EnvSetting { get; }

        static ConfigurationManager()
        {
            //Check for file existence before using the config
            if (File.Exists(String.Format("{0}\\appsettings.json", Directory.GetCurrentDirectory())))
            {
                AppSetting = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
            }
            if (File.Exists(String.Format("{0}\\appsettings.local.json", Directory.GetCurrentDirectory())))
            {
                LocalAppSetting = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.local.json")
                    .Build();
            }
            // Pulls in all CORDYN_ prefixed Environment Variables
            EnvSetting = new ConfigurationBuilder().AddEnvironmentVariables("CORDYN_").Build();
        }

        public static String FetchConfig(String ConfigKey)
        {
            String returnValue = "";
            String EnvKey = ConfigKey.Replace(":", "_");
            if (EnvSetting != null && EnvSetting[EnvKey] != null)
            {
                returnValue = EnvSetting[EnvKey];
            }
            else if (LocalAppSetting != null && LocalAppSetting[ConfigKey] != null)
            {
                returnValue = LocalAppSetting[ConfigKey];
            }
            else if (AppSetting != null && AppSetting[ConfigKey] != null)
            {
                returnValue = AppSetting[ConfigKey];
            }
            else
            {
                throw new Exception(String.Format("Configuration or file not found. Key requested {0}", ConfigKey));
            }
            return returnValue;
        }

    }
}
