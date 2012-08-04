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

            String dbPath = Path.Combine(dirname, @"Licenses.sqlite3");

        }
        public void CheckAndCreateDatabase()
        {
            if(!File.Exists(dbPath))
            {
	            CreateDatabase();
            }
            else
            {
                CheckDatabaseSchema();
            }
        }

        private void CreateDatabase()
        { 
        
        }

        private void CheckDatabaseSchema()
        { 
        
        }
    }
}
