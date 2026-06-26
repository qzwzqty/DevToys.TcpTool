using System.Net;
using System.Net.Sockets;
using DevToys.TcpTool.Core;

namespace DevToys.TcpTool.Networking;

public sealed class TcpServerHost : IAsyncDisposable
{
    private TcpListener? _listener;
    private readonly List<TcpServerClient> _clients = [];
    private CancellationTokenSource? _acceptCts;

    public IReadOnlyList<TcpServerClient> Clients => _clients.AsReadOnly();
    public event EventHandler<LogEntry>? LogReceived;
    public event EventHandler<TcpServerClient>? ClientConnected;
    public event EventHandler<TcpServerClient>? ClientDisconnected;

    public void StartAsync(string ip, int port, CancellationToken ct = default)
    {
        _listener = new TcpListener(IPAddress.Parse(ip), port);
        _listener.Start();
        _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        LogReceived?.Invoke(this, LogEntry.Info("Server", $"Listening on {ip}:{port}"));
        _ = Task.Run(() => AcceptLoopAsync(_acceptCts.Token), _acceptCts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient tcpClient = await _listener!.AcceptTcpClientAsync(ct);
                var serverClient = new TcpServerClient(tcpClient);
                serverClient.LogReceived += (_, e) => LogReceived?.Invoke(this, e);
                serverClient.Disconnected += (_, _) =>
                {
                    _clients.Remove(serverClient);
                    ClientDisconnected?.Invoke(this, serverClient);
                    _ = serverClient.DisposeAsync().AsTask();
                };
                _clients.Add(serverClient);
                ClientConnected?.Invoke(this, serverClient);
                LogReceived?.Invoke(this, LogEntry.Connected("Server", $"{serverClient.RemoteEndPoint} connected"));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, LogEntry.Error("Server", $"Accept error: {ex.Message}"));
        }
    }

    public async Task StopAsync()
    {
        _acceptCts?.Cancel();
        _listener?.Stop();
        LogReceived?.Invoke(this, LogEntry.Info("Server", "Stopped listening"));

        var disconnected = _clients.ToArray();
        _clients.Clear();
        foreach (var client in disconnected)
        {
            ClientDisconnected?.Invoke(this, client);
            await client.DisposeAsync();
        }
    }

    public void RemoveClient(TcpServerClient client)
    {
        _clients.Remove(client);
        ClientDisconnected?.Invoke(this, client);
        _ = client.DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _acceptCts?.Dispose();
    }
}
