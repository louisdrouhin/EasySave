using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using EasyLog.Lib;
using EasyLogLib = EasyLog.Lib.EasyLog;

namespace EasyLog.Server;

// Log server using TCP sockets
// Listens for incoming connections, receives JSON messages, and writes them to log files
public class SocketServer
{
    private Socket? _listenerSocket;
    private readonly int _port;
    private readonly EasyLogLib _logger;
    private bool _isRunning;

    // Initializes the server with the listening port and log directory
    // @param port - port on which the server listens (typically 5000)
    // @param logDirectory - directory where log files will be stored
    public SocketServer(int port, string logDirectory)
    {
        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("Log directory cannot be null or empty", nameof(logDirectory));

        _port = port;
        _logger = new EasyLogLib(new JsonLogFormatter(), logDirectory);
        _isRunning = false;
    }

    // Starts the server and begins listening for incoming connections
    // Creates a socket, binds it to the IP address and port, and accepts clients in a loop
    // Each client is handled asynchronously to allow simultaneous connections
    public void Start()
    {
        try
        {
            _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listenerSocket.Listen(100);

            _isRunning = true;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EasyLog Server listening on port {_port}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log directory: {_logger.GetLogDirectory()}");

            while (_isRunning && _listenerSocket != null)
            {
                try
                {
                    Socket clientSocket = _listenerSocket.Accept();
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] New client connected from {clientSocket.RemoteEndPoint}");
                    _ = HandleClientAsync(clientSocket);
                }
                catch (SocketException ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Socket error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Server error: {ex.Message}");
            throw;
        }
        finally
        {
            Stop();
        }
    }

    // Handles communication with a connected client
    // Reads JSON messages sent by the client, deserializes them, and writes them to log files
    // @param clientSocket - socket of the connected client
    private async Task HandleClientAsync(Socket clientSocket)
    {
        var clientEndpoint = clientSocket.RemoteEndPoint?.ToString() ?? "Unknown";

        try
        {
            using (var networkStream = new NetworkStream(clientSocket))
            using (var reader = new StreamReader(networkStream))
            {
                string line;
                int messageCount = 0;

                while ((line = await reader.ReadLineAsync()) != null && _isRunning)
                {
                    try
                    {
                        var logData = JsonSerializer.Deserialize<JsonElement>(line);

                        var timestamp = logData.GetProperty("timestamp").GetDateTime();
                        var name = logData.GetProperty("name").GetString() ?? "Unknown";
                        var contentJson = logData.GetProperty("content").GetRawText();
                        var content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson) ?? new Dictionary<string, object>();

                        content["clientIp"] = clientEndpoint;

                        _logger.Write(timestamp, name, content);
                        messageCount++;
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Invalid JSON from client: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error processing log: {ex.Message}");
                    }
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Client disconnected ({messageCount} messages received)");
            }
        }
        catch (IOException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Client {clientEndpoint} disconnected unexpectedly: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Client handler error: {ex.Message}");
        }
        finally
        {
            try
            {
                clientSocket.Close();
                clientSocket.Dispose();
            }
            catch { }
        }
    }

    // Stops the server by closing the listening socket and releasing resources
    // Closes the socket, stops accepting clients, and closes the logger
    public void Stop()
    {
        _isRunning = false;

        try
        {
            _listenerSocket?.Close();
            _listenerSocket?.Dispose();
            _logger?.Close();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Server stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error stopping server: {ex.Message}");
        }
    }
}
