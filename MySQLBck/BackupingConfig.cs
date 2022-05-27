using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.arnet.MySQLBackuper
{
	public class MySQLInstance
	{
		public string IPAddress { get; set; }
		public string Port { get; set; }
		public string Name { get; set; }
		public string Databases { get; set; }

		public List<string> DatabaseNameList
		{
			get
			{
				return Databases.Split(" ").ToList();
			}
		}
		public override string ToString()
        {
			return $"{IPAddress}:{Port} {Databases}";
        }
	}

	public class BackupingConfig
	{
		public string UserName { get; set; }
		public string Password { get; set; }

		public string DumperPath { get; set; }
		public string DumpPath { get; set; }

		public List<MySQLInstance> Servers { get; set; }

		public override string ToString()
		{
			return $"{UserName}/{Password} {DumpPath}";
		}
	}
}
