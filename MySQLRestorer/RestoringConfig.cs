using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLRestorer
{
    internal class RestoringConfig
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string RestoringServer { get; set; }
        public string InputFile { get; set; }

        public string ServerOnly { get { return RestoringServer.Split(":")[0]; } }
        public string Port { get { return RestoringServer.Split(":")[1]; } }
    }
}
