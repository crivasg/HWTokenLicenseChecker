using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Windows.Forms;

namespace HWTokenLicenseChecker
{
    class ProcessLMXLog
    {
        public String Path { get; set; }
        private String[] Lines { get; set; }
        private List<String> Usage { get; set; }

        public ProcessLMXLog( )
        {
            
        }

        public void ProcessLogFile()
        {
            ReadFile();
            PrepareForDatabase();
            MessageBox.Show(this.Lines.Length.ToString());
        
        }

        private void ReadFile()
        {
            StreamReader myFile = new StreamReader(this.Path);
            String myString = myFile.ReadToEnd();
            this.Lines = myString.Split('\n');
            myFile.Close();
        }

        private void PrepareForDatabase()
        {
            this.Usage = new List<String>();

            foreach (String line in this.Lines)
            {
                if (line.Trim().Length > 0 && (line.Contains(@"CHECKIN") || line.Contains(@"CHECKOUT")))
                { 
                    String[] splittedLine = line.Trim().Split(' ');

                }
            }
        }

    }
}
