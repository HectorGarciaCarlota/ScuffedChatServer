using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class ChatServer
{
    private TcpListener listener;
    private readonly int port = 8000;
    private bool isRunning = false;
    private ConcurrentDictionary<TcpClient, StreamWriter> clients = new ConcurrentDictionary<TcpClient, StreamWriter>();

    public ChatServer()
    {
        listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        isRunning = true;
        listener.Start();
        AcceptClients();
    }

    public void Stop()
    {
        isRunning = false;
        foreach (var client in clients.Keys)
        {
            client.Close();
        }
        listener.Stop();
    }

    private async void AcceptClients()
    {
        while (isRunning)
        {
            try
            {
                var tcpClient = await listener.AcceptTcpClientAsync();
                var streamWriter = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
                clients.TryAdd(tcpClient, streamWriter);

                // Start a new task to handle the communication
                Task.Run(() => HandleClient(tcpClient, streamWriter));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private async Task HandleClient(TcpClient tcpClient, StreamWriter writer)
    {
        var reader = new StreamReader(tcpClient.GetStream());
        while (isRunning)
        {
            try
            {
                var message = await reader.ReadLineAsync();
                if (message == null) // Client has disconnected
                    break;

                Console.WriteLine("Received: " + message);
                await BroadcastMessage(message, tcpClient); // Pass the sender's TcpClient
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }

        clients.TryRemove(tcpClient, out _);
        tcpClient.Close();
    }

    private async Task BroadcastMessage(string message, TcpClient senderTcpClient)
    {
        foreach (var client in clients.Keys)
        {
            if (client != senderTcpClient) // Check if the client is not the sender
            {
                StreamWriter writer = clients[client];
                await writer.WriteLineAsync("Nigga: " + message);
            }
        }
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new ChatServer();
            server.Start();

            Console.WriteLine("Chat server started. Press any key to exit.");
            Console.ReadKey();

            server.Stop();
            Console.WriteLine("Chat server stopped.");
        }
    }
}
