using System.Net;
using System.Net.Sockets;

namespace DevToys.TcpTool.Networking;

public static class LocalIpAddressProvider
{
    public static IReadOnlyList<string> GetSelectableAddresses()
    {
        HashSet<string> addresses = new(StringComparer.Ordinal)
        {
            IPAddress.Any.ToString(),
            IPAddress.Loopback.ToString()
        };

        foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                addresses.Add(address.ToString());
            }
        }

        return addresses.OrderBy(x => x == IPAddress.Any.ToString() ? 0 : x == IPAddress.Loopback.ToString() ? 1 : 2)
            .ThenBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }
}
