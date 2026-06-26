using System.Net;
using System.Net.Sockets;
using DevToys.TcpTool.Core;
using DevToys.TcpTool.Networking;

namespace DevToys.TcpTool.Tests;

public sealed class TcpLoopbackTests : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private TcpClient? _acceptedClient;

    [Fact]
    public async Task Client_sends_bytes_to_server()
    {
        _listener.Start();
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Task<byte[]> serverTask = Task.Run(async () =>
        {
            _acceptedClient = await _listener.AcceptTcpClientAsync();
            byte[] buffer = new byte[3];
            int read = await _acceptedClient.GetStream().ReadAsync(buffer);
            return buffer[..read];
        });

        await using TcpClientSession session = new();
        await session.ConnectAsync("127.0.0.1", port);
        await session.SendAsync([0x01, 0x02, 0x03]);

        byte[] received = await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal([0x01, 0x02, 0x03], received);
    }

    [Fact]
    public async Task Server_sends_bytes_to_client()
    {
        _listener.Start();
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        TaskCompletionSource<byte[]> receivedTcs = new();
        Task clientTask = Task.Run(async () =>
        {
            await using TcpClientSession session = new();
            session.LogReceived += (_, entry) =>
            {
                if (entry.Kind == LogEntryKind.Received && entry.Payload is not null)
                    receivedTcs.TrySetResult(entry.Payload);
            };
            await session.ConnectAsync("127.0.0.1", port);
            byte[] received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal([0x0A, 0x0B], received);
        });

        _acceptedClient = await _listener.AcceptTcpClientAsync();
        await _acceptedClient.GetStream().WriteAsync(new byte[] { 0x0A, 0x0B });

        await clientTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TcpServerHost_receives_bytes_from_client()
    {
        _listener.Start();
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _listener.Stop();

        await using TcpServerHost host = new();
        TaskCompletionSource<byte[]> receivedTcs = new();
        host.LogReceived += (_, entry) =>
        {
            if (entry.Kind == LogEntryKind.Received && entry.Payload is not null)
                receivedTcs.TrySetResult(entry.Payload);
        };
        host.StartAsync("127.0.0.1", port);

        using TcpClient client = new();
        await client.ConnectAsync("127.0.0.1", port);
        await client.GetStream().WriteAsync(new byte[] { 0xDE, 0xAD });

        byte[] received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal([0xDE, 0xAD], received);
    }

    public void Dispose()
    {
        _acceptedClient?.Dispose();
        _listener.Stop();
    }
}
