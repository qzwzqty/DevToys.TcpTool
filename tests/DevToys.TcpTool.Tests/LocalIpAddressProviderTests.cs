using DevToys.TcpTool.Networking;

namespace DevToys.TcpTool.Tests;

public sealed class LocalIpAddressProviderTests
{
    [Fact]
    public void GetSelectableAddresses_includes_any_and_loopback()
    {
        IReadOnlyList<string> addresses = LocalIpAddressProvider.GetSelectableAddresses();

        Assert.Contains("0.0.0.0", addresses);
        Assert.Contains("127.0.0.1", addresses);
    }

    [Fact]
    public void GetSelectableAddresses_returns_unique_addresses()
    {
        IReadOnlyList<string> addresses = LocalIpAddressProvider.GetSelectableAddresses();

        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }
}
