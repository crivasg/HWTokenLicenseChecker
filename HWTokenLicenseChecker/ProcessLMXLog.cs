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

        private const String READY_TO_SERVE = @"Ready to serve...";
        private const String CHECKOUT_STR = @"CHECKOUT";
        private const String CHECKIN_STR = @"CHECKIN";

        public ProcessLMXLog( )
        {
            
        }

        public void ProcessLogFile()
        {
            ReadFile();
            PrepareForDatabase();
            MessageBox.Show(this.Usage.Count.ToString());
        
        }

        public void Close()
        { 
        
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

            //int index = -1;

            foreach (String line in this.Lines.Reverse<String>())
            {
                if (line.Trim().Contains(READY_TO_SERVE))
                {
                   break;
                }

                this.Usage.Add(line.Trim());
                

                //if (line.Trim().Length > 0 && (line.Contains(@"CHECKIN") || line.Contains(@"CHECKOUT")))
                //{ 
                //    String[] splittedLine = line.Trim().Split(' ');

                //}
            }
        }

    }
}
