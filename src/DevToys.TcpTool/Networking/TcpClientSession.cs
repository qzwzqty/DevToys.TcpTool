using System.Net;
using System.Net.Sockets;
using DevToys.TcpTool.Core;

namespace DevToys.TcpTool.Networking;

public sealed class TcpClientSession : IAsyncDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _readCts;
    private IPEndPoint? _lastLocalEndPoint;
    private IPEndPoint? _lastRemoteEndPoint;

    public bool IsConnected => _client?.Connected ?? false;
    public IPEndPoint? LocalEndPoint => _client?.Client.LocalEndPoint as IPEndPoint ?? _lastLocalEndPoint;
    public IPEndPoint? RemoteEndPoint => _client?.Client.RemoteEndPoint as IPEndPoint ?? _lastRemoteEndPoint;
    public event EventHandler<LogEntry>? LogReceived;
    public event EventHandler<bool>? ConnectionChanged;

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        if (_client is not null) await DisconnectAsync();
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, ct);
        _stream = _client.GetStream();
        _readCts = new CancellationTokenSource();
        _lastLocalEndPoint = _client.Client.LocalEndPoint as IPEndPoint;
        _lastRemoteEndPoint = _client.Client.RemoteEndPoint as IPEndPoint;
        _ = Task.Run(() => ReceiveLoopAsync(_readCts.Token), _readCts.Token);
        LogReceived?.Invoke(this, LogEntry.Connected("Client", $"Connected to {host}:{port}"));
        ConnectionChanged?.Invoke(this, true);
    }

    public async Task SendAsync(byte[] payload, CancellationToken ct = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected.");
        await _stream.WriteAsync(payload, ct);
        LogReceived?.Invoke(this, LogEntry.Sent("Client", $"Sent {payload.Length} bytes", payload));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested && _stream is not null)
            {
                int read = await _stream.ReadAsync(buffer, ct);
                if (read == 0) break;
                byte[] payload = buffer[..read];
                LogReceived?.Invoke(this, LogEntry.Received("Client", $"Received {read} bytes", payload));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            LogReceived?.Invoke(this, LogEntry.Error("Client", $"Receive error: {ex.Message}"));
        }
        finally
        {
            LogReceived?.Invoke(this, LogEntry.Disconnected("Client", "Disconnected"));
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public async Task DisconnectAsync()
    {
        bool wasConnected = _client is not null;
        if (_readCts is not null)
        {
            _readCts.Cancel();
            _readCts.Dispose();
            _readCts = null;
        }
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
        if (_client is not null)
        {
            _client.Close();
            _client.Dispose();
            _client = null;
        }
        if (wasConnected)
        {
            LogReceived?.Invoke(this, LogEntry.Disconnected("Client", "Disconnected"));
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
