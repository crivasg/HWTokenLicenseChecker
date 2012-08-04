using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HWTokenLicenseChecker
{
    class Setup
    {

        private String dirname = @"";

        public Setup()
        {
        
        }

        public String DataPath
        {
            get { return dirname; }
        }

        /// <summary>
        /// Method to check if the %%APPDATA%% folder exists,
        /// if not, it creates it
        /// </summary>
         public void CheckAndCreateAppData()
	    {
            String appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            String exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            dirname = Path.Combine(appDataDir, exeName);

            if (!Directory.Exists(dirname))
            {
                Directory.CreateDirectory(dirname);
            }


        }
    }
}
