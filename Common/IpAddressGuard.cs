using System.Net;
using System.Net.Sockets;

namespace EventsApp.Common
{
    /// <summary>
    /// Shared SSRF guard used by the link-preview HttpClient and any caller
    /// that needs to refuse outbound connections to private / loopback /
    /// link-local addresses.
    /// </summary>
    public static class IpAddressGuard
    {
        public static async Task<bool> IsPrivateOrLoopbackHostAsync(string host, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host)) return true;

            if (IPAddress.TryParse(host, out var direct))
            {
                return IsPrivateOrLoopback(direct);
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
                return addresses.Length == 0 || addresses.Any(IsPrivateOrLoopback);
            }
            catch
            {
                return true;
            }
        }

        public static bool IsPrivateOrLoopback(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return bytes[0] == 10
                       || bytes[0] == 127
                       || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                       || (bytes[0] == 192 && bytes[1] == 168)
                       || (bytes[0] == 169 && bytes[1] == 254)
                       // Carrier-grade NAT and other reserved ranges block list.
                       || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                       || bytes[0] == 0;
            }

            return address.AddressFamily == AddressFamily.InterNetworkV6 &&
                   (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
                    || IPAddress.IPv6Loopback.Equals(address)
                    || IPAddress.IPv6Any.Equals(address));
        }
    }
}
