using System.Net;
using System.Net.Sockets;
using DevToys.TcpTool.Core;

namespace DevToys.TcpTool.Networking;

public sealed class TcpServerClient : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _readCts = new();
    private volatile bool _disposed;
    public IPEndPoint RemoteEndPoint { get; }

    public event EventHandler<LogEntry>? LogReceived;
    public event EventHandler? Disconnected;

    public TcpServerClient(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
        _ = Task.Run(() => ReceiveLoopAsync(_readCts.Token), _readCts.Token);
    }

    public async Task SendAsync(byte[] payload, CancellationToken ct = default)
    {
        await _stream.WriteAsync(payload, ct);
        LogReceived?.Invoke(this, LogEntry.Sent($"Server->{RemoteEndPoint}", $"Sent {payload.Length} bytes", payload));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int read = await _stream.ReadAsync(buffer, ct);
                if (read == 0) break;
                byte[] payload = buffer[..read];
                LogReceived?.Invoke(this, LogEntry.Received($"Server<-{RemoteEndPoint}", $"Received {read} bytes", payload));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            LogReceived?.Invoke(this, LogEntry.Error($"{RemoteEndPoint}", $"Error: {ex.Message}"));
        }
        finally
        {
            LogReceived?.Invoke(this, LogEntry.Disconnected($"{RemoteEndPoint}", "Client disconnected"));
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _readCts.Cancel();
        _readCts.Dispose();
        await _stream.DisposeAsync();
        _client.Dispose();
    }
}
