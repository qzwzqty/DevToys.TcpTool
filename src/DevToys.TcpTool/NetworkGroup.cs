using System.ComponentModel.Composition;
using DevToys.Api;

namespace DevToys.TcpTool;

[Export(typeof(GuiToolGroup))]
[Name("Network")]
[Order(After = PredefinedCommonToolGroupNames.EncodersDecoders)]
internal sealed class NetworkGroup : GuiToolGroup
{
    [ImportingConstructor]
    internal NetworkGroup()
    {
        IconFontName = "FluentSystemIcons";
        IconGlyph = '\uE670';
        DisplayTitle = Strings.GroupDisplayTitle;
        AccessibleName = Strings.GroupAccessibleName;
    }
}
