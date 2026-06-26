using DevToys.Api;
using DevToys.TcpTool.Core;
using DevToys.TcpTool.Networking;
using static DevToys.Api.GUI;

namespace DevToys.TcpTool.UI;

internal sealed class ClientPanel
{
    private readonly TcpClientSession _client = new();
    private readonly List<LogEntry> _logs = [];

    private string _host = "";
    private int _port;
    private string _sendText = "";
    private bool _sendAsText;
    private string _localLabel = "";
    private bool _showLog = true;

    private readonly IUIButton _connectButton = Button("connect");
    private readonly IUIButton _disconnectButton = Button("disconnect");
    private readonly IUISingleLineTextInput _hostInput = SingleLineTextInput("host");
    private readonly IUINumberInput _portInput = NumberInput("port");
    private readonly IUIInfoBar _errorBar = InfoBar("clientError");
    private readonly IUIProgressRing _connectingRing = ProgressRing("connectingRing");
    private readonly IUIList _timelineList = List("clientTimeline");
    private readonly IUIGrid _root = Grid("clientGrid");

    private enum Row { Main }
    private enum TimelineRow { Scroll }
    private enum Column { Main }

    public ClientPanel()
    {
        _client.LogReceived += OnLog;
        _client.ConnectionChanged += OnConnectionChanged;
        Build();
    }

    public IUIGrid RootElement => _root;

    private void Build()
    {
        _connectButton.Text(Strings.ConnectButton).AccentAppearance().OnClick(OnConnectAsync);
        _disconnectButton.Text(Strings.DisconnectButton).OnClick(OnDisconnectAsync).Disable();

        _root
            .RowSmallSpacing()
            .Rows((Row.Main, Auto))
            .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
            .Cells(
                Cell(Row.Main, Column.Main,
                    Stack().Vertical().SmallSpacing().WithChildren(
                        _errorBar.Error().Closable().Close(),
                        Stack().Horizontal().SmallSpacing().WithChildren(
                            _hostInput.Title(Strings.HostTitle).Text(_host).HideCommandBar()
                                .OnTextChanged(v => { _host = v; return ValueTask.CompletedTask; }),
                            _portInput.Title(Strings.PortTitle).Minimum(0).Maximum(65535).Value(_port).HideCommandBar()
                                .OnValueChanged(v => { _port = (int)v; return ValueTask.CompletedTask; }),
                            _connectButton,
                            _disconnectButton,
                            _connectingRing.Hide()),
                        Stack().Horizontal().SmallSpacing().WithChildren(
                            Label().Style(UILabelStyle.BodyStrong).Text(Strings.MessagesTitle),
                            Switch("showClientLog")
                                .OnText(Strings.ShowLogToggle)
                                .OffText(Strings.ShowLogToggle)
                                .On()
                                .OnToggle(isOn => { _showLog = isOn; RefreshTimeline(); return ValueTask.CompletedTask; })),
                        Grid("clientTimelineGrid")
                            .Rows((TimelineRow.Scroll, new UIGridLength(360, UIGridUnitType.Pixel)))
                            .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                            .Cells(Cell(TimelineRow.Scroll, Column.Main, _timelineList.ForbidSelectItem())),
                        MultiLineTextInput("clientSend")
                            .Title(Strings.TextToSendTitle)
                            .Text(_sendText)
                            .Extendable()
                            .CommandBarExtraContent(
                                SelectDropDownList("clientFormat")
                                    .WithItems(
                                        Item(Strings.FormatHex, false),
                                        Item(Strings.FormatText, true))
                                    .Select(0)
                                    .OnItemSelected(v => { _sendAsText = v?.Value is true; return ValueTask.CompletedTask; }))
                            .OnTextChanged(v => { _sendText = v; return ValueTask.CompletedTask; }),
                        Button("clientSendBtn").Text(Strings.SendButton).AccentAppearance().OnClick(OnSendAsync))));
    }

    private void OnLog(object? sender, LogEntry entry)
    {
        _logs.Add(entry);
        RefreshTimeline();
    }

    private void RefreshTimeline()
    {
        IUIListItem[] items = _logs
            .AsEnumerable()
            .Reverse()
            .Select(BuildTimelineItem)
            .Where(x => x is not null)
            .Select(x => Item(x!, value: null))
            .ToArray();

        _timelineList.WithItems(items);
    }

    private IUIElement? BuildTimelineItem(LogEntry entry)
    {
        if (entry is { Kind: LogEntryKind.Sent, Payload: not null })
        {
            return ChatBubble(_localLabel, entry.Timestamp, HexPayloadConverter.ToHex(entry.Payload), fromSelf: true);
        }

        if (entry is { Kind: LogEntryKind.Received, Payload: not null })
        {
            string serverLabel = $"{Strings.ServerSenderName} {_client.RemoteEndPoint?.Address}";
            return ChatBubble(serverLabel, entry.Timestamp, HexPayloadConverter.ToHex(entry.Payload), fromSelf: false);
        }

        if (!_showLog)
        {
            return null;
        }

        return Label()
            .Style(UILabelStyle.Caption)
            .AlignHorizontally(UIHorizontalAlignment.Center)
            .Text(LogView.Format(entry));
    }

    private static IUIElement ChatBubble(string sender, DateTimeOffset timestamp, string content, bool fromSelf)
    {
        IUIStack bubble = Stack().Vertical().NoSpacing().WithChildren(
            Label().Style(UILabelStyle.Caption).Text($"{sender}  {timestamp:HH:mm:ss}"),
            Label().Style(UILabelStyle.Body).Text(content));

        return Card(bubble)
            .AlignHorizontally(fromSelf ? UIHorizontalAlignment.Right : UIHorizontalAlignment.Left);
    }

    private void OnConnectionChanged(object? sender, bool connected)
    {
        if (connected)
        {
            _localLabel = $"{Strings.ClientSenderName} {_client.LocalEndPoint?.Port}";
            _connectButton.Disable();
            _disconnectButton.Enable();
            _hostInput.Disable();
            _portInput.Disable();
        }
        else
        {
            _connectButton.Enable();
            _disconnectButton.Disable();
            _hostInput.Enable();
            _portInput.Enable();
        }
    }

    private async ValueTask OnConnectAsync()
    {
        _errorBar.Close();
        _connectButton.Disable();
        _connectingRing.Show();
        try
        {
            await _client.ConnectAsync(_host, _port);
        }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Client", ex.Message));
            RefreshTimeline();
            _errorBar.Title(Strings.ConnectErrorTitle).Description(ex.Message).Open();
            _connectButton.Enable();
        }
        finally
        {
            _connectingRing.Hide();
        }
    }

    private async ValueTask OnDisconnectAsync()
    {
        try { await _client.DisconnectAsync(); }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Client", ex.Message));
            RefreshTimeline();
        }
    }

    private async ValueTask OnSendAsync()
    {
        try
        {
            byte[] payload = _sendAsText
                ? HexPayloadConverter.FromUtf8Text(_sendText)
                : HexPayloadConverter.Parse(_sendText);
            await _client.SendAsync(payload);
        }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Client", ex.Message));
            RefreshTimeline();
        }
    }
}
