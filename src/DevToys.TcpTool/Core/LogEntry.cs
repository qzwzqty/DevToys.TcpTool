namespace DevToys.TcpTool.Core;
public sealed record LogEntry(DateTimeOffset Timestamp, LogEntryKind Kind, string Source, string Message, byte[]? Payload)
{
    public static LogEntry Info(string s, string m) => new(DateTimeOffset.Now, LogEntryKind.Info, s, m, null);
    public static LogEntry Connected(string s, string m) => new(DateTimeOffset.Now, LogEntryKind.Connected, s, m, null);
    public static LogEntry Disconnected(string s, string m) => new(DateTimeOffset.Now, LogEntryKind.Disconnected, s, m, null);
    public static LogEntry Sent(string s, string m, byte[] p) => new(DateTimeOffset.Now, LogEntryKind.Sent, s, m, p);
    public static LogEntry Received(string s, string m, byte[] p) => new(DateTimeOffset.Now, LogEntryKind.Received, s, m, p);
    public static LogEntry Error(string s, string m) => new(DateTimeOffset.Now, LogEntryKind.Error, s, m, null);
}
