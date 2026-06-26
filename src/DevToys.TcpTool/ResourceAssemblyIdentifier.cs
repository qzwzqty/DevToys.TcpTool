using System.ComponentModel.Composition;
using DevToys.Api;

namespace DevToys.TcpTool;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(ResourceAssemblyIdentifier))]
public sealed class ResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        return ValueTask.FromResult(Array.Empty<FontDefinition>());
    }
}
