using System;
namespace ThePathBot.Utilities
{
    public class DBConnection
    {
        private string databaseName = string.Empty;
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string databaseUser { get; set; }
        public string Password { get; set; }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if (_instance == null)
                _instance = new DBConnection();
            return _instance;
        }

        public string databasePort { get; set; }
        public string connectionString
        {
            get { return $"Server=williamspires.co.uk; Port={databasePort}; database={databaseName}; UID={databaseUser}; password={Password}"; }
        }
    }
}
