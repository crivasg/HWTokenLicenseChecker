using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HWTokenLicenseChecker
{
    public partial class EnvironmentVariablesForm : Form
    {

        public List<String> Variables { set; private get; }

        public EnvironmentVariablesForm( )
        {
            InitializeComponent();
        }

        private void btn_Dismiss_EnvVarForm_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EnvironmentVariablesForm_Load(object sender, EventArgs e)
        {
            foreach (String eVariable in Variables)
            { 
                
            
            }
        }
    }
}
