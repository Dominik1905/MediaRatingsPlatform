using System;
using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;

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
        Console.WriteLine("Server läuft...");
        Listen();
    }

    private async void Listen()
    {
        while (true)
        {
            var context = await _listener.GetContextAsync();
            _ = Task.Run(() => Router.Handle(context));
        }
    }
}