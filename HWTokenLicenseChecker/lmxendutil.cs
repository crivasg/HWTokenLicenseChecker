using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using System.Diagnostics;

namespace HWTokenLicenseChecker
{
    class lmxendutil
    {
        
        private const String ALTAIR_HOME_ENV_VAR = @"ALTAIR_HOME";
        private const String LMX_LICENSE_PATH_ENV_VAR = @"LMX_LICENSE_PATH";
        private const String LMX_END_USER_UTIL_NAME = @"lmxendutil";

	    private String lmxendutilPath = @"";
        private String folder = @"";

        private String lmx_port = @"";
        private String lmx_server = @"";

        private String[] output = null;

        private List<String> lstFilesFound = null;

	    public lmxendutil ()
	    {

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

        public void ExecuteLMX()
        {
            this.GetLmxEndUtilPath();
            this.GetData();
            this.FixXMLFile();       
        }

	    private void GetData()
	    {

            String args = String.Format(@"-licstatxml -port {0} -host {1} ", lmx_port, lmx_server);

            // http://www.dotnetperls.com/redirectstandardoutput
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
                //
                // Read in all the text from the process with the StreamReader.
                //
                using (StreamReader reader = process.StandardOutput)
                {
                    result = reader.ReadToEnd();
                    
                }
            }

            output = result.Split('\n');
        }
	    

	    private void GetLmxEndUtilPath()
	    {

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
                if (fileFound.Contains(LMX_END_USER_UTIL_NAME))
                {
                    lmxendutilPath = fileFound;
                }
            }

            if (String.IsNullOrEmpty(lmxendutilPath))
            {
                MessageBox.Show(@"LMX End user utility not found!");
                throw new System.ArgumentNullException(lmxendutilPath, @"LMX End user utility not found!");
            }

	    }

        private void FixXMLFile()
        {

            String xmlFile = Path.Combine(folder, @"Licenses.xml");
            StringBuilder sb = new StringBuilder();

            foreach (String line in output)
            {
                if (line.StartsWith(@">") || line.StartsWith(@"<"))
                {
                    sb.AppendLine(line);
                }            
            }

            using (StreamWriter outfile = new StreamWriter(xmlFile))
            {
                outfile.Write(sb.ToString());
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
    }
}
