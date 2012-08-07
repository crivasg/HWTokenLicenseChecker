using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace HWTokenLicenseChecker
{
    class EnvVariable
    {
        private String envName = @"";
        private String envValue = @"";

        public EnvVariable()
        { 
        
        }

        public String Name
        {
            set
            {
                envName = value;
            }
            get
            {
                return envName;
            }
        }

        public String Value
        {
            set
            {
                envValue = value;
            }
            get
            {
                return envValue;
            }
        }


        public void GetEnviromentVariableData()
        {
            if (!String.IsNullOrEmpty(envName))
            {
                envValue = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.Machine);
                if (String.IsNullOrEmpty(envValue))
                {
                    envValue = Environment.GetEnvironmentVariable(envName, EnvironmentVariableTarget.User);
                }

                if (String.IsNullOrEmpty(envValue))
                {
                    SetEnviromentVariableData();
                    QuestionToSetEnviromentVariable();
                }
            }
        }

        private void SetEnviromentVariableData()
        { 
        
        }

        private void QuestionToSetEnviromentVariable()
        {

            String question = String.Format(@"Set environment variable '{0}' to '{1}'", envName, envValue);

            var result = MessageBox.Show(question, @"Env. Variable " + envName,
            MessageBoxButtons.YesNo,MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Environment.SetEnvironmentVariable(envName, envValue, EnvironmentVariableTarget.User);
            }

        }        
    }
}
