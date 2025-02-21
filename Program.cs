using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AsyncSocketServer
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
        Socket listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, 5000));
        listener.Listen(10);
        Console.WriteLine("Socket Server is running on port 5000...");

        while (true)
        {
            Socket clientSocket = await listener.AcceptAsync();
            await HandleClientAsync(clientSocket);
        }
    }

    private static async Task HandleClientAsync(Socket clientSocket)
    {
        IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
        Console.WriteLine($"Client connected: {clientEndPoint}");
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int received = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0) break;

                string request = Encoding.UTF8.GetString(buffer, 0, received);
                if (request == "exit") break;

                string[] parts = request.Split(' ');
                if (parts.Length != 2 || !currency.ContainsKey(parts[0]) || !currency.ContainsKey(parts[1]))
                {
                    await SendMessageAsync(clientSocket, "Invalid currencies.");
                    continue;
                }

                float result = currency[parts[0]] / currency[parts[1]];
                await SendMessageAsync(clientSocket, result.ToString());
                Console.WriteLine($"{clientEndPoint} requested conversion: {parts[0]} to {parts[1]} - Result: {result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Client disconnected: {clientEndPoint}");
            clientSocket.Close();
        }
    }

    private static async Task SendMessageAsync(Socket clientSocket, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await clientSocket.SendAsync(buffer, SocketFlags.None);
    }
}
