using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HardwareMonitor
{
    class Utils
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
    }
}
