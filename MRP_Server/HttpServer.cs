using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MediaRatingsPlatform;

public class HttpServer
{
    private readonly HttpListener _listener;

    public HttpServer(string prefix)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine("Server läuft auf " + string.Join(", ", _listener.Prefixes));
        _ = Listen();
    }


    private async Task Listen()
    {
        while (true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Router.Handle(context);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Request Error] {ex.Message}");
                        context.Response.StatusCode = 500;
                        var errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                        var buffer = Encoding.UTF8.GetBytes(errorJson);
                        context.Response.ContentType = "application/json";
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.Close();
                    }
                });
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"[Listener Warning] {ex.Message} (ErrorCode {ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Listener Error] {ex.Message}");
            }
        }
    }
}