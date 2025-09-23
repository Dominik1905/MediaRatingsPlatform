using System.Data.Common;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starte MRP-Server...");
        var server = new HttpServer("http://localhost:8080/");
        server.Start();
        Console.WriteLine("Drücke ENTER zum Beenden...");
        Console.ReadLine();
    }
}