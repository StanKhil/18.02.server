using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AsyncTCPServer
{
    private static readonly Dictionary<string, float> currency = new()
    {
        { "EURO", 0.85f },
        { "POUND", 0.73f },
        { "DOLLAR", 1.0f },
        { "GRIVNA", 40.0f },
        { "LEI", 4.0f }
    };

    private static readonly Dictionary<string, int> clientRequests = new();
    private const int MaxRequests = 5;

    static async Task Main()
    {
        TcpListener listener = new(IPAddress.Loopback, 5000);
        listener.Start();
        Console.WriteLine("Server is running on 127.0.0.1:5000...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            await HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var endPoint = client.Client.RemoteEndPoint.ToString();
        Console.WriteLine($"Client connected: {endPoint}");
        NetworkStream stream = client.GetStream();



        try
        {
            while (true)
            {
                string request = await ReceiveMessageAsync(stream);
                if (request == "exit") break;

                if (clientRequests.ContainsKey(endPoint) && clientRequests[endPoint] >= MaxRequests)
                {
                    await SendMessageAsync(stream, "Request limit exceeded. Try again later.");
                    client.Close();
                    return;
                }

                string[] parts = request.Split(' ');
                if (parts.Length != 2 || !currency.ContainsKey(parts[0]) || !currency.ContainsKey(parts[1]))
                {
                    await SendMessageAsync(stream, "Invalid currencies.");
                    continue;
                }

                float result = currency[parts[0]] / currency[parts[1]];
                await SendMessageAsync(stream, result.ToString());
                Console.WriteLine($"{endPoint} requested conversion: {parts[0]} to {parts[1]} - Result: {result}");

                if (!clientRequests.ContainsKey(endPoint))
                    clientRequests[endPoint] = 1;
                else
                    clientRequests[endPoint]++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Client disconnected: {endPoint}");
            client.Close();
        }
    }

    private static async Task<string> ReceiveMessageAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int received = await stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    private static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(buffer, 0, buffer.Length);
    }
}
