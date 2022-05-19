using MySql.Data.MySqlClient;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace com.arnet.MySQLBck
{
    internal class Backuper
    {
        public static readonly string DEFAULT_SERVER_NAME = "localhost";
        public static readonly string DEFAULT_PORT = "53306";
        
        public static readonly string[] COMMON_CREDENTIALS = { "demo/demo", "oper/online", "oper/lipa" };

        public static readonly string[] SYS_DATABASES = { "information_schema", "mysql", "performance_schema", "sys" };

        private MySqlConnection Connection { get; set; }
        private NameValueCollection _appSettings;
        private readonly string _serverName;
        private readonly string _port;
        private string? _userName;
        private string? _password;
        public bool FoundCorrectCredentials { get { return Connection is not null; } }

        public Backuper()
        {
            object appSettings = ConfigurationManager.GetSection(sectionName: "appSettings");
            if(appSettings == null)
            {
                Console.WriteLine("Greška : nema sekcije appSettings u App.config fajlu.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            _appSettings = appSettings as NameValueCollection;
            _serverName = _appSettings.Get("server_name") ?? DEFAULT_SERVER_NAME;
            _port = _appSettings.Get("port") ?? DEFAULT_PORT;
            string? userNameFromSettings = _appSettings.Get("user_name");
            string? passwordFromSettings = _appSettings.Get("password");
            if (userNameFromSettings is null)
            {
                Console.WriteLine("Upozorenje : nema parametra userName u App.config fajlu. Svakako pokušavam da iskoristim i difoltne.");
            }
            if (passwordFromSettings is null)
            {
                Console.WriteLine("Upozorenje : nema parametra password u App.config fajlu. Svakako pokušavam da iskoristim i difoltne.");
            }
            if (userNameFromSettings is not null && passwordFromSettings is not null)
            {
                try
                {
                    Connection = getConnection(_serverName, _port, userNameFromSettings, passwordFromSettings);
                    _userName = userNameFromSettings;
                    _password = passwordFromSettings;
                }
                catch (Exception)
                {
                    // Nothing, just could not connect with those credentials
                    Console.WriteLine("Neuspešna konekcija");
                }
            }
            if (!FoundCorrectCredentials)
                checkCommonCredentials();
        }
        private MySqlConnection getConnection(string serverName, string port, string userName, string password)
        {
            string connstring = string.Format("Server={0}; port={1}; UID={2}; password={3}", serverName, port, userName, password);
            var connection = new MySqlConnection(connstring);
            Console.WriteLine("kačim se na " + connstring) ;
            connection.Open();
            return connection;
        }

        public void dumpDatabases()
        {
            try
            {
                Process dumperProcess = newDumperProcess();
                var dumpPath = _appSettings.Get("dump_path");
                if(dumpPath == null)
                {
                    Console.WriteLine("Greška : bekap folder nije definisan. Ništa od dampovanja.");
                    Console.ReadLine();
                    Environment.Exit(2);
                }
                var outputStream = new StreamWriter(dumpPath);
                dumperProcess.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                Console.WriteLine($"okidam {dumperProcess.StartInfo.FileName}{dumperProcess.StartInfo.Arguments}");
                dumperProcess.Start();
                dumperProcess.BeginOutputReadLine();
                dumperProcess.WaitForExit();
                outputStream.Dispose();
                Console.WriteLine($"Potraži rezultat u {dumpPath}");
            }
            catch(Exception e)
            {
                Console.WriteLine("Greška. Dampovanje puklo uz eksepšn :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private void checkCommonCredentials()
        {
            foreach (string credentialsAsString in COMMON_CREDENTIALS)
            {
                string[] creds = credentialsAsString.Split("/");
                string userName = creds[0];
                string password = creds[1];
                try
                {
                    Connection = getConnection(_serverName, _port, userName, password);
                    _userName = userName;
                    _password = password;
                }
                catch (Exception)
                {
                    // Nothing, just could not connect with those credentials
                    Console.WriteLine("Neuspešna konekcija");
                }
            }
        }

        private Process newDumperProcess()
        {
            Process process = new Process();
            var dumperPath = _appSettings.Get("dumper_path");
            if (dumperPath == null)
            {
                Console.WriteLine("Nije definisana putanja do fajla mysqldump.exe !");
                Console.ReadLine();
                Environment.Exit(2);
            }
            process.StartInfo.FileName = dumperPath;
            var databasesSpaceSeparatedList = getDatabasesSpaceSeparatedList();
            process.StartInfo.Arguments = $" -h{_serverName} -P{_port} -u{_userName} -p{_password} --databases{databasesSpaceSeparatedList}";
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
