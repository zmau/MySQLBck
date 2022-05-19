// See https://aka.ms/new-console-template for more information
using com.arnet.MySQLBck;

Backuper bck = new Backuper();
if (bck.FoundCorrectCredentials)
    bck.dumpDatabases();
else Console.WriteLine("Nisam uspeo da nađem parametre za kačenje na bazu. Ništa od bekapa.");
Console.ReadLine();