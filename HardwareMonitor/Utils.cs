using System.Net.NetworkInformation;

namespace HardwareMonitor
{
    public class Utils
    {
        public static double GetTimestamp()
        {
            return DateTime.UtcNow
               .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
               .TotalMilliseconds;
        }

        public static string GetLocalIPv4(NetworkInterfaceType type)
        {

            string addr = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == type && item.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties props = item.GetIPProperties();
                    if (props.GatewayAddresses.FirstOrDefault() != null)
                    {
                        foreach (UnicastIPAddressInformation ip in props.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                addr = ip.Address.ToString();
                                break;
                            }
                        }
                    }
                }
                if (addr != "") { break; }
            }
            return addr;
        }

        public static long GetUTCTimestamp()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
    }
}
