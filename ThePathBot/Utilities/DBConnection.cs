using System;
namespace ThePathBot.Utilities
{
    public class DBConnection
    {
        public string DatabaseName { get; set; } = string.Empty;

        public string databaseUser { get; set; }
        public string Password { get; set; }
        public string databaseServer { get; set; }
        public string databasePort { get; set; }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if (_instance == null)
                _instance = new DBConnection();
            return _instance;
        }

        
        public string connectionString => $"Server={databaseServer}; Port={databasePort}; database={DatabaseName}; UID={databaseUser}; password={Password}";
    }
}
