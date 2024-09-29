using System.Net.Sockets;
using System.Net;

namespace SImulator.ViewModel.Helpers;

internal static class NetworkHelper
{   
    private static string[]? _ipAddresses = null;

    internal static async Task<string[]> GetIdAddressesAsync()
    {
        if (_ipAddresses != null)
        {
            return _ipAddresses;
        }

        try
        {
            _ipAddresses = (await Dns.GetHostAddressesAsync(Dns.GetHostName()))
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();
        }
        catch
        {
            _ipAddresses = Array.Empty<string>();
        }

        return _ipAddresses;
    }

    internal static string FindLocalNetworkAddress(string[]? ipAddresses)
    {
        if (ipAddresses == null)
        {
            return "";
        }

        foreach (var ipAddress in ipAddresses)
        {
            if (ipAddress.StartsWith("192.168."))
            {
                return ipAddress;
            }
        }

        return ipAddresses.FirstOrDefault() ?? "";
    }
}
