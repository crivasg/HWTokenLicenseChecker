using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        
        }
    }
}
