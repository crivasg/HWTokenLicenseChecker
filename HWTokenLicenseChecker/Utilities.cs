﻿using System;
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

        public static String GetIPFromHostname(String hostname)
        {

            String ip = String.Empty;

            IPAddress[] addresslist = Dns.GetHostAddresses(hostname);
            foreach (IPAddress theaddress in addresslist)
            {
                ip = theaddress.ToString();
            }

            return ip;
        }

        public static bool SaveFile(String format, String filter, String title, String filename)
        {
            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                DefaultExt = format,
                Title = title,
                Filter = filter,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            String destination = String.Empty;
            bool status = true;

            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                destination = saveDlg.FileName;
                try
                {
                    File.Copy(filename, destination);
                }
                catch (Exception ex)
                {
                    status = false;
                }
            }

            return status;
        }


        public static bool ExportDataToCSV(String filter, String title, String filename, DataGridView dgv)
        {
            bool status = true;
            String csvString = String.Empty;
            String separator = @",";

            SaveFileDialog saveDlg = new SaveFileDialog()
            {
                Title = title,
                Filter = filter,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveDlg.ShowDialog() == DialogResult.OK)
            {

                if (saveDlg.FilterIndex == 2)
                {
                    separator = "\t";
                }

                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    csvString += column.HeaderText + separator;
                }
                csvString += Environment.NewLine;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        csvString += cell.Value.ToString() + separator; 
                    }
                    csvString += Environment.NewLine;
                }

                IEnumerable<String> csvData = csvString.Split('\n').ToList().Where(x => x.Trim().Length>0);

                try
                {
                    using (StreamWriter streamWriter = new StreamWriter(saveDlg.FileName))
                    {
                        foreach (String line in csvData)
                        {
                            int index = line.LastIndexOfAny(new Char[] { ',', '\t' });
                            streamWriter.Write(line.Substring(0,index) + Environment.NewLine); 
                        }
                    }
                }
                catch
                {
                    status = false;
                }

            }

            return status;
        }

    }
}
