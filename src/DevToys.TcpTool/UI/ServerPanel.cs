using DevToys.Api;
using DevToys.TcpTool.Core;
using DevToys.TcpTool.Networking;
using static DevToys.Api.GUI;

namespace DevToys.TcpTool.UI;

internal sealed class ServerPanel
{
    private readonly TcpServerHost _server = new();
    private readonly List<LogEntry> _logs = [];
    private readonly HashSet<string> _selectedClientEndpoints = new(StringComparer.Ordinal);

    private string _ip = LocalIpAddressProvider.GetSelectableAddresses().FirstOrDefault() ?? "0.0.0.0";
    private int _port;
    private string _sendText = "";
    private bool _sendAsText;
    private bool _showLog = true;
    private string? _filterEndpoint;

    private readonly IUIButton _startButton = Button("start");
    private readonly IUIButton _stopButton = Button("stop");
    private readonly IUISelectDropDownList _ipDropDown = SelectDropDownList("serverIp");
    private readonly IUINumberInput _portInput = NumberInput("serverPort");
    private readonly IUIInfoBar _errorBar = InfoBar("serverError");
    private readonly IUIProgressRing _startingRing = ProgressRing("startingRing");
    private readonly IUIList _clientsList = List("clients");
    private readonly IUIList _timelineList = List("serverTimeline");
    private readonly IUISelectDropDownList _clientFilter = SelectDropDownList("clientFilter");
    private readonly IUIGrid _root = Grid("serverGrid");

    private enum Row { Main }
    private enum TimelineRow { Scroll }
    private enum Column { Main }
    private enum SplitColumn { Left, Right }
    private enum SplitRow { Content }

    public ServerPanel()
    {
        _server.LogReceived += OnLog;
        _server.ClientConnected += OnClientsChanged;
        _server.ClientDisconnected += OnClientsChanged;
        Build();
        RefreshFilterList();
    }

    public IUIGrid RootElement => _root;

    private void Build()
    {
        _startButton.Text(Strings.StartButton).AccentAppearance().OnClick(OnStartAsync);
        _stopButton.Text(Strings.StopButton).OnClick(OnStopAsync).Disable();

        _root
            .RowSmallSpacing()
            .Rows((Row.Main, Auto))
            .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
            .Cells(
                Cell(Row.Main, Column.Main,
                    Stack().Vertical().SmallSpacing().WithChildren(
                        _errorBar.Error().Closable().Close(),
                        Stack().Horizontal().SmallSpacing().WithChildren(
                            _ipDropDown.Title(Strings.IpTitle)
                                .WithItems(LocalIpAddressProvider.GetSelectableAddresses().Select(x => Item(x, x)).ToArray())
                                .Select(0)
                                .OnItemSelected(v => { _ip = v?.Value?.ToString() ?? _ip; return ValueTask.CompletedTask; }),
                            _portInput.HideCommandBar().Title(Strings.PortTitle).Minimum(0).Maximum(65535).Step(1).Value(_port)
                                .OnValueChanged(v => { _port = (int)v; return ValueTask.CompletedTask; }),
                            _startButton,
                            _stopButton,
                            _startingRing.Hide()),
                        Stack().Horizontal().SmallSpacing().WithChildren(
                            Label().Style(UILabelStyle.BodyStrong).Text(Strings.MessagesTitle),
                            _clientFilter
                                .Title(Strings.FilterTitle)
                                .OnItemSelected(v => { _filterEndpoint = v?.Value?.ToString(); RefreshTimeline(); return ValueTask.CompletedTask; }),
                            Switch("showServerLog")
                                .OnText(Strings.ShowLogToggle)
                                .OffText(Strings.ShowLogToggle)
                                .On()
                                .OnToggle(isOn => { _showLog = isOn; RefreshTimeline(); return ValueTask.CompletedTask; })),
                        Grid("serverTimelineGrid")
                            .Rows((TimelineRow.Scroll, new UIGridLength(320, UIGridUnitType.Pixel)))
                            .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                            .Cells(Cell(TimelineRow.Scroll, Column.Main, _timelineList.ForbidSelectItem())),
                        Grid("serverSplitGrid")
                            .ColumnSmallSpacing()
                            .Columns(
                                (SplitColumn.Left, new UIGridLength(3, UIGridUnitType.Fraction)),
                                (SplitColumn.Right, new UIGridLength(7, UIGridUnitType.Fraction)))
                            .Rows((SplitRow.Content, Auto))
                            .Cells(
                                Cell(SplitRow.Content, SplitColumn.Left,
                                    Stack().Vertical().SmallSpacing().WithChildren(
                                        Label().Style(UILabelStyle.BodyStrong).Text(Strings.ConnectedClientsTitle),
                                        Grid("serverClientsGrid")
                                            .Rows((TimelineRow.Scroll, new UIGridLength(180, UIGridUnitType.Pixel)))
                                            .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                                            .Cells(Cell(TimelineRow.Scroll, Column.Main, _clientsList.ForbidSelectItem())))),
                                Cell(SplitRow.Content, SplitColumn.Right,
                                    Stack().Vertical().SmallSpacing().WithChildren(
                                        MultiLineTextInput("serverSend")
                                            .Title(Strings.TextToSendTitle)
                                            .Text(_sendText)
                                            .Extendable()
                                            .CommandBarExtraContent(
                                                SelectDropDownList("serverFormat")
                                                    .WithItems(
                                                        Item(Strings.FormatHex, false),
                                                        Item(Strings.FormatText, true))
                                                    .Select(0)
                                                    .OnItemSelected(v => { _sendAsText = v?.Value is true; return ValueTask.CompletedTask; }))
                                            .OnTextChanged(v => { _sendText = v; return ValueTask.CompletedTask; }),
                                        Button("serverSendBtn").Text(Strings.SendButton).AccentAppearance().OnClick(OnSendAsync)))))
                        ));
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
        if (entry.Payload is not null && _filterEndpoint is not null && !SourceMatchesFilter(entry.Source))
        {
            return null;
        }

        if (entry is { Kind: LogEntryKind.Sent, Payload: not null })
        {
            string clientLabel = entry.Source.StartsWith("Server->", StringComparison.Ordinal)
                ? entry.Source["Server->".Length..]
                : entry.Source;
            return ChatBubble($"Server -> {clientLabel}", entry.Timestamp, HexPayloadConverter.ToHex(entry.Payload), fromSelf: true);
        }

        if (entry is { Kind: LogEntryKind.Received, Payload: not null })
        {
            string clientLabel = entry.Source.StartsWith("Server<-", StringComparison.Ordinal)
                ? entry.Source["Server<-".Length..]
                : entry.Source;
            return ChatBubble($"{clientLabel} -> Server", entry.Timestamp, HexPayloadConverter.ToHex(entry.Payload), fromSelf: false);
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

    private bool SourceMatchesFilter(string source)
    {
        string? flowEndpoint = source.StartsWith("Server->", StringComparison.Ordinal) ? source["Server->".Length..]
            : source.StartsWith("Server<-", StringComparison.Ordinal) ? source["Server<-".Length..]
            : null;
        return string.Equals(flowEndpoint ?? source, _filterEndpoint, StringComparison.Ordinal);
    }

    private static IUIElement ChatBubble(string sender, DateTimeOffset timestamp, string content, bool fromSelf)
    {
        IUIStack bubble = Stack().Vertical().NoSpacing().WithChildren(
            Label().Style(UILabelStyle.Caption).Text($"{sender}  {timestamp:HH:mm:ss}"),
            Label().Style(UILabelStyle.Body).Text(content));

        return Card(bubble)
            .AlignHorizontally(fromSelf ? UIHorizontalAlignment.Right : UIHorizontalAlignment.Left);
    }

    private void OnClientsChanged(object? sender, TcpServerClient client)
    {
        RefreshClientList();
        RefreshFilterList();
    }

    private void RefreshClientList()
    {
        HashSet<string> live = _server.Clients.Select(x => x.RemoteEndPoint.ToString()).ToHashSet(StringComparer.Ordinal);
        _selectedClientEndpoints.RemoveWhere(x => !live.Contains(x));

        IUIListItem[] items = _server.Clients
            .Select(client =>
            {
                string endpoint = client.RemoteEndPoint.ToString();
                IUIElement row = Stack().Horizontal().SmallSpacing().WithChildren(
                    Switch($"client-{endpoint}")
                        .OnText(endpoint)
                        .OffText(endpoint)
                        .OnToggle(isOn =>
                        {
                            if (isOn) _selectedClientEndpoints.Add(endpoint);
                            else _selectedClientEndpoints.Remove(endpoint);
                            return ValueTask.CompletedTask;
                        }));
                return Item(row, endpoint);
            })
            .ToArray();

        _clientsList.WithItems(items);
    }

    private void RefreshFilterList()
    {
        IUIDropDownListItem[] items = _server.Clients
            .Select(c => c.RemoteEndPoint.ToString())
            .Prepend(null)
            .Select(ep => Item(ep is null ? Strings.AllClients : ep, ep))
            .ToArray();

        _clientFilter.WithItems(items);
        if (_filterEndpoint is null || !_server.Clients.Any(c => c.RemoteEndPoint.ToString() == _filterEndpoint))
        {
            _filterEndpoint = null;
            _clientFilter.Select(0);
        }
    }

    private ValueTask OnStartAsync()
    {
        _errorBar.Close();
        _startingRing.Show();
        try
        {
            _server.StartAsync(_ip, _port);
            _startButton.Disable();
            _stopButton.Enable();
            _ipDropDown.Disable();
            _portInput.Disable();
        }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Server", ex.Message));
            RefreshTimeline();
            _errorBar.Title(Strings.StartErrorTitle).Description(ex.Message).Open();
        }
        finally
        {
            _startingRing.Hide();
        }
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnStopAsync()
    {
        try
        {
            await _server.StopAsync();
            _selectedClientEndpoints.Clear();
            RefreshClientList();
            _startButton.Enable();
            _stopButton.Disable();
            _ipDropDown.Enable();
            _portInput.Enable();
        }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Server", ex.Message));
            RefreshTimeline();
        }
    }

    private async ValueTask OnSendAsync()
    {
        _errorBar.Close();

        TcpServerClient[] selectedClients = _server.Clients
            .Where(c => _selectedClientEndpoints.Contains(c.RemoteEndPoint.ToString()))
            .ToArray();

        if (selectedClients.Length == 0)
        {
            _errorBar.Description(Strings.NoClientSelected).Open();
            return;
        }

        try
        {
            byte[] payload = _sendAsText
                ? HexPayloadConverter.FromUtf8Text(_sendText)
                : HexPayloadConverter.Parse(_sendText);

            foreach (TcpServerClient client in selectedClients)
            {
                await client.SendAsync(payload);
            }
        }
        catch (Exception ex)
        {
            _logs.Add(LogEntry.Error("Server", ex.Message));
            RefreshTimeline();
            _errorBar.Description(ex.Message).Open();
        }
    }
}
