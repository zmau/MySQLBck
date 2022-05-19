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

        private MySqlConnection? Connection { get; set; }
        private NameValueCollection _appSettings;

        private readonly string _serverName;
        private readonly string _port;
        private string? _userName;
        private string? _password;
        private string? _destinationFilePath;
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
                Connection = getConnection(_serverName, _port, userNameFromSettings, passwordFromSettings);
            }
            if (!FoundCorrectCredentials)
                checkCommonCredentials();
        }
        private MySqlConnection? getConnection(string serverName, string port, string userName, string password)
        {
            try
            {
                string connstring = string.Format("Server={0}; port={1}; UID={2}; password={3}", serverName, port, userName, password);
                var connection = new MySqlConnection(connstring);
                Console.WriteLine("kačim se na " + connstring);
                connection.Open();
                _userName = userName;
                _password = password;
                Console.WriteLine("Uspeo da se konektujem sa ovim parametrima!");
                return connection;
            }
            catch (Exception)
            {
                Console.WriteLine("Neuspešna konekcija");
                return null;
            }

        }

        private void checkCommonCredentials()
        {
            foreach (string credentialsAsString in COMMON_CREDENTIALS)
            {
                if (!FoundCorrectCredentials)
                {
                    string[] creds = credentialsAsString.Split("/");
                    Connection = getConnection(_serverName, _port, creds[0], creds[1]);
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
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            return process;
        }

        private List<string> getDatabaseNames()
        {
            string query = "show databases";
            var cmd = new MySqlCommand(query, Connection);
            var reader = cmd.ExecuteReader();
            List<string> list = new List<string>();
            while (reader.Read())
            {
                if(!SYS_DATABASES.Contains(reader.GetString("database")))
                    list.Add(reader.GetString("database"));
            }
            return list;
        }

        public void dumpDatabases()
        {
            try
            {
                var dumpPath = _appSettings.Get("dump_path");
                if (dumpPath == null)
                {
                    Console.WriteLine("Greška : bekap folder nije definisan. Ništa od dampovanja.");
                    Console.ReadLine();
                    Environment.Exit(2);
                }
                var databaseNamesList = getDatabaseNames();
                foreach (var databaseName in databaseNamesList)
                {
                    var dumperForCurrentDatabase = newDumperProcess();
                    dumperForCurrentDatabase.StartInfo.Arguments = $" -h{_serverName} -P{_port} -u{_userName} -p{_password} --databases {databaseName}";
                    _destinationFilePath = Path.Combine(dumpPath, $"dump-{databaseName}.sql");
                    var outputStream = new StreamWriter(_destinationFilePath);
                    dumperForCurrentDatabase.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                    Console.WriteLine($"okidam {GetCommandLineString(dumperForCurrentDatabase.StartInfo)}");
                    dumperForCurrentDatabase.Start();
                    dumperForCurrentDatabase.BeginOutputReadLine();
                    dumperForCurrentDatabase.WaitForExit();
                    outputStream.Dispose();
                    Console.WriteLine($"Potraži rezultat u {_destinationFilePath}");
                }
                Console.WriteLine("Bekapovanje završeno. Lupi ENTER za gašenje prozora.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Greška. Dampovanje puklo uz eksepšn :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        private string GetCommandLineString(ProcessStartInfo StartInfo)
        {
            return $"{StartInfo.FileName}{StartInfo.Arguments} > {_destinationFilePath}";
        }


    }
}
