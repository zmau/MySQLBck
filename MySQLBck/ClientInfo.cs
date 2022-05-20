using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.arnet.MySQLBck
{
    public class ClientInfo
    {
        public string? ServerIP { get; set; }
        public string? Port { get; set; }
        public string? DirectoryName { get; set; }
        private MySqlConnection? _connection { get; set; }

        public string ServerAndPort{
            get {return $"{ServerIP }:{Port}"; }
        }

        public MySqlConnection? getConnection(string userName, string password)
        {
            if (_connection == null)
            {
                try
                {
                    string connstring = string.Format("Server={0}; port={1}; UID={2}; password={3}", ServerIP, Port, userName, password);
                    MySqlConnection connection = new MySqlConnection(connstring);
                    connection.Open();
                    return connection;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return _connection;
        }


    }
}
