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

            String outputXMLFile = Path.Combine(folder, @"licenses.tmp");
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

            // get the port and ip of the hyperorks license
            /*String server_info = Environment.GetEnvironmentVariable(LMX_LICENSE_PATH_ENV_VAR);
            if (!String.IsNullOrEmpty(server_info))
            {
                String[] server_info_array = server_info.Split(new Char[] { '@' });
                lmx_port = server_info_array[0];
                lmx_server = server_info_array[1];
            }
            else
            {
                //[REDACTED] settings
                lmx_port = @"6200";
                lmx_server = @"192.128.2.36";
            }*/

            EnvVariable lmxEnvVar = new EnvVariable() { Name = LMX_LICENSE_PATH_ENV_VAR, Type = EnvVarType.HostPortAndIp };
            lmxEnvVar.GetEnviromentVariableData();
            String server_info = lmxEnvVar.Value;
            String[] server_info_array = server_info.Split(new Char[] { '@' });
            lmx_port = server_info_array[0];
            lmx_server = server_info_array[1];
            MessageBox.Show(server_info);


            // get the path to 'lmxendutil.exe'
            String altair_Home = Environment.GetEnvironmentVariable(ALTAIR_HOME_ENV_VAR);
            if (String.IsNullOrEmpty(altair_Home))
            {
                altair_Home = Environment.GetEnvironmentVariable(ALTAIR_HOME_ENV_VAR, EnvironmentVariableTarget.User);
            }
            String arch = this.GetArch();
		    String lmxPath = @"security\bin\#####\lmxendutil.exe";

            lmxPath = lmxPath.Replace(@"#####", arch);
		    // check if env variable exists? and get arch by code: win32 or win64?

		    if( String.IsNullOrEmpty(altair_Home) )
		    {
			    FolderBrowserDialog browserDialog = new FolderBrowserDialog();
			    browserDialog.Description = @"Select the Altair Home folder (W:\path\to\Altair\##.#).";
			    browserDialog.ShowNewFolderButton = false;
			    browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

			    if (browserDialog.ShowDialog()  == DialogResult.OK )
			    {
				    altair_Home	= browserDialog.SelectedPath;
                    String question = String.Format(@"Set environment variable '{0}' to '{1}'", ALTAIR_HOME_ENV_VAR, altair_Home);

                    var result = MessageBox.Show(question, 
                        @"Env. Variable " + ALTAIR_HOME_ENV_VAR,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Environment.SetEnvironmentVariable(ALTAIR_HOME_ENV_VAR,
                            altair_Home, 
                            EnvironmentVariableTarget.User); 
                    }

			    }
		    }

		    lmxendutilPath = Path.Combine(altair_Home,lmxPath);
		    

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

        private String GetArch()
        {

            String arch = @"win16";
            if (IntPtr.Size == 8)
            {
                arch = @"win64";
            }
            else if (IntPtr.Size == 4)
            {
                arch = @"win32";
            }

            return arch;
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
