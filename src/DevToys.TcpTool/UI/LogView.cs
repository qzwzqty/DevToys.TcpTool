using DevToys.Api;
using DevToys.TcpTool.Core;
using static DevToys.Api.GUI;

namespace DevToys.TcpTool.UI;

internal static class LogView
{
    public static string Glyph(LogEntryKind kind) => kind switch
    {
        LogEntryKind.Connected => "\u25B2",
        LogEntryKind.Disconnected => "\u25BC",
        LogEntryKind.Sent => "\u2192",
        LogEntryKind.Received => "\u2190",
        LogEntryKind.Error => "\u2715",
        _ => "\u00B7"
    };

    public static string Format(LogEntry entry)
        => $"[{entry.Timestamp:HH:mm:ss}] {Glyph(entry.Kind)} {entry.Source}: {entry.Message}";

    public static IUIListItem[] BuildListItems(IEnumerable<LogEntry> logs)
        => logs.Reverse()
            .Select(entry => Item(
                Label().Style(UILabelStyle.Caption).Text(Format(entry)),
                value: null))
            .ToArray();
}
