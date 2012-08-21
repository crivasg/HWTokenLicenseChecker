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
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            textBox1.Text = @"This tool was created by Cesar A. Rivas (crivasg@gmail.com)";
            
        }

        private void dismissAboutButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
