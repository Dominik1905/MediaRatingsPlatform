

using Microsoft.Data.SqlClient;

namespace DatabaseObjects;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "jdbc:postgresql://myuser:mypassword@localhost:15432/mydb";
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            Console.WriteLine("Verbindung erfolgreich!");
        }
    }
}