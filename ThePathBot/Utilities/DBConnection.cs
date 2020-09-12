using System;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace ThePathBot.Utilities
{
    public class DBConnectionUtils
    {
        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string ReturnPopulatedConnectionStringAsync()
        {
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            MySqlConnectionStringBuilder mcsb = new MySqlConnectionStringBuilder
            {
                Database = configJson.databaseName,
                Password = configJson.databasePassword,
                UserID = configJson.databaseUser,
                Port = configJson.databasePort,
                Server = configJson.databaseServer,
                MaximumPoolSize = 300
            };

            return mcsb.ToString();
        }

        public static string ReturnPopulatedConnectionStringStatic()
        {
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            MySqlConnectionStringBuilder mcsb = new MySqlConnectionStringBuilder
            {
                Database = configJson.databaseName,
                Password = configJson.databasePassword,
                UserID = configJson.databaseUser,
                Port = configJson.databasePort,
                Server = configJson.databaseServer,
                MaximumPoolSize = 300
            };

            return mcsb.ToString();
        }
    }
}
