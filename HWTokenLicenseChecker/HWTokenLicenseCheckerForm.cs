using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Data.Common;
using System.Data.SQLite;

using System.IO;

namespace HWTokenLicenseChecker
{
    public partial class HWTokenLicenseCheckerForm : Form
    {
        private String sqlPath = @"";
        private String folder = @"";

        private int minHWPAFeatureId = -1;
        private int maxHWPAFeatureId = -2;

        public HWTokenLicenseCheckerForm()
        {
            InitializeComponent();

            Setup setup = new Setup();
            setup.CheckAndCreateAppData();
            setup.RemoveTempFiles();
            sqlPath = setup.DatabasePath;
            folder = setup.DataPath;

            GetLMXLicenseData();
        }

        private void GetLMXLicenseData()
        {

            lmxendutil lmx = new lmxendutil() { AppDataFolder = folder };
            lmx.ExecuteLMX();
           
            Clipboard.SetText(sqlPath);

            LMX2SQLite lmx2Sqlite = new LMX2SQLite {SqlitePath = sqlPath };
            lmx2Sqlite.CreateDatabase();
            lmx2Sqlite.ReadXMLLicenseData();
            lmx2Sqlite.ImportToDatabase();

            lmx2Sqlite.CloseDatabase();
            LoadToDataGridView();

            
  
        }

        private void LoadToDataGridView()
        {

            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + sqlPath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            String sqlQuery = @"SELECT LOWER(name) AS Username, host AS Hostname, MAX(used_licenses) AS Tokens, share_custom AS ""Custom String"", feature_id as ""Feature Id"" FROM user WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner = 0) GROUP BY Username, Hostname UNION SELECT LOWER(name), host, MAX(used_licenses)||""-HWPA"", share_custom, feature_id FROM user WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner != 0) GROUP BY name, host, feature_id ORDER BY Tokens DESC, Username ASC, Hostname ASC";

            SQLiteDataAdapter db = new SQLiteDataAdapter(sqlQuery, cnn);

            DataSet ds = new DataSet();
            ds.Reset();

            DataTable dt = new DataTable();
            db.Fill(ds);
            dt = ds.Tables[0];
            dataGridView.DataSource = dt;

            for (int i = 0; i < dataGridView.ColumnCount; ++i )
            {
                if (i != 1)
                {
                    dataGridView.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                
            }
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            

            // update the string in the status bar.
            int used_licenses = -1;
            int total_licenses = -1;
            String end_date = @"";
            sqlQuery = @"SELECT used_licenses,total_licenses,end FROM feature WHERE name = ""HyperWorks"";";

            //
            cmd.CommandText = sqlQuery;
            using(DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    used_licenses = int.Parse(reader[0].ToString());
                    total_licenses = int.Parse(reader[1].ToString());
                    end_date = reader[2].ToString();
                }
                reader.Close();
            }

            sqlQuery = @"SELECT port,ip,uptime FROM license_path;";
            cmd.CommandText = sqlQuery;

            int port = -1;
            String ip = @"";
            String uptime = @"";
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    port = int.Parse(reader[0].ToString());
                    ip = reader[1].ToString();
                    uptime = reader[2].ToString().Replace('=',' ');;

                }
                reader.Close();
            }

            toolStripStatusLabel1.Text = String.Format(@"Uptime: {0}. {1} of {2} license(s) used. Expiration Date: {3}", 
                uptime,used_licenses, total_licenses, end_date);

            this.Text += String.Format(@" {0}@{1}",port,ip);
            // {0}@{1}.

            // Get range HWPartner's feature...
            sqlQuery = @"SELECT MIN(feature_id),MAX(feature_id) FROM feature WHERE isPartner != 0;";
            cmd.CommandText = sqlQuery;

            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    minHWPAFeatureId = int.Parse(reader[0].ToString());
                    maxHWPAFeatureId = int.Parse(reader[1].ToString());
                }
                reader.Close();
            }

            cmd.Dispose();
            cnn.Close();

        }

        private void GetUserTokenInfo()
        {
            int numRows = dataGridView.Rows.Count;

            if (numRows < 1)
            {
                return;
            }

            String user = @"";
            String host = @"";
            int feature_id = -1;
            int tokens = -1;
            DataGridViewRow currentRow = dataGridView.CurrentRow;
            try
            {
                user = Convert.ToString(currentRow.Cells[0].Value);
                host = Convert.ToString(currentRow.Cells[1].Value);
                tokens = Convert.ToInt32(currentRow.Cells[2].Value);
                feature_id = Convert.ToInt32(currentRow.Cells[4].Value);
            }
            catch { return; }

            userTextBox.Text = user;
            tokensTextBox.Text = tokens.ToString();

            if (feature_id >= minHWPAFeatureId && feature_id <= minHWPAFeatureId)
            {
                // feature is HWPartner
            }
            else
            {
                ProcessTokens(@"=");
            }

            //MessageBox.Show(@"Hello!");
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {

            GetUserTokenInfo();
            
        }

        private void ProcessTokens(String comparisonString)
        { 
               
        }

    }
}
