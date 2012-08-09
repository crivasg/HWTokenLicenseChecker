using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Drawing;

//http://stackoverflow.com/questions/5427020/prompt-dialog-in-windows-forms

namespace HWTokenLicenseChecker
{
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form();
            prompt.Width = 500;
            prompt.Height = 200;
            prompt.Text = caption;

            prompt.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            Label textLabel = new Label() { Left = 50, Top = 20, Text = text + @":",
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
            };
            
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 ,
                Height = 100, Font = new Font("Microsoft Sans Serif", 24, FontStyle.Regular)
            };

            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 125,
                Height = 35, Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular) };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.ShowDialog();
            return textBox.Text;
        }

    }
}
