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
        public String XMLFile { private get; set; }
        public String LmxPath { get; set; }
        public String LMXConfigTool { get; private set; }
        
        private const String ALTAIR_HOME_ENV_VAR = @"ALTAIR_HOME";
        private const String LMX_LICENSE_PATH_ENV_VAR = @"LMX_LICENSE_PATH";
        private const String LMX_END_USER_UTIL_NAME = @"lmxendutil.exe";
        private const String LMX_CONFIG_TOOL_NAME = @"lmxconfigtool.exe";

        private String lmx_port = String.Empty;
        private String lmx_server = String.Empty;

        private String[] output = null;
        private List<String> lstFilesFound = new List<String>();

        private const int MAX_NUM_OF_PING_ITERS = 3;

	    public lmxendutil ()
	    {
            this.AppStatus = Status.OK;
	    }

        public void ExecuteLMX()
        {
            this.GetLmxEndUtilPath();
            this.PingLMXServer();
            this.GetData();
            this.FixXMLFile();
            this.CheckIfLMXServerIsRunning();
            
        }

        public String LMXStatusMessage()
        {
            String msg = String.Empty;

            switch (this.AppStatus)
            {
                case Status.ServerOffline:
                    msg = @"Server may be offline.";
                    break;
                case Status.LicenseServerOffline:
                    msg = @"LMX license server not running on server.";
                    break;
                case Status.LmxExecuteError:
                    msg = @"Error while executing the LMX end user utilities.";
                    break;
                case Status.EndUserUtilityNotFound:
                    msg = @"LMX end user utility not found.";
                    break;
                case Status.ConfigToolNotFound:
                    msg = @"LMX configuration tool not found.";
                    break;
                case Status.LmxToolsNotFound:
                    msg = @"LMX tools not found.";
                    break;
                case Status.FailedToFixXMLFile:
                    msg = @"Failed to fix the XML file.";
                    break;
            }
            msg += @" Contact your network administrator.";

            return msg;      
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
                start.FileName = this.LmxPath;
                start.Arguments = args;
                String result = String.Empty;

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
            catch
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
            
            lstFilesFound = Directory.GetFiles(securityPath, @"*.exe", SearchOption.AllDirectories).ToList();
           

            foreach (String fileFound in lstFilesFound)
            {
                String fname = Path.GetFileName(fileFound);

                if (fname.Equals(LMX_END_USER_UTIL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    this.LmxPath = fileFound;
                }
                if (fname.Equals(LMX_CONFIG_TOOL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    this.LMXConfigTool = fileFound;
                }
            }

            // checks...
            if (String.IsNullOrEmpty(this.LmxPath) &&
                 String.IsNullOrEmpty(this.LMXConfigTool))
            {
                this.AppStatus = Status.LmxToolsNotFound;
            }
            else if (String.IsNullOrEmpty(this.LmxPath))
            {
                this.AppStatus = Status.EndUserUtilityNotFound;
            }
            else if (String.IsNullOrEmpty(this.LMXConfigTool))
            {
                this.AppStatus = Status.ConfigToolNotFound;
            }

	    }

        private void FixXMLFile()
        {
            if (this.AppStatus != Status.OK)
            {
                return;
            }

            try 
            {
                //String xmlFile = Path.Combine(folder, @"Licenses.xml");
                StringBuilder sb = new StringBuilder();
                foreach (String line in output.Where(line => line.StartsWith(">") || line.StartsWith("<")))
                {
                    sb.AppendLine(line.Trim());
                }

                using (StreamWriter outfile = new StreamWriter(this.XMLFile))
                {
                    outfile.Write(sb.ToString());
                    outfile.Close();
                }
            }
            catch
            {
                this.AppStatus = Status.FailedToFixXMLFile;
            }
            finally 
            { 
            }


        }

        private void CheckIfLMXServerIsRunning()
        {
            if (this.AppStatus != Status.OK)
            {
                return;
            }

            XDocument xdoc = XDocument.Load(this.XMLFile);
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

            IPStatus pingResponse = Utilities.PingServer(lmx_server);
            if (pingResponse != IPStatus.Success)
            {
                this.AppStatus = Status.ServerOffline;
            }
          
        }
    }
}
