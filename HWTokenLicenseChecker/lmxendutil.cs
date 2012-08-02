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

        private String[] data = null;
	    private String lmxendutilPath = @"";

        private String lmx_port = @"";
        private String lmx_server = @"";

	    public lmxendutil ()
	    {
		    this.GetLmxEndUtilPath();
            this.FixXMLFile();
		    this.GetData();
	    }

	    public String[] Data
	    {
		    get { return this.data; }
	    }

        public String LmxPath
        {
            get { return this.lmxendutilPath; }
        }

	    private void GetData()
	    {

            String outputXMLFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"licenses.tmp");
            if (File.Exists(outputXMLFile))
            {
                //File.Delete(outputXMLFile);
            }

            String args = String.Format(@"-licstatxml -port {0} -host {1} > {2}", lmx_port, lmx_server, outputXMLFile);
            //MessageBox.Show(args);

            /* 
             
             
            ProcessStartInfo psi = new ProcessStartInfo(lmxendutilPath);
            psi.Arguments = @" > " + outputXMLFile;
            psi.RedirectStandardOutput = true;
            //psi.CreateNoWindow = true;
            //psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process listFiles;
            try
            {
                listFiles = Process.Start(psi);
                StreamReader myOutput = listFiles.StandardOutput;
                listFiles.WaitForExit();
                if (listFiles.HasExited)
                {
                    String output = myOutput.ReadToEnd();
                    data = output.Split('\n');
                }
            }
            catch (Exception e)
            {

                MessageBox.Show(String.Format("{0} Exception caught.", e));
            }
            */
        }
	    

	    private void GetLmxEndUtilPath()
	    {
            // get the port and ip of the hyperorks license
            String server_info = Environment.GetEnvironmentVariable(LMX_LICENSE_PATH_ENV_VAR);
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
            }


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
            String outputXMLFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"licenses.tmp");
            String xmlFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"licenses.xml");
            if (File.Exists(outputXMLFile))
            {
                StringBuilder sb = new StringBuilder();

                using (StreamReader sr = new StreamReader(outputXMLFile))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // the XML data from lmx contains some non-xml data, which needs
                        // to be removed
                        if(line.StartsWith(@">") || line.StartsWith(@"<"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
                using (StreamWriter outfile = new StreamWriter(xmlFile))
                {
                    outfile.Write(sb.ToString());
                }
            }

            if (File.Exists(outputXMLFile))
            {
                File.Delete(outputXMLFile);
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
    }
}
