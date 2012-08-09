using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Windows.Forms;

namespace HWTokenLicenseChecker
{

    enum EnvVarType
    {
        FolderPath,
        FilePath,
        IntegerValue,
        FloatValue,
        StringValue,
        HostIp,
        HostPortAndIp
    };

    class EnvVariable
    {
        private String envName = @"";
        private String envValue = @"";
        private String description = @"";
        private EnvVarType type;

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

        public String Description
        {
            set
            {
                description = value;
            }
            get
            {
                return description;
            }
        }

        public EnvVarType Type
        {
            set
            {
                type = value;
            }        
        }


        public void GetEnviromentVariableData()
        {
            GetHostPortAndIp();

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

		    switch (type)
		    {
			    case EnvVarType.FolderPath :
				    GetFolderPath();
				    break;
			    case EnvVarType.FilePath :
				    GetFilePath();
				    break;
			    case EnvVarType.IntegerValue :
				    GetIntegerValue();
				    break;
			    case EnvVarType.FloatValue :
                    GetFloatValue();
				    break;
			    case EnvVarType.StringValue :
                    GetStringValue();
				    break;
			    case EnvVarType.HostIp :
				    GetHostIp();
				    break;
			    case EnvVarType.HostPortAndIp :
				    GetHostPortAndIp();
				    break;
		    }        
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

        private void GetFolderPath()
        {
            FolderBrowserDialog browserDialog = new FolderBrowserDialog();
            browserDialog.Description = description;
            browserDialog.ShowNewFolderButton = false;
            browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (browserDialog.ShowDialog() == DialogResult.OK)
            {
                envValue = browserDialog.SelectedPath;
            }
        }

        private void GetFilePath()
        {
            // change to openFileBrowserDialog
            OpenFileDialog browserDialog = new OpenFileDialog();
            browserDialog.Title = description;
            browserDialog.CheckFileExists = true;
            browserDialog.Multiselect = false;
            browserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (browserDialog.ShowDialog() == DialogResult.OK)
            {
                envValue = browserDialog.FileName;
            }
        }

        private void GetIntegerValue()
        {
            bool flag = false;
            int number = -9999999;

            while (!flag)
            {
                GetStringValue();
                flag = int.TryParse(envValue, out number);
                //MessageBox.Show(String.Format(@"{0} {1} {2}", envValue,number, flag));
            }
            
        }

        private void GetFloatValue()
        {
            bool flag = false;
            float number = -9999999;

            while (!flag)
            {
                GetStringValue();
                flag = float.TryParse(envValue, out number);
                //MessageBox.Show(String.Format(@"{0} {1} {2}", envValue,number, flag));
            }
        }

        private void GetStringValue()
        {

            bool flag = true;
            while (flag)
            {
                envValue = Prompt.ShowDialog("Value", String.Format(@"Enter value for {0}",envName) );
                if (!String.IsNullOrEmpty(envValue))
                {
                    flag = false;
                }
            }
        }

        private void GetHostIp()
        {

            bool flag = true;
            String tmp1 = @"";

            while (flag)
            {
                GetStringValue();

                Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                MatchCollection result = ip.Matches(envValue);
                tmp1 = envValue;
                if (result.Count > 0)
                {
                    envValue = result[0].ToString();
                    //MessageBox.Show(String.Format(@"{0} {1}", tmp1, envValue));
                    flag = false;
                }

            }

        }

        private void GetHostPortAndIp()
        {
            bool flag = true;
            String tmp1 = @"";

            while (flag)
            {
                GetStringValue();

                Regex ip = new Regex(@"\b\d{1,100}\@\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                MatchCollection result = ip.Matches(envValue);
                tmp1 = envValue;
                if (result.Count > 0)
                {
                    envValue = result[0].ToString();
                    //MessageBox.Show(String.Format(@"{0} {1}", tmp1, envValue));
                    flag = false;
                }

            }
        }

    }
}
