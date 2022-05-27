using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace com.arnet.MySQLBackuper
{
    internal class BackuperJSON
    {
        private BackupingConfig _config;

        private static readonly string DEFAULT_USER_NAME = "oper";
        private static readonly string DEFAULT_PASSWORD = "online";
        private static readonly string COMMON_PASSWORD_WARNING = "[Warning] Using a password on the command line interface can be insecure.";

        private bool _dumperReportedErrors = false;

        public BackuperJSON()
        {
            var config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appconfig.json").Build();

            var section = config.GetSection(nameof(BackupingConfig));
            _config = section.Get<BackupingConfig>();
            if (_config is null)
            {
                Console.WriteLine("Ne mogu da pročitam atribut BackupingConfig. Ništa od dampovanja.");
                Environment.Exit(1);
                Console.ReadLine();
            }
            if (_config.UserName is null)
            {
                _config.UserName = DEFAULT_USER_NAME;
            }
            if (_config.Password is null)
            {
                _config.Password = DEFAULT_PASSWORD;
            }
            if (_config.DumperPath is null)
            {
                Console.WriteLine("Ne mogu da pročitam atribut DumperPath. Ništa od dampovanja.");
                Environment.Exit(2);
                Console.ReadLine();
            }
            if (_config.DumpPath is null)
            {
                Console.WriteLine("Ne mogu da pročitam atribut DumpPath. Ništa od dampovanja.");
                Environment.Exit(3);
                Console.ReadLine();
            }
        }

        public void dumpAll()
        {
            Console.WriteLine("Počinjem bekapovanje mysql servera...");
            foreach (var serverInstance in _config.Servers)
            {
                Console.WriteLine($"    Bekapujem server {serverInstance} u direktorijum {serverInstance.Name}...");
                dumpDatabases(serverInstance);
            }
            string areThereErrors = _dumperReportedErrors ? "MySqlDump je prijavio neke greške!" : "MySqlDump nije prijavio ni jedan problem.";
            Console.WriteLine($"{Environment.NewLine}Bekapovanje završeno. {areThereErrors} Lupi ENTER za gašenje prozora.");
        }

        private void dumpDatabases(MySQLInstance clientInfo)
        {
            try
            {
                string dumpPathForClient = Path.Combine(_config.DumpPath, clientInfo.Name);
                Directory.CreateDirectory(dumpPathForClient);

                foreach (var databaseName in clientInfo.DatabaseNameList) // možda sve baze u cugu ? sve bi išlo u jedan fajl
                {
                    string now = DateTime.Now.ToString("dd.MM.yyyy_HH.mm");
                    Process dumperForCurrentDatabase = newDumperProcess();
                    dumperForCurrentDatabase.StartInfo.Arguments = $" -h{clientInfo.IPAddress} -P{clientInfo.Port} -u{_config.UserName} -p{_config.Password} --databases {databaseName}";
                    string destinationFilePath = Path.Combine(dumpPathForClient, $"dump-{databaseName}_{now}.sql");
                    var outputStream = new StreamWriter(destinationFilePath);
                    dumperForCurrentDatabase.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                    Console.WriteLine($"      > mysqldump {dumperForCurrentDatabase.StartInfo.Arguments} > {destinationFilePath}");
                    dumperForCurrentDatabase.Start();
                    dumperForCurrentDatabase.BeginOutputReadLine();
                    dumperForCurrentDatabase.BeginErrorReadLine();
                    dumperForCurrentDatabase.WaitForExit();
                    outputStream.Dispose();
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"    Greška. Dampovanje za server {clientInfo.Name} puklo uz eksepšn :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(" Ovo mi sve pokaži da vidim o čemu se radi!");
            }
        }

        private Process newDumperProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = _config.DumperPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (sender, args) => {
                if (args is null || args.Data is null)
                    return;
                if (!args.Data.Contains(COMMON_PASSWORD_WARNING))
                {
                    _dumperReportedErrors = true;
                    if (args.Data.Contains("Warning"))
                        Console.WriteLine($"      {args.Data}");
                    else Console.WriteLine($"      ERROR : {args.Data}");
                }
            };
            return process;
        }
    }
}
