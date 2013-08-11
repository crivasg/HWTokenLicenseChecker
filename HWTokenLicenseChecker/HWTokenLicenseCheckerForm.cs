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
using System.Diagnostics;
using System.Collections;

namespace HWTokenLicenseChecker
{
    public partial class HWTokenLicenseCheckerForm : Form
    {
        private String databasePath = String.Empty;
        private String lmxconfigtool = String.Empty;

        private List<String> APP_ENV_VARS = new List<String> () { @"ALTAIR_HOME", @"LMX_LICENSE_PATH" };
        private List<EnvironmentVariableTarget> APP_ENV_VARS_TARGET = new List<EnvironmentVariableTarget>();

        private List<String> usersWithProblems = new List<String>();

        private const String GITHUB_REPO_URL = @"https://github.com/crivasg/HWTokenLicenseChecker";

        private int minHWPAFeatureId = -1;
        private int maxHWPAFeatureId = -2;

        private int selectedRow = -1;
        private bool isRunning = false;

        public HWTokenLicenseCheckerForm()
        {
            
            UpgradeSettings();
            InitializeComponent();
            
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
                isRunning = false;

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
            SQLiteDataAdapter db = new SQLiteDataAdapter(Queries.FillDataGridView, cnn);

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

            //
            cmd.CommandText = Queries.GatherHyperworksTokensData;
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

            cmd.CommandText = Queries.GatherLicenseServerData;

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
            cmd.CommandText = Queries.GetMinMaxPartnerFeaturesIds;

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

            cmd.CommandText = String.Format(Queries.GatherTokenUsagePerUsernameAndHostname, user,host,isPartner);

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
            Application.Exit();
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

            if (File.Exists(lmxconfigtool) && !String.IsNullOrEmpty(lmxconfigtool))
            {
                Process.Start(lmxconfigtool);
            }
            else
            {
                MessageBox.Show(String.Format(@"{0} does not exist.", Path.GetFileName(lmxconfigtool)));
            }

        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                MessageBox.Show(@"Is running");
            }
            else
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

            String aboutText = Properties.Resources.AboutStringRes; 
            
            AboutForm aboutForm = new AboutForm(aboutText.Replace("#",Environment.NewLine+Environment.NewLine));
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
            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            cmd.CommandText = Queries.UsersWithLockedTokens;

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

        private void GetTargetOfEnviromentVariables()
        {
            IDictionary userEnvironmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            IDictionary machineEnvironmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);

            int index = 0;
            bool found = false;
            foreach (String env in APP_ENV_VARS)
            {
                found = false;
                index = APP_ENV_VARS.IndexOf(env);

                IEnumerable<String> keyList = userEnvironmentVariables.Keys.Cast<String>().Where(x => x.CompareTo(env) == 0);

                foreach (String key in keyList)
                { 
                    APP_ENV_VARS_TARGET.Insert(index,EnvironmentVariableTarget.User);
                    found = true;
                    break;
                }

                if (found)
                {
                    continue;
                }

                keyList = machineEnvironmentVariables.Keys.Cast<String>().Where(x => x.CompareTo(env) == 0);

                foreach (String key in keyList)
                {
                    APP_ENV_VARS_TARGET.Insert(index, EnvironmentVariableTarget.Machine);
                    found = true;
                    break;
                }
                if (found)
                {
                    continue;
                }

                APP_ENV_VARS_TARGET.Insert(index, EnvironmentVariableTarget.Process);
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

        private void HWTokenLicenseCheckerForm_Load(object sender, EventArgs e)
        {

            toolStripStatusLabel1.Text = String.Empty;

            GetTargetOfEnviromentVariables();

            GetLMXLicenseData();
            AddIconsToMenuItems();
        }

        private void enviomentVarsStripMenuItem_Click(object sender, EventArgs e)
        {
            EnvironmentVariablesForm evForm = new EnvironmentVariablesForm() {
                Variables = APP_ENV_VARS,
                Targets = APP_ENV_VARS_TARGET
            };
            evForm.StartPosition = FormStartPosition.CenterParent;
            evForm.ShowDialog();
        }

    }
}
