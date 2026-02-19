using EasyLog.Server;

var port = int.Parse(Environment.GetEnvironmentVariable("LOG_PORT") ?? "5000");
var logDirectory = Environment.GetEnvironmentVariable("LOG_DIR") ?? Path.Combine(AppContext.BaseDirectory, "logs");

Console.WriteLine($"Starting EasyLog Server...");
Console.WriteLine($"Port: {port}");
Console.WriteLine($"Log Directory: {logDirectory}");
Console.WriteLine();

try
{
    var server = new SocketServer(port, logDirectory);

    // Graceful shutdown on Ctrl+C
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Shutdown signal received");
        server.Stop();
    };

    server.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    Environment.Exit(1);
}
