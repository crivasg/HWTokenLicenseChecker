using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using System.Net;
using System.Net.NetworkInformation;

namespace HWTokenLicenseChecker
{
    public static class Utilities
    {
        // Gets the %APPDATA% folder ...
        public readonly static String ApplicationDataDir = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA"),
            Path.GetFileNameWithoutExtension(Application.ExecutablePath));

        // Gets the TMP folder ...
        public readonly static String TempDir = Environment.GetEnvironmentVariable("TMP");

        public static String GetVariableValue(String strEnvironmentVariable )
        {

            if (String.IsNullOrEmpty(strEnvironmentVariable))
            {
                return String.Empty;
            }

            String valueStr = String.Empty;

            valueStr = Environment.GetEnvironmentVariable(strEnvironmentVariable, EnvironmentVariableTarget.User);
            if (String.IsNullOrEmpty(valueStr))
            {
                valueStr = Environment.GetEnvironmentVariable(strEnvironmentVariable, EnvironmentVariableTarget.Machine);
            }

            return valueStr;
        }

        public static IPStatus PingServer(String ipAddressString)
        {
            //http://msdn.microsoft.com/en-us/library/system.net.networkinformation.ping(v=vs.90).aspx 

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128, 
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            String data = new String('a', 32);
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            PingReply reply = pingSender.Send(ipAddressString, timeout, buffer, options);
        
            return reply.Status;
        }
    }
}
