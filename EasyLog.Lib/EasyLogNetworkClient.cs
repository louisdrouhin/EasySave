using System.Net.Sockets;
using System.Text.Json;

namespace EasyLog.Lib;

// Network client for sending logs to an EasyLog server
// Uses TCP to connect to the server and send messages formatted as JSON
public class EasyLogNetworkClient
{
    private Socket? _socket;
    private NetworkStream? _stream;
    private StreamWriter? _writer;
    private readonly string _host;
    private readonly int _port;

    // Initializes the client with the server address and port
    // @param host - IP address or domain name of the EasyLog server
    // @param port - port on which the EasyLog server listens (typically 5000)
    public EasyLogNetworkClient(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be null or empty", nameof(host));
        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        _host = host;
        _port = port;
    }

    // Establishes a TCP connection with the EasyLog server
    // Creates a socket, connects to the server, and prepares a stream for writing
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

    // Sends a log message to the EasyLog server
    // @param timestamp - date and time of the log
    // @param name - name of the log (e.g., "UserLogin", "Error", etc.)
    // @param content - dictionary of data associated with the log
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

    // Closes the connection with the EasyLog server and releases resources
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

    // Indicates whether the client is currently connected to the EasyLog server
    public bool IsConnected => _socket?.Connected ?? false;
}
