// See https://aka.ms/new-console-template for more information
using com.arnet.MySQLBackuper;
using com.arnet.MySQLBck;

BackuperJSON bck = new BackuperJSON();
bck.dumpAll();
Console.ReadLine();