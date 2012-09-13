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

        public String AppDataPath { get; private set; }
        public String DatabasePath { get; private set; }
        public String XMLPath { get; private set; }

        private const String SQLITE_FILE_NAME = @"Licenses.sqlite3";
        private const String XML_FILE_NAME = @"Licenses.xml";


        public Setup()
        {
            this.AppDataPath = String.Empty;
            this.DatabasePath = String.Empty;
            this.XMLPath = String.Empty;
        }

        /// <summary>
        /// Method to check if the %%APPDATA%% folder exists,
        /// if not, it creates it
        /// </summary>
         public void CheckAndCreateAppData()
	     {
             this.AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
             String exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
             this.AppDataPath = Path.Combine(this.AppDataPath, exeName);

             if (!Directory.Exists(this.AppDataPath))
             {
                 Directory.CreateDirectory(this.AppDataPath);
             }

             this.DatabasePath = Path.Combine(this.AppDataPath, SQLITE_FILE_NAME);
             this.XMLPath = Path.Combine(this.AppDataPath, XML_FILE_NAME);

         }

         /// <summary>
         /// Removes all unneeded and temporary files at %APPDATA%\HWTokenLicenseChecker\
         /// folders
         /// </summary>
         public void RemoveTempFiles()
         {

             try
             {
                 if (File.Exists(this.XMLPath))
                 {
                     File.Delete(this.XMLPath);
                 }
             }
             catch (IOException deleteError)
             {
                 String tmp = String.Format(@"Error while deleting the file: {0}",
                     this.XMLPath);
                 MessageBox.Show(deleteError.ToString());
             }
             finally
             {
             }
         }

    }
}
