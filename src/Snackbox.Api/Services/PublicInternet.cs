using System.Net;
using System.Net.Sockets;

namespace Snackbox.Api.Services;

/// <summary>
/// Connections for fetching pictures from addresses an admin pasted or a search returned: only
/// to public internet addresses. Checked when the socket connects, so neither a redirect nor a
/// DNS name pointing at 127.0.0.1 or the office network gets the API to fetch from inside.
/// </summary>
public static class PublicInternet
{
    public const string HttpClientName = "PublicInternet";

    public static SocketsHttpHandler CreateHandler() => new()
    {
        ConnectCallback = ConnectAsync,
        // With a proxy the callback would vet the proxy, not the target
        UseProxy = false,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    };

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var address = addresses.FirstOrDefault(IsPublic)
                      ?? throw new HttpRequestException($"{context.DnsEndPoint.Host} is not a public internet address");

        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return false;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 10                                   // private
                     || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)  // private
                     || (b[0] == 192 && b[1] == 168)               // private
                     || (b[0] == 169 && b[1] == 254)               // link-local
                     || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) // carrier-grade NAT
                     || (b[0] == 192 && b[1] == 0 && b[2] is 0 or 2)  // IETF protocol assignments, documentation
                     || (b[0] == 198 && b[1] is 18 or 19)          // benchmarking
                     || (b[0] == 198 && b[1] == 51 && b[2] == 100) // documentation
                     || (b[0] == 203 && b[1] == 0 && b[2] == 113)  // documentation
                     || b[0] == 0 || b[0] >= 224);                 // "this network", multicast, reserved
        }

        // IPv6: an allowlist, not a denylist - only global unicast (2000::/3). That leaves out
        // loopback, link-local, unique local, multicast, IPv4-mapped/-compatible and NAT64
        // (64:ff9b::/96), which can all lead back into the local network. Within 2000::/3,
        // 6to4 (2002::/16) and Teredo (2001::/32) carry an IPv4 address too, 2001:db8::/32 is
        // documentation.
        var v6 = address.GetAddressBytes();
        if ((v6[0] & 0xE0) != 0x20) return false;
        if (v6[0] == 0x20 && v6[1] == 0x02) return false;
        if (v6[0] == 0x20 && v6[1] == 0x01 && ((v6[2] == 0x00 && v6[3] == 0x00) || (v6[2] == 0x0D && v6[3] == 0xB8))) return false;
        return true;
    }
}
