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
        public static readonly string DEFAULT_USER_NAME = "oper";
        public static readonly string DEFAULT_PASSWORD = "online";

        private static readonly string[] SYS_DATABASES = { "information_schema", "mysql", "performance_schema", "sys" };
        private static readonly string SHOW_DATABASES = "show databases";

        private NameValueCollection _appSettings;
        private List<ClientInfo> _clients;
        
        private string? _dumperPath;
        private string? _dumpRootPath;
        private string? _userName;
        private string? _password;

        public Backuper()
        {
            _appSettings = ConfigurationManager.GetSection(sectionName: "appSettings") as NameValueCollection;
            if(_appSettings == null)
            {
                Console.WriteLine("Greška : nema sekcije appSettings u App.config fajlu. Ništa od dampovanja.");
                Console.ReadLine();
                Environment.Exit(1);
            }

            _userName = _appSettings.Get("user_name") ?? DEFAULT_USER_NAME;
            _password = _appSettings.Get("password") ?? DEFAULT_PASSWORD;

            _dumperPath = _appSettings.Get("dumper_path");
            if (_dumperPath == null)
            {
                Console.WriteLine("Greška. Nije definisana putanja do fajla mysqldump.exe !");
                Console.ReadLine();
                Environment.Exit(2);
            }
            _dumpRootPath = _appSettings.Get("dump_path");
            if (_dumpRootPath == null)
            {
                Console.WriteLine("Greška : bekap folder nije definisan. Ništa od dampovanja.");
                Console.ReadLine();
                Environment.Exit(3);
            }
            readClientInfo();
        }

        private void readClientInfo()
        {
            string? servers = _appSettings.Get("server_names");
            if (servers is null)
            {
                Console.WriteLine("server_name nije definisan. Ništa od dampovanja.");
                Console.ReadLine();
                Environment.Exit(4);
            }
            string? subdirectories = _appSettings.Get("dump_path_subdirectories");
            if (subdirectories is null)
            {
                Console.WriteLine("dump_path_subdirectories nisu definisani. Ništa od dampovanja.");
                Console.ReadLine(); 
                Environment.Exit(5);
            }

            _clients = new List<ClientInfo>();
            List<string> serverList = servers.Split(", ").ToList();
            List<string> subDirectoryList = subdirectories.Split(", ").ToList();
            for(int i = 0; i < serverList.Count; i++)
            {
                var serverAndPort = serverList[i].Split(":");
                if (serverAndPort.Length > 1)
                {
                    ClientInfo clientInfo = new ClientInfo
                    {
                        ServerIP = serverAndPort[0],
                        Port = serverAndPort[1],
                        DirectoryName = subDirectoryList[i],
                    };
                    _clients.Add(clientInfo);
                }
                else Console.WriteLine($"   Preskačem {serverAndPort[0]}, nema porta.");
            }
        }
        private Process newDumperProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = _dumperPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            return process;
        }

        private List<string> getDatabaseNames(MySqlConnection mySqlInstanceConnection)
        {
            var cmd = new MySqlCommand(SHOW_DATABASES, mySqlInstanceConnection);
            var reader = cmd.ExecuteReader();
            List<string> list = new();
            while (reader.Read())
            {
                if(!SYS_DATABASES.Contains(reader.GetString("database")))
                    list.Add(reader.GetString("database"));
            }
            return list;
        }

        public void dumpAll()
        {
            Console.WriteLine("Počinjem bekapovanje mysql servera...");
            foreach (var clientInfo in _clients)
            {
                Console.WriteLine($"    Bekapujem server {clientInfo.ServerAndPort} u direktorijum {clientInfo.DirectoryName}");
                dumpDatabases(clientInfo);
            }
            Console.WriteLine($"{Environment.NewLine}Bekapovanje završeno. Lupi ENTER za gašenje prozora.");
        }

        private void dumpDatabases(ClientInfo clientInfo)
        {
            try
            {
                string dumpPathForClient = Path.Combine(_dumpRootPath, clientInfo.DirectoryName);
                Directory.CreateDirectory(dumpPathForClient);
                MySqlConnection clientConnection = clientInfo.getConnection(_userName, _password);
                if (clientConnection is null)
                {
                    Console.WriteLine("    Neuspešna konekcija. Ništa od dampovanja za ovaj server.");
                    return;
                }
                List<string> databaseNamesList = getDatabaseNames(clientConnection);
                foreach (var databaseName in databaseNamesList)
                {
                    string now = DateTime.Now.ToString("dd.MM.yyyy_HH.mm");
                    Process dumperForCurrentDatabase = newDumperProcess();
                    dumperForCurrentDatabase.StartInfo.Arguments = $"-h{clientInfo.ServerIP} -P{clientInfo.Port} -u{_userName} -p{_password} --databases {databaseName}";
                    string destinationFilePath = Path.Combine(dumpPathForClient, $"dump-{databaseName}_{now}.sql");
                    var outputStream = new StreamWriter(destinationFilePath);
                    dumperForCurrentDatabase.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                    Console.WriteLine($"      > mysqldump {dumperForCurrentDatabase.StartInfo.Arguments} > {destinationFilePath}");
                    dumperForCurrentDatabase.Start();
                    dumperForCurrentDatabase.BeginOutputReadLine();
                    dumperForCurrentDatabase.WaitForExit();
                    outputStream.Dispose();
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"    Greška. Dampovanje za server {clientInfo.ServerAndPort} puklo uz eksepšn :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(" Ovo mi sve pokaži da vidim o čemu se radi!");
            }
        }
    }
}
