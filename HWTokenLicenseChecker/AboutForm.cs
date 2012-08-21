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
        public AboutForm(String aboutText)
        {
            InitializeComponent();
            textBox1.Text = aboutText;   
        }

        private void dismissAboutButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
