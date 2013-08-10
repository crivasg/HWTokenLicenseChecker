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
        public List<EnvironmentVariableTarget> Targets { set; private get; }

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
            evDataGridView.ColumnHeadersBorderStyle = ProperColumnHeadersBorderStyle;

            foreach (String eVariable in Variables)
            {
                evDataGridView.Rows.Add(eVariable, Utilities.GetVariableValue(eVariable));
            }

            evDataGridView.ClearSelection();
        }


        /// <summary>
        /// Remove the column header border in the Aero theme in Vista,
        /// but keep it for other themes such as standard and classic.
        /// http://www.dotnetperls.com/datagridview
        /// </summary>
        static DataGridViewHeaderBorderStyle ProperColumnHeadersBorderStyle
        {
            get
            {
                return (SystemFonts.MessageBoxFont.Name == "Segoe UI") ?
                DataGridViewHeaderBorderStyle.None :
                DataGridViewHeaderBorderStyle.Raised;
            }
        }

        private void evDataGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            DataGridViewRow currentRow = evDataGridView.CurrentRow;
            String variableName = Convert.ToString(currentRow.Cells[0].Value);
            String variableValue = Convert.ToString(currentRow.Cells[1].Value);

            String bb = Prompt.ShowDialog(variableName, @"Enter value for enviroment variable", variableValue);

            if (bb.CompareTo(variableValue) != 0)
            { 
                // set the enviroment variable
                int index = Variables.IndexOf(variableName);
                EnvironmentVariableTarget tgt = Targets[index];

                try
                {
                    Environment.SetEnvironmentVariable(variableName, bb, tgt);
                    currentRow.Cells[1].Value = bb;
                }
                catch
                {
                    MessageBox.Show(String.Format("Could not set the variable {0} to {1}", variableName,bb));
                }

            }

            //MessageBox.Show(String.Format(@"{0}:{1}", variableName, variableValue));
            evDataGridView.ClearSelection();

        }
    }
}
