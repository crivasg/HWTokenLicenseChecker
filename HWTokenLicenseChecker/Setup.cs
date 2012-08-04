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

        public Setup()
        {
        
        }

        /// <summary>
        /// Method to check if the %%APPDATA%% folder exists,
        /// if not, it creates it
        /// </summary>
         public void CheckAndCreateAppData()
	    {
            String appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            String appDataDir2 = Application.UserAppDataPath;


        }
    }
}
