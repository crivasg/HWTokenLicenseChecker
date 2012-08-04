using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace HWTokenLicenseChecker
{
    class Setup
    {

        private String dirname = @"";
        private String dbPath = @"";

        private SQLiteConnection cnn;

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

         public void RemoveTempFiles()
         {

             String[] fileNames = Directory.GetFiles(dirname);
             foreach (String fileName in fileNames)
             {
                 String tmp = Path.Combine(dirname, fileName);
                 
                 if(File.Exists(tmp) && !Path.GetFileName(tmp).Equals( @"Licenses.sqlite3") )
                 {
                     try
                     {
                         File.Delete(tmp);
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
