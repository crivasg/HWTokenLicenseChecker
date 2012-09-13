using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.Linq;


namespace HWTokenLicenseChecker
{
    enum Status
    { 
        OK = 0,
        ServerOffline,
        LicenseServerOffline,
        LmxExecuteError,
        EndUserUtilityNotFound,
        ConfigToolNotFound,
        LmxToolsNotFound,
        FailedToFixXMLFile
    };

    class lmxendutil
    {

        public Status AppStatus { get ; private set; }

        private const String ALTAIR_HOME_ENV_VAR = @"ALTAIR_HOME";
        private const String LMX_LICENSE_PATH_ENV_VAR = @"LMX_LICENSE_PATH";
        private const String LMX_END_USER_UTIL_NAME = @"lmxendutil.exe";
        private const String LMX_CONFIG_TOOL_NAME = @"lmxconfigtool.exe";

	    private String lmxendutilPath = @"";
        private String lmxconfigtoolPath = @"";
        private String folder = @"";

        private String lmx_port = @"";
        private String lmx_server = @"";

        private String[] output = null;

        private List<String> lstFilesFound = null;

	    public lmxendutil ()
	    {
            this.AppStatus = Status.OK;
	    }

        public String LmxPath
        {
            get { return this.lmxendutilPath; }
        }

        public String AppDataFolder
        {
            set { folder = value; }
            get { return this.folder; }
        }

        public String LMXConfigTool
        {
            get { return lmxconfigtoolPath;  }
        }

        public void ExecuteLMX()
        {
            this.GetLmxEndUtilPath();
            this.PingLMXServer();
            this.GetData();
            this.FixXMLFile();
            this.CheckIfLMXServerIsRunning();
            
        }

	    private void GetData()
	    {
            if (this.AppStatus != Status.OK)
            {
                return;
            }


            String args = String.Format(@"-licstatxml -port {0} -host {1} ", lmx_port, lmx_server);

            // http://www.dotnetperls.com/redirectstandardoutput

            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = lmxendutilPath;
                start.Arguments = args;
                String result = @"";

                start.RedirectStandardOutput = true;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.WindowStyle = ProcessWindowStyle.Hidden;


                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();

                    }
                }
                output = result.Split('\n');
                
            }
            catch (Exception ex)
            {
                //MessageBox.Show(@"Error while executing the LMX End User Utility (lmxendutil.exe)." +
                //    Environment.NewLine + @"The Application will quit" + Environment.NewLine + ex.ToString());
                //output = null;

                this.AppStatus = Status.LmxExecuteError;

                //throw new System.ArgumentNullException(lmxendutilPath, @"Error while executing the LMX End User Utility ");
            }
            finally
            {

            }
        }
	    
	    private void GetLmxEndUtilPath()
	    {
            if (this.AppStatus != Status.OK)
            {
                return;
            }

            // Get the server info using the LMX_LICENSE_PATH enviroment variable
            EnvVariable lmxEnvVar = new EnvVariable() { Name = LMX_LICENSE_PATH_ENV_VAR, Type = EnvVarType.HostPortAndIp };
            lmxEnvVar.GetEnviromentVariableData();
            String server_info = lmxEnvVar.Value;
            String[] server_info_array = server_info.Split(new Char[] { '@' });
            lmx_port = server_info_array[0];
            lmx_server = server_info_array[1];
            //MessageBox.Show(server_info);


            // Get the ALTAIR_HOME folder.
            EnvVariable altairEnvVar = new EnvVariable() { Name = ALTAIR_HOME_ENV_VAR, Type = EnvVarType.FolderPath };
            altairEnvVar.GetEnviromentVariableData();
            String altair_Home = altairEnvVar.Value;
            //MessageBox.Show(altair_Home);

            String securityPath = Path.Combine(altair_Home, @"security");
            lstFilesFound = new List<String>();
            DirSearch(securityPath, @"*.exe");

            foreach (String fileFound in lstFilesFound)
            {
                String fname = Path.GetFileName(fileFound);

                if (fname.Equals(LMX_END_USER_UTIL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    lmxendutilPath = fileFound;
                }
                if (fname.Equals(LMX_CONFIG_TOOL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    lmxconfigtoolPath = fileFound;
                }
            }

            // checks...
            if (String.IsNullOrEmpty(lmxendutilPath) &&
                 String.IsNullOrEmpty(lmxconfigtoolPath))
            {
                this.AppStatus = Status.LmxToolsNotFound;
            }
            else if (String.IsNullOrEmpty(lmxendutilPath))
            {
                this.AppStatus = Status.EndUserUtilityNotFound;
            }
            else if (String.IsNullOrEmpty(lmxconfigtoolPath))
            {
                this.AppStatus = Status.ConfigToolNotFound;
            }


            /*
            if (String.IsNullOrEmpty(lmxendutilPath))
            {
                MessageBox.Show(@"LMX End user utility not found!");
                throw new System.ArgumentNullException(lmxendutilPath, @"LMX End user utility not found!");
            }

            if (String.IsNullOrEmpty(lmxconfigtoolPath))
            {
                MessageBox.Show(@"LMX Config Tool not found!");
                throw new System.ArgumentNullException(lmxendutilPath, @"LMX End user utility not found!");
            }
             * 
             */
	    }

        private void FixXMLFile()
        {
            if (this.AppStatus != Status.OK)
            {
                return;
            }

            try 
            {
                String xmlFile = Path.Combine(folder, @"Licenses.xml");
                StringBuilder sb = new StringBuilder();

                foreach (String line in output)
                {
                    if (line.Trim().Length == 0)
                    {
                        continue;
                    }

                    if (line.StartsWith(@">") || line.StartsWith(@"<"))
                    {
                        sb.AppendLine(line.Trim());
                    }
                }

                using (StreamWriter outfile = new StreamWriter(xmlFile))
                {
                    outfile.Write(sb.ToString());
                    outfile.Close();
                }
            }
            catch (Exception ex)
            {
                this.AppStatus = Status.FailedToFixXMLFile;
            }
            finally 
            { 
            }


        }

        private void DirSearch(String sDir, String fileExtension)
        {
            // http://support.microsoft.com/kb/303974
            try
            {
                foreach (String d in Directory.GetDirectories(sDir))
                {
                    foreach (String f in Directory.GetFiles(d, fileExtension))
                    {
                        lstFilesFound.Add(f);
                    }
                    DirSearch(d, fileExtension);
                }
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.Message);
                //Console.WriteLine(excpt.Message);
            }

        }

        private void CheckIfLMXServerIsRunning()
        {
            if (this.AppStatus != Status.OK)
            {
                return;
            }

            XDocument xdoc = XDocument.Load(Path.Combine(folder, @"Licenses.xml"));
            String result = String.Empty;
            //Run query
            var lv1s = from lv1 in xdoc.Descendants("LM-X")
                       select new
                       {
                           ServerVersion = lv1.Element("LICENSE_PATH").Attribute("SERVER_VERSION").Value,
                       };

            //Loop through results
            foreach (var lv1 in lv1s)
            {
                result += lv1.ServerVersion.ToString();
            }

            double sVersion = -9999.9;
            bool parseResult = double.TryParse(result, out sVersion);

            if (!parseResult)
            {
                this.AppStatus = Status.LicenseServerOffline;
            }

            // if results is empty, the license server may not be running.
            //MessageBox.Show(result + Environment.NewLine + parseResult.ToString() + " " + this.AppStatus.ToString());
        }

        private void PingLMXServer()
        {

            if (this.AppStatus != Status.OK)
            {
                return;
            }

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

            PingReply reply = pingSender.Send(lmx_server, timeout, buffer, options);
            String response = String.Empty;

            if( reply.Status != IPStatus.Success)
            {
                this.AppStatus = Status.ServerOffline;
            }

            //if (reply.Status == IPStatus.Success)
            //{
            //    response += String.Format("Address: {0}\n", reply.Address.ToString());
            //    response += String.Format("RoundTrip time: {0}\n", reply.RoundtripTime);
            //    response += String.Format("Time to live: {0}\n", reply.Options.Ttl);
            //    response += String.Format("Don't fragment: {0}\n", reply.Options.DontFragment);
            //    response += String.Format("Buffer size: {0}\n", reply.Buffer.Length);
            //    response += String.Format("Status: {0}\n", reply.Status.ToString());
            //    MessageBox.Show(response);
            //}
            //else 
            //{
            //    MessageBox.Show(reply.Status.ToString());
            //}
          
        }
    }
}
