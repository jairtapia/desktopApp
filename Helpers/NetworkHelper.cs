using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DesktopAssistant.Helpers;

/// <summary>
/// Helper to retrieve local network information.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Gets the primary local IP address (IPv4, non-loopback).
    /// </summary>
    public static string GetLocalIpAddress()
    {
        try
        {
            // Prefer WiFi or Ethernet interfaces that are up
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                    && (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                        || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

            foreach (var ni in networkInterfaces)
            {
                var ipProps = ni.GetIPProperties();
                var ipv4Addr = ipProps.UnicastAddresses
                    .FirstOrDefault(addr =>
                        addr.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(addr.Address));

                if (ipv4Addr != null)
                {
                    return ipv4Addr.Address.ToString();
                }
            }

            // Fallback: use DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var fallback = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            return fallback?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// Gets the machine hostname.
    /// </summary>
    public static string GetHostName() => Environment.MachineName;

    /// <summary>
    /// Checks if a port is available for use.
    /// </summary>
    public static bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
