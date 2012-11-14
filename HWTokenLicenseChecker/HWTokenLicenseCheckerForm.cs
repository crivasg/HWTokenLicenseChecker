﻿using System;
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
using System.Diagnostics;

namespace HWTokenLicenseChecker
{
    public partial class HWTokenLicenseCheckerForm : Form
    {
        private String databasePath = String.Empty;
        private String lmxconfigtool = String.Empty;

        private List<String> usersWithProblems = new List<String>();

        private const String GITHUB_REPO_URL = @"https://github.com/crivasg/HWTokenLicenseChecker";

        private const String SQL_QUERY_FOR_GRIDDATAVIEW =
        @"SELECT LOWER(name) AS Username, host AS Hostname, MAX(used_licenses) AS Tokens, share_custom AS 'Custom String','' AS Type, login_time AS Date, feature_id as 'Feature Id' 
    FROM user 
    WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner = 0) AND isBorrow = 0 GROUP BY Username, Hostname 
    UNION 
    SELECT LOWER(name), host, MAX(used_licenses), share_custom,'HWPA',login_time AS Date, feature_id 
    FROM user 
    WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner != 0) AND isBorrow = 0 GROUP BY name, host, feature_id  
    UNION 
    SELECT LOWER(name), host, MAX(used_licenses), share_custom, 'BRRW', login_time AS Date, feature_id 
    FROM user 
    WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner = 0) AND isBorrow = 1 GROUP BY name, host 
    ORDER BY Tokens DESC, Username ASC, Hostname ASC;";

        private int minHWPAFeatureId = -1;
        private int maxHWPAFeatureId = -2;

        private int selectedRow = -1;
        private bool isRunning = false;

        public HWTokenLicenseCheckerForm()
        {

            UpgradeSettings();
            InitializeComponent();

            GetLMXLicenseData();

            AddIconsToMenuItems();
        }

        private void GetLMXLicenseData()
        {

            isRunning = true;

            Setup setup = new Setup();
            setup.CheckAndCreateAppData();
            setup.RemoveTempFiles();
            databasePath = setup.DatabasePath;

            lmxendutil lmx = new lmxendutil() {
                XMLFile = setup.XMLPath
            };

            lmx.ExecuteLMX();
            lmxconfigtool = lmx.LMXConfigTool;

            Status status = lmx.AppStatus;

            if (status != Status.OK)
            {
                String msg = lmx.LMXStatusMessage();
                MessageBox.Show(msg, @"LMX Error");

                return;
            }
           
            //Clipboard.SetText(sqlPath);

            LMX2SQLite lmx2Sqlite = new LMX2SQLite {
                DatabasePath = setup.DatabasePath,
                XMLFile = setup.XMLPath
            };
            lmx2Sqlite.Run();
            lmx2Sqlite.CloseDatabase();
         
            LoadToDataGridView();

            // checks if there is user with locked tokens.
            CheckForLockedTokens();

            isRunning = false;
        }

        private void LoadToDataGridView()
        {
            String sqlQuery = String.Empty;

            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            SQLiteDataAdapter db = new SQLiteDataAdapter(SQL_QUERY_FOR_GRIDDATAVIEW, cnn);

            DataSet ds = new DataSet();
            ds.Reset();

            DataTable dt = new DataTable();
            db.Fill(ds);
            dt = ds.Tables[0];
            dataGridView.DataSource = dt;

            for (int i = 0; i < dataGridView.ColumnCount; ++i )
            {
                if (i != 3)
                {
                    dataGridView.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                
            }
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            

            // update the string in the status bar.
            int used_licenses = -1;
            int total_licenses = -1;
            String end_date = String.Empty;
            sqlQuery = @"SELECT used_licenses,total_licenses,end FROM feature WHERE name = 'HyperWorks';";

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
            String ip = String.Empty;
            String uptime = String.Empty;
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

            int numberOfUsers = dataGridView.RowCount;

            String numberOfUsersStr = numberOfUsers != 1 ? @"Users" : @"User";
            toolStripStatusLabel1.Text = String.Format(@"{0} {1}. Uptime: {2}. {3} of {4} license(s) used. Expiration Date: {5}", 
                numberOfUsers,numberOfUsersStr, uptime,used_licenses, total_licenses, end_date);

           
            this.Text += String.Format(@" {0}@{1} ",port,ip);
            // {0}@{1}.

            // Get range HWPartner's feature...
            sqlQuery = @"SELECT MIN(feature_id),MAX(feature_id) FROM feature WHERE isPartner = 1;";
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

            String user = String.Empty;
            String host = String.Empty;
            String tokenType = String.Empty;
            int feature_id = -1;
            int tokens = -1;
            DataGridViewRow currentRow = dataGridView.CurrentRow;
            selectedRow = currentRow.Index;

            try
            {
                user = Convert.ToString(currentRow.Cells["Username"].Value).ToLower();
                host = Convert.ToString(currentRow.Cells["Hostname"].Value).ToUpper();
                tokenType = Convert.ToString(currentRow.Cells["Type"].Value).ToUpper();
                tokens = int.Parse(currentRow.Cells["Tokens"].Value.ToString());
                borrowHWPATextBox.Text = String.Empty;
                borrowHWPATextBox.BackColor = Color.FromKnownColor(KnownColor.Window);
                if (tokenType.Contains(@"HWPA") || tokenType.Contains(@"BRRW"))
                {
                   
                    borrowHWPATextBox.BackColor = Color.MistyRose;

                    //String[] tmpTokensArray = tmpTokens.Split(new Char[] {'-'});
                    //tokens = int.Parse(tmpTokensArray[0]);

                    if (tokenType.Contains(@"HWPA"))
                    {
                        borrowHWPATextBox.Text = @"HWPA";
                    }
                    if (tokenType.Contains(@"BRRW"))
                    {
                        borrowHWPATextBox.Text = @"BORROW";
                    }
                }

                int numOfCellsInRow = currentRow.Cells.Count;
                feature_id = Convert.ToInt32(currentRow.Cells["Feature Id"].Value);
            }
            catch { return; }

            userTextBox.Text = user;
            tokensTextBox.Text = tokens.ToString();
            //borrowHWPATextBox.Text = String.Empty;

            if (feature_id >= minHWPAFeatureId && feature_id <= minHWPAFeatureId)
            {
                // feature is HWPartner
                //borrowHWPATextBox.Text = @"HWPA";
                ProcessTokens(user, host, feature_id, 1);
            }
            else
            {
                // feature normal
                ProcessTokens(user, host, feature_id, 0);
            }

            //MessageBox.Show(@"Hello!");
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {

            GetUserTokenInfo();
            
        }
        /// <summary>
        /// Process the HWU tokens used. Args: 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="host"></param>
        /// <param name="feature_id"></param>
        /// <param name="isPartner"> 1 = HWPA tokens used, 0 - Normal tokens</param>
        private void ProcessTokens(String user, String host, int feature_id, int isPartner)
        {

            //@"SELECT DISTINCT feature_id FROM feature WHERE isPartner = 1"

            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);

            // get the features used
            String sqlQuery = String.Format(@"SELECT DISTINCT feature.name,user.login_time,user.host||'/'||user.ip 
                FROM user JOIN feature USING (feature_id) 
                WHERE user.name = '{0}' AND user.host = '{1}' AND user.feature_id IN ( 
                    SELECT DISTINCT feature_id FROM feature WHERE isPartner = {2}
                );", user, host, isPartner);

            cmd.CommandText = sqlQuery;

            String tmp = String.Empty;
            //String logTmp = String.Empty;
            List<String> featureList = new List<String>();
            List<DateTime> dateList = new List<DateTime>();
            List<String> hostList = new List<String>();

            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tmp = reader[0].ToString();
                    String tmpString = reader[1].ToString().Replace(@"""", @"");
                    String tmpHost = reader[2].ToString();
                    DateTime dt = DateTime.Parse(tmpString, System.Globalization.CultureInfo.InvariantCulture);
                    dateList.Add(dt);

                    if (!featureList.Contains(tmp))
                    {
                        featureList.Add(tmp);
                    }

                    if (!hostList.Contains(tmpHost))
                    {
                        hostList.Add(tmpHost);
                    }
                }
                reader.Close();
            }
            cmd.Dispose();
            cnn.Close();

            featureTextBox.Text = String.Join(Environment.NewLine, featureList.ToArray());

            //Sorts the date list
            dateList.Sort((x, y) => x.CompareTo(y));

            int dateIndex = 0;
            if ( borrowHWPATextBox.Text == @"BORROW")
            {
                dateIndex = dateList.Count - 1;
            }

            checkoutTextBox.Text = dateList[dateIndex].ToString();

            TimeSpan ts = DateTime.Now - dateList[dateIndex];
            int days = Math.Abs(ts.Days);
            int hours = Math.Abs(ts.Hours );
            int minutes = Math.Abs(ts.Minutes);

            String sessionDuration = String.Format(@"{0}days {1:00}hrs {2:00}min", 
                days, hours, minutes);

            sessionTimeTextBox.Text = sessionDuration;
            hostTextBox.Text = hostList[0];
        
        }

        private void userTextBox_Enter(object sender, EventArgs e)
        {
            //MessageBox.Show(selectedRow.ToString());
            dataGridView.Focus();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView.MultiSelect = true;
            dataGridView.SelectAll();
            DataObject dataObj = dataGridView.GetClipboardContent();
            Clipboard.SetDataObject(dataObj, true);
            dataGridView.ClearSelection();
            dataGridView.MultiSelect = false;

            dataGridView.Rows[selectedRow].Selected = true;

            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void copyRowToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DataGridViewRow currentRow = dataGridView.CurrentRow;
            int numCells = currentRow.Cells.Count;
            String textToCopy = String.Empty;
            for (int ii = 0; ii < numCells; ++ii)
            {
                textToCopy += String.Format("{0}\t", currentRow.Cells[ii].Value);
            }

            Clipboard.SetText(textToCopy);
            
        }

        private void csvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String csvString = String.Empty;
            int numCols = dataGridView.Columns.Count;
            int numRows = dataGridView.Rows.Count;

            for (int col = 0; col < numCols; ++col)
            {
                csvString += dataGridView.Columns[col].HeaderText + ",";
            }
            csvString += Environment.NewLine;
            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numCols; ++col)
                {
                    csvString += dataGridView.Rows[row].Cells[col].Value + ",";
                }
                csvString += Environment.NewLine;
            }
            // 
            saveCSVFileDialog.Title = "Export data inta CSV file";
            saveCSVFileDialog.Filter = "CSV Files|*.csv|All Files|*.*";
            if (saveCSVFileDialog.ShowDialog() == DialogResult.OK)
            {
                //MessageBox.Show(csvString + Environment.NewLine + saveCSVFileDialog.FileName);
                StreamWriter streamWriter = new StreamWriter(saveCSVFileDialog.FileName);

                streamWriter.Write(csvString);
                streamWriter.Close();
            }

        }

        private void sQLiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile("sqlite3", "SQLite3 Database|*.sqlite3|All Files|*.*", "Export SQLite Data");
        }

        private void lmxConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(lmxconfigtool);

        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                MessageBox.Show(@"Is running");
            }
            if (!isRunning)
            {
                refreshToolStripMenuItem.Enabled = false;
                this.Text = @"HW Token License Checker";
                GetLMXLicenseData();
                refreshToolStripMenuItem.Enabled = true;
            }
        }

        private void xMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile("xml", "XML Document|*.xml|All Files|*.*", "Export XML Document");
        }

        private void SaveFile(String format, String filter, String title)
        {

            String destination = String.Empty;
            String source = databasePath;
            saveCSVFileDialog.Title = title;
            saveCSVFileDialog.Filter = filter;

            if(format.ToLower() == @"xml")
            {
                source = Path.ChangeExtension(source,format.ToLower());
            }

            if (saveCSVFileDialog.ShowDialog() == DialogResult.OK)
            {
                destination = saveCSVFileDialog.FileName;
                try
                {
                    File.Copy(source,destination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {

                }
            }       
        }

        private void HWTokenLicenseCheckerForm_ResizeEnd(object sender, EventArgs e)
        {
            Properties.Settings.Default.FormLocation = this.Location;
            Properties.Settings.Default.Save();

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

            String aboutText = @"This tool as created by Cesar A. Rivas ( crivasg@gmail.com ) to check and track the usage of the HyperWorks tokens by quering the LMX tools."
                + Environment.NewLine + Environment.NewLine
                + @"Inspried by LS-DYNA Program Manager. The code for this application is hosted at http://github.com/. Pull requsts with improvements are welcome! "
                + @"Select Help> Visit project Github repo to browse the code"
                + Environment.NewLine + Environment.NewLine 
                + @"You are free to use and modify this application. If you modify the application, please send a pull request with your improvements." ;

            AboutForm aboutForm = new AboutForm(aboutText);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.ShowDialog();
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LaunchURL(GITHUB_REPO_URL, @"Failed to open the Github repo.");
        }

        private void addIssueStripMenuItem_Click(object sender, EventArgs e)
        {
            String issuesURL = String.Format(@"{0}/issues", GITHUB_REPO_URL);
            LaunchURL(issuesURL, @"Failed to open the Github Issues.");
        }

        private void copyRepoStripMenuItem1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(GITHUB_REPO_URL);
        }

        private void LaunchURL(String url, String textMsg)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show(textMsg+ Environment.NewLine + ex.ToString());
            }
            finally
            {

            }       
        }

        private void processLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void CheckForLockedTokens()
        {
            String sqlQuery = @"SELECT DISTINCT  user.name||':'||user.host||':'||user.used_licenses  
                    FROM user JOIN feature USING (feature_id) 
                    WHERE user.share_custom LIKE '%:%:%' AND feature.name = 'HyperWorks';";

            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            cmd.CommandText = sqlQuery;

            String share_custom = String.Empty;
            
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {

                    share_custom = reader[0].ToString();
                    usersWithProblems.Add(share_custom);

                }
                reader.Close();
            }
            cmd.Dispose();
            cnn.Close();     

        }

        private void ApplyStyleToCells()
        {
            // http://blog.csharphelper.com/2010/05/31/calculate-a-datagridview-columns-value-and-highlight-specific-values-in-c.asp

            dataGridView.ReadOnly = false;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {

                String userData = (String)row.Cells["Username"].Value + @":" +
                                  (String)row.Cells["Hostname"].Value + @":" + 
                                  (String)row.Cells["Tokens"].Value.ToString();

                if (usersWithProblems.Contains(userData))
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.BackColor = Color.MistyRose;
                    }
                }
                else
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.BackColor = Color.White;
                    }
                }
            }

            this.Update();
            dataGridView.ReadOnly = true;
        }

        private void dataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            ApplyStyleToCells();
        }

        private void UpgradeSettings()
        {
            if (!Properties.Settings.Default.Upgraded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Upgraded = true;
                Properties.Settings.Default.Save();
            }
            this.Location = Properties.Settings.Default.FormLocation;
        }

        private void dataGridView_Sorted(object sender, EventArgs e)
        {
            ApplyStyleToCells();
        }

        private void AddIconsToMenuItems()
        {
            if (File.Exists(this.lmxconfigtool))
            {
                lmxConfigToolStripMenuItem.Image = Icon.ExtractAssociatedIcon(this.lmxconfigtool).ToBitmap();
            }
        }

        /// <summary>
        /// Quits the application when the esc key is pressed
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) this.Close();
            return base.ProcessCmdKey(ref msg, keyData);
        }

    }
}
