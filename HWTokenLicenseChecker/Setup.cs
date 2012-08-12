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
        private String dbPath = @"";

        public Setup()
        {
        
        }

        public String DataPath
        {
            get { return dirname; }
        }

        public String DatabasePath
        {
            get { return dbPath; }
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

            dbPath = Path.Combine(dirname, @"Licenses.sqlite3");

         }

         /// <summary>
         /// Removes all unneeded and temporary files at %APPDATA%\HWTokenLicenseChecker\
         /// folders
         /// </summary>
         public void RemoveTempFiles()
         {

             String[] fileNames = Directory.GetFiles(dirname);
             foreach (String fileName in fileNames)
             {
                 //String tmp = Path.Combine(dirname, fileName);

                 if (File.Exists(fileName) && !Path.GetFileName(fileName).Equals(@"Licenses.sqlite3"))
                 {
                     try
                     {
                         File.Delete(fileName);
                     }
                     catch (IOException deleteError)
                     {
                         MessageBox.Show(deleteError.ToString());
                     }
                 }

             }
         }

    }
}
