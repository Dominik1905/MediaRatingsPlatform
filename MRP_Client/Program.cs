namespace MRP_Client;

class Program
{
    static async Task Main(string[] args)
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:8080/products");
        Console.WriteLine("Antwort vom Server:");
        Console.WriteLine(response);
    }
}