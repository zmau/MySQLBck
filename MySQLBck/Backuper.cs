using MySql.Data.MySqlClient;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace com.arnet.MySQLBck
{
    internal class Backuper
    {
        public static readonly string DEFAULT_SERVER_NAME = "localhost";
        public static readonly string DEFAULT_USER_NAME = "root";
        public static readonly string DEFAULT_PASSWORD = "root";
        public static readonly string[] COMMON_CREDENTIALS = { "demo/demo", "oper/online", "oper/lipa" };

        public static readonly string[] SYS_DATABASES = { "information_schema", "mysql", "performance_schema", "sys" };

        private MySqlConnection Connection { get; set; }
        private readonly NameValueCollection _appSettings;
        private readonly string _serverName;
        private readonly string _userName;
        private readonly string _password;


        public Backuper()
        {
            //TODO change parameters
            Connection = getConnection("localhost", "root", "root");
            _appSettings = ConfigurationManager.GetSection(sectionName: "appSettings") as NameValueCollection;
            if(_appSettings == null)
            {
                Console.WriteLine("Missing appSettings section in App.config file.");
                Environment.Exit(1);
            }
            _serverName = _appSettings.Get("server_name") ?? DEFAULT_SERVER_NAME;
            _userName = _appSettings.Get("user_name") ?? DEFAULT_USER_NAME;
            _password = _appSettings.Get("password") ?? DEFAULT_PASSWORD;
        }
        public MySqlConnection getConnection(string serverName, string userName, string password)
        {
            string connstring = string.Format("Server={0}; UID={1}; password={2}", serverName, userName, password);
            var connection = new MySqlConnection(connstring);
            connection.Open();
            return connection;
        }

        public void insert(string text)
        {
            string query = $"use db1; insert into log (text) values ('{text}')";
            var cmd = new MySqlCommand(query, Connection);
            cmd.ExecuteNonQuery();
        }

        public void dumpDatabases()
        {
            try
            {
                Process dumperProcess = newDumperProcess();
                var dumpPath = _appSettings.Get("dump_path");
                if(dumpPath == null)
                {
                    Console.WriteLine("Backup destination folder not defined!");
                    Environment.Exit(2);
                }
                var outputStream = new StreamWriter(dumpPath);
                dumperProcess.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                dumperProcess.Start();
                dumperProcess.BeginOutputReadLine();
                dumperProcess.WaitForExit();
                outputStream.Dispose();
            }
            catch(Exception e)
            {
                Console.WriteLine("Dumping failed with exception :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private Process newDumperProcess()
        {
            Process process = new Process();
            var dumperPath = _appSettings.Get("dumper_path");
            if (dumperPath == null)
            {
                Console.WriteLine("path to mysqldump.exe not defined!");
                Environment.Exit(2);
            }
            process.StartInfo.FileName = dumperPath;
            var databasesSpaceSeparatedList = getDatabasesSpaceSeparatedList();
            process.StartInfo.Arguments = $" -h{_serverName} -u{_userName} -p{_password} --databases{databasesSpaceSeparatedList}";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            return process;
        }

        private string getDatabasesSpaceSeparatedList()
        {
            string query = "show databases";
            var cmd = new MySqlCommand(query, Connection);
            var reader = cmd.ExecuteReader();
            StringBuilder list = new StringBuilder();
            while (reader.Read())
            {
                if(!SYS_DATABASES.Contains(reader.GetString("database")))
                    list.Append(" " + reader.GetString("database"));
            }
            return list.ToString();
        }

    }
}
