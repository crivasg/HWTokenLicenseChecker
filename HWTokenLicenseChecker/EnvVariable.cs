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
    }
}
