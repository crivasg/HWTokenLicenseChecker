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

        public ProcessLMXLog( )
        {
            
        }

        public void ProcessLogFile()
        {
            ReadFile();
        
        }

        private void ReadFile()
        {
            StreamReader myFile = new StreamReader(this.Path);
            String myString = myFile.ReadToEnd();
            String[] lines = myString.Split('\n');
            myFile.Close();

            MessageBox.Show(String.Join(Environment.NewLine,lines));
            
        }
    }
}
