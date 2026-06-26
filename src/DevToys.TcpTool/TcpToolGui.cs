using System.ComponentModel.Composition;
using DevToys.Api;
using DevToys.TcpTool.UI;
using static DevToys.Api.GUI;

namespace DevToys.TcpTool;

[Export(typeof(IGuiTool))]
[Name("TCP Tool")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE670',
    GroupName = "Network",
    ResourceManagerAssemblyIdentifier = nameof(ResourceAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.TcpTool.Strings",
    ShortDisplayTitleResourceName = "ShortDisplayTitle",
    LongDisplayTitleResourceName = "LongDisplayTitle",
    DescriptionResourceName = "Description",
    AccessibleNameResourceName = "AccessibleName")]
internal sealed class TcpToolGui : IGuiTool
{
    private readonly ClientPanel _clientPanel = new();
    private readonly ServerPanel _serverPanel = new();

    private readonly IUIButton _tabClientButton = Button("tabClient");
    private readonly IUIButton _tabServerButton = Button("tabServer");

    private enum Row { Tabs, Content }
    private enum Column { Main }

    public TcpToolGui()
    {
        ShowClient();
    }

    public UIToolView View
        => new UIToolView(
            Grid("rootGrid")
                .RowSmallSpacing()
                .Rows(
                    (Row.Tabs, Auto),
                    (Row.Content, new UIGridLength(1, UIGridUnitType.Fraction)))
                .Columns((Column.Main, new UIGridLength(1, UIGridUnitType.Fraction)))
                .Cells(
                    Cell(Row.Tabs, Column.Main,
                        Stack().Horizontal().SmallSpacing().WithChildren(
                            _tabClientButton.Text(Strings.ClientTab).AccentAppearance().OnClick(ShowClient),
                            _tabServerButton.Text(Strings.ServerTab).OnClick(ShowServer))),
                    Cell(Row.Content, Column.Main, _clientPanel.RootElement),
                    Cell(Row.Content, Column.Main, _serverPanel.RootElement)));

    private void ShowClient()
    {
        _clientPanel.RootElement.Show();
        _serverPanel.RootElement.Hide();
        _tabClientButton.AccentAppearance();
        _tabServerButton.NeutralAppearance();
    }

    private void ShowServer()
    {
        _serverPanel.RootElement.Show();
        _clientPanel.RootElement.Hide();
        _tabServerButton.AccentAppearance();
        _tabClientButton.NeutralAppearance();
    }

    public void OnDataReceived(string dataTypeName, object? parsedData) { }
}
