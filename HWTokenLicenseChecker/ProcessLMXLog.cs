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
        private const String STATUS_STR = @"STATUS";
        private const String USER_INACTIVE = @"USER INACTIVE";


        public ProcessLMXLog( )
        {
            
        }

        public void ProcessLogFile()
        {
    
            ReadFile();
            PrepareForDatabase();
    
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

            foreach (String line in this.Lines.Reverse<String>())
            {
                if (line.Contains(READY_TO_SERVE))
                {
                   break;
                }
                if (line.Contains(STATUS_STR) || line.Contains(USER_INACTIVE))
                {
                    continue;
                }

                this.Usage.Add(line.Trim());
                
            }
            this.Lines = null;

            MessageBox.Show(String.Join(Environment.NewLine,this.Usage.ToArray()));

            //foreach (String item in this.Usage)
            //{ 
            //    if()
            //    {
                
            //    }
            //}

        }

    }
}
