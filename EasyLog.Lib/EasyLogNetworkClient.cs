using System.Net.Sockets;
using System.Text.Json;

namespace EasyLog.Lib;

// Client réseau pour envoyer des logs à un serveur EasyLog
// Utilise TCP pour se connecter au serveur et envoyer des messages formatés en JSON
public class EasyLogNetworkClient
{
    private Socket? _socket;
    private NetworkStream? _stream;
    private StreamWriter? _writer;
    private readonly string _host;
    private readonly int _port;

    // Initialise le client avec l'adresse du serveur et le port
    // @param host - adresse IP ou nom de domaine du serveur EasyLog
    // @param port - port sur lequel le serveur EasyLog écoute (généralement 5000)
    public EasyLogNetworkClient(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be null or empty", nameof(host));
        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        _host = host;
        _port = port;
    }

    // Établit une connexion TCP avec le serveur EasyLog
    // Crée un socket, se connecte au serveur, et prépare un flux pour l'écriture
    public void Connect()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_host, _port);
            _stream = new NetworkStream(_socket);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to EasyLog server at {_host}:{_port}",
                ex);
        }
    }

    // Envoie un message de log au serveur EasyLog
    // @param timestamp - date et heure du log
    // @param name - nom du log (ex: "UserLogin", "Error", etc.)
    // @param content - dictionnaire de données associées au log
    public void Send(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (_writer == null || _socket == null || !_socket.Connected)
            throw new InvalidOperationException("Client is not connected. Call Connect() first.");

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        try
        {
            var logData = new
            {
                timestamp,
                name,
                content
            };

            var json = JsonSerializer.Serialize(logData);
            _writer.WriteLine(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to send log message to server",
                ex);
        }
    }

    // Ferme la connexion avec le serveur EasyLog et libère les ressources
    public void Disconnect()
    {
        try
        {
            _writer?.Close();
            _writer?.Dispose();
            _stream?.Close();
            _stream?.Dispose();
            _socket?.Close();
            _socket?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during disconnect - {ex.Message}");
        }
    }

    // Indique si le client est actuellement connecté au serveur EasyLog
    public bool IsConnected => _socket?.Connected ?? false;
}
