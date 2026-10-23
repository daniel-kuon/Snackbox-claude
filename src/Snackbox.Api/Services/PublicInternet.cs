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
                     || b[0] == 0 || b[0] >= 224);                 // "this network", multicast, reserved
        }

        // IPv6: link-local, site-local, unique local (fc00::/7) and multicast are not public
        return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
                 || (address.GetAddressBytes()[0] & 0xFE) == 0xFC);
    }
}
