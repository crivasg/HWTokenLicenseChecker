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
        //private String envName = @"";
        //private String envValue = @"";
        //private String description = @"";
        //private EnvVarType type;

        private String[] regexStrings = {
				@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",              // IP
				@"\b\d{1,100}\@\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"    //PORT@IP
		    };
        private Char[] splitters = new Char[] { '@', '.' };

        public String Name { private get; set; }
        public String Value { get; private set; }
        public String Description { private get; set; }
        public EnvVarType Type { private get; set; }

        public EnvVariable()
        { 
            
        }

        public void GetEnviromentVariableData()
        {
            //GetHostPortAndIp();

            if (!String.IsNullOrEmpty(this.Name))
            {
                this.Value = Environment.GetEnvironmentVariable(this.Name, EnvironmentVariableTarget.Machine);
                if (String.IsNullOrEmpty(this.Value))
                {
                    this.Value = Environment.GetEnvironmentVariable(this.Name, EnvironmentVariableTarget.User);
                }

                if (String.IsNullOrEmpty(this.Value))
                {
                    SetEnviromentVariableData();
                    QuestionToSetEnviromentVariable();
                }
            }
        }

        private void SetEnviromentVariableData()
        { 

		    switch (this.Type)
		    {
			    case EnvVarType.FolderPath :
				    GetFolderPath();
				    break;
			    case EnvVarType.FilePath :
				    GetFilePath();
				    break;
			    case EnvVarType.IntegerValue :
                    this.Value = ((int)this.ParseStringValueToNumeric(typeof(int))).ToString();
				    break;
			    case EnvVarType.FloatValue :
                    this.Value = ((float)this.ParseStringValueToNumeric(typeof(float))).ToString();
				    break;
			    case EnvVarType.StringValue :
                    GetStringValue();
				    break;
			    case EnvVarType.HostIp :
				    GetHostPortAndIp(0);
				    break;
			    case EnvVarType.HostPortAndIp :
                    GetHostPortAndIp(1);
				    break;
		    }        
        }

        private void QuestionToSetEnviromentVariable()
        {

            String question = String.Format(@"Set environment variable '{0}' to '{1}'", this.Name, this.Value);

            var result = MessageBox.Show(question, @"Env. Variable " + this.Name,
            MessageBoxButtons.YesNo,MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Environment.SetEnvironmentVariable(this.Name, this.Value, EnvironmentVariableTarget.User);
            }

        }

        private void GetFolderPath()
        {
            FolderBrowserDialog browserDialog = new FolderBrowserDialog();
            browserDialog.Description = this.Description;
            browserDialog.ShowNewFolderButton = false;
            browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (browserDialog.ShowDialog() == DialogResult.OK)
            {
                this.Value = browserDialog.SelectedPath;
            }
        }

        private void GetFilePath()
        {
            // change to openFileBrowserDialog
            OpenFileDialog browserDialog = new OpenFileDialog();
            browserDialog.Title = this.Description;
            browserDialog.CheckFileExists = true;
            browserDialog.Multiselect = false;
            browserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (browserDialog.ShowDialog() == DialogResult.OK)
            {
                this.Value = browserDialog.FileName;
            }
        }

        private void GetIntegerValue()
        {
            bool flag = false;
            int number = -9999999;

            while (!flag)
            {
                GetStringValue();
                flag = int.TryParse(this.Value, out number);
                //MessageBox.Show(String.Format(@"{0} {1} {2}", envValue,number, flag));
            }
            
        }

        private Object ParseStringValueToNumeric(Type type)
        {
            bool flag = false;
            Object tmp = null;

            while (!flag)
            {
                GetStringValue();
                try
                {
                    tmp = Convert.ChangeType(this.Value, type);
                    flag = true;
                }
                catch
                {
                    flag = false;
                }
            }

            return tmp;
        }

        private void GetFloatValue()
        {
            bool flag = false;
            float number = -9999999;

            while (!flag)
            {
                GetStringValue();
                flag = float.TryParse(this.Value, out number);
                //MessageBox.Show(String.Format(@"{0} {1} {2}", envValue,number, flag));
            }
        }

        private void GetStringValue()
        {

            bool flag = true;
            while (flag)
            {
                this.Value = Prompt.ShowDialog("Value", String.Format(@"Enter value for {0}",this.Name) );
                if (!String.IsNullOrEmpty(this.Value))
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

                String[] tmpArray = this.Value.Split(splitters);

                if (tmpArray.Length == 4)
                { 

                    Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    MatchCollection result = ip.Matches(this.Value);
                    tmp1 = this.Value;
                    if (result.Count > 0)
                    {
                        this.Value = result[0].ToString();
                        //MessageBox.Show(String.Format(@"{0} {1}", tmp1, envValue));
                        flag = false;
                    }
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

                String[] tmpArray = this.Value.Split(splitters);
                if (tmpArray.Length == 5)
                {
                    Regex ip = new Regex(@"\b\d{1,100}\@\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    MatchCollection result = ip.Matches(this.Value);
                    tmp1 = this.Value;
                    if (result.Count > 0)
                    {
                        this.Value = result[0].ToString();
                        //MessageBox.Show(String.Format(@"{0} {1}", tmp1, this.Value));
                        flag = false;
                    }
                }

            }
        }

        private void GetHostPortAndIp(int indexOfRegex)
        {
            bool flag = true;
            String[] tmpStr = regexStrings[indexOfRegex].Split(splitters);
            int length = tmpStr.Length;

            while (flag)
            {
                GetStringValue();

                String[] tmpArray = this.Value.Split(splitters);
                if (tmpArray.Length == length)
                {
                    Regex ip = new Regex(regexStrings[indexOfRegex]);
                    MatchCollection result = ip.Matches(this.Value);
                    //tmp1 = this.Value;
                    if (result.Count > 0)
                    {
                        this.Value = result[0].ToString();
                        //MessageBox.Show(String.Format(@"{0} {1}", tmp1, this.Value));
                        flag = false;
                    }
                }

            }
        }

    }
}
