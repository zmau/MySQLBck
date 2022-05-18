using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.arnet.MySQLBck
{
    internal class Backuper
    {
        public static readonly string DUMPER_PATH = "C:\\dev\\arnet\\mysql\\bin\\mysqldump";
        public static readonly string DUMP_PATH = "C:\\dev\\arnet\\dump\\dump.sql";
        private MySqlConnection Connection { get; set; }

        public Backuper()
        {
            Connection = getConnection("localhost", "root", "root");
        }
        public MySqlConnection getConnection(string serverName, string userName, string password)
        {
            string connstring = string.Format("Server={0}; UID={1}; password={2}", serverName, userName, password);
            var connection = new MySqlConnection(connstring);
            connection.Open();
            return connection;
        }

        public void query()
        {
            string query = "use db1; SELECT count(*) cnt FROM tasks";
            var cmd = new MySqlCommand(query, Connection);
            var reader = cmd.ExecuteReader();
            reader.Read();
            int count = reader.GetInt32("cnt");
            Console.WriteLine(count);
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
                Process pProcess = new Process();
                pProcess.StartInfo.FileName = $"{DUMPER_PATH}";
                pProcess.StartInfo.Arguments = $" -hlocalhost -uroot -proot --databases db1";
                Console.WriteLine($"{pProcess.StartInfo.FileName}{pProcess.StartInfo.Arguments}");

                var outputStream = new StreamWriter(DUMP_PATH);
                //pProcess.OutputDataReceived += (sender, args) => outputStream.WriteLine(args.Data);
                pProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        outputStream.WriteLine(e.Data);
                    }
                }); 
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.CreateNoWindow = true;

                pProcess.StartInfo.RedirectStandardOutput = true;

                //Optional
                //pProcess.StartInfo.WorkingDirectory = strWorkingDirectory;

                pProcess.Start();

                Console.ReadLine();
                pProcess.WaitForExit();
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
    }
}
