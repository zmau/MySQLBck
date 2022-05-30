using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Text;

namespace MySQLRestorer
{
    internal class Restorer
    {
        private readonly RestoringConfig _config;
        private static readonly string DEFAULT_USER_NAME = "oper";
        private static readonly string DEFAULT_PASSWORD = "online";

        public Restorer()
        {
            try
            {
                var config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appconfig.json").Build();

                var section = config.GetSection(nameof(RestoringConfig));
                _config = section.Get<RestoringConfig>();
                if (_config is null)
                {
                    Console.WriteLine("Ne mogu da pročitam atribut RestoringConfig. Ništa od ristorovanja.");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
                if (_config.UserName is null)
                {
                    _config.UserName = DEFAULT_USER_NAME;
                }
                if (_config.Password is null)
                {
                    _config.Password = DEFAULT_PASSWORD;
                }
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Console.WriteLine("Nekorektan format nekog od konfiguracionih parametara. Ništa od ristorovanja.");
                if (ex.InnerException is not null)
                {
                    Console.WriteLine($"Detalji : {ex.InnerException.StackTrace}");
                }
                Console.ReadLine();
                Environment.Exit(2);
            }
        }

        public void restore()
        {
            try
            {
                Console.WriteLine($"Generišem bazu iz fajla { _config.InputFile} na server { _config.RestoringServer}");
                string connstring = string.Format("Server={0}; port={1}; database=mysql; UID={2}; password={3}", _config.ServerOnly, _config.Port, _config.UserName, _config.Password);
                MySqlConnection connection = new MySqlConnection(connstring);
                connection.Open();

                MySqlScript script = new MySqlScript(connection, File.ReadAllText(_config.InputFile));
                script.Delimiter = ";";
                script.Execute();
                Console.WriteLine("Generisanje baze završeno. Proveri na serveru.");
            }
            catch (MySqlException e)
            {
                Console.WriteLine($"Greška. Generisanje baze puklo uz eksepšn :");
                Console.WriteLine(e.Message);
                Console.WriteLine("Proveri parametre konekcije. Probaj da ručno pristupiš serveru sa tim parametrima.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Greška. Generisanje baze puklo uz eksepšn :");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(" Ovo mi sve pokaži da vidim o čemu se radi!");
            }
        }

    }
}
