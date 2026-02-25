using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using EasyLog.Lib;
using EasyLogLib = EasyLog.Lib.EasyLog;

namespace EasyLog.Server;

// Serveur de logs utilisant des sockets TCP
// Écoute les connexions entrantes, reçoit des messages JSON, et les écrit dans des fichiers de log
public class SocketServer
{
    private Socket? _listenerSocket;
    private readonly int _port;
    private readonly EasyLogLib _logger;
    private bool _isRunning;

    // Initialise le serveur avec le port d'écoute et le répertoire de logs
    // @param port - port sur lequel le serveur écoute (généralement 5000)
    // @param logDirectory - répertoire où les fichiers de log seront stockés
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

    // Démarre le serveur et commence à écouter les connexions entrantes
    // Crée un socket, le lie à l'adresse IP et au port, et accepte les clients en boucle
    // Chaque client est traité de manière asynchrone pour permettre des connexions simultanées
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

    // Gère la communication avec un client connecté
    // Lit les messages JSON envoyés par le client, les désérialise, et les écrit dans les fichiers de log
    // @param clientSocket - socket du client connecté
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

    // Arrête le serveur en fermant le socket d'écoute et en libérant les ressources
    // Ferme le socket, arrête d'accepter les clients, et ferme le logger
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
