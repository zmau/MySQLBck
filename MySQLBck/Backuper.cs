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
        public static string DUMPER_PATH = "C:\\dev\\arnet\\mysql\\bin\\mysqldump";
        public static string DUMP_PATH = "C:\\dev\\arnet\\dump\\dump.sql";
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
                System.Diagnostics.Process pProcess = new System.Diagnostics.Process();

                /*   pProcess.StartInfo.FileName = $"{DUMPER_PATH}";
                   //pProcess.StartInfo.FileName = "cmd.exe";

                   //strCommandParameters are parameters to pass to program
                   pProcess.StartInfo.Arguments = $" -hlocalhost -uroot -proot --databases db1";
   //                pProcess.StartInfo.Arguments = $" -hlocalhost -uroot -proot --all-databases > {DUMP_LOCATION}";
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

                   //Set output of program to be written to process output stream
                   pProcess.StartInfo.RedirectStandardOutput = true;

                   //Optional
                   //pProcess.StartInfo.WorkingDirectory = strWorkingDirectory;

                   //Start the process
                   pProcess.Start();

     //              string strOutput = pProcess.StandardOutput.ReadToEnd();
   //                Console.WriteLine(strOutput);

                   Console.ReadLine();
                   //Wait for process to finish
                   pProcess.WaitForExit();*/
                /* Process cmd = new Process();
                 cmd.StartInfo.FileName = "cmd.exe";
                 cmd.StartInfo.Arguments = strCmdText;
                 cmd.StartInfo.RedirectStandardInput = true;
                 cmd.StartInfo.RedirectStandardOutput = true;
                 cmd.StartInfo.CreateNoWindow = true;
                 cmd.StartInfo.UseShellExecute = false;
                 cmd.Start();
                 string strOutput = cmd.StandardOutput.ReadToEnd();

                 cmd.StandardInput.WriteLine("echo Oscar");
                 cmd.StandardInput.Flush();
                 cmd.StandardInput.Close();
                 cmd.WaitForExit();
                 Console.WriteLine(cmd.StandardOutput.ReadToEnd());*/


                var strCmdText = $"{pProcess.StartInfo.FileName}{pProcess.StartInfo.Arguments}";
                Console.WriteLine(strCmdText);
                System.Diagnostics.Process.Start("CMD.exe", strCmdText);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
