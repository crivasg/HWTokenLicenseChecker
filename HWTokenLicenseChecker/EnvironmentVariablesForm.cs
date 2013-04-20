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
            MessageBox.Show(evDataGridView.CurrentRow.Index.ToString());
        }
    }
}
