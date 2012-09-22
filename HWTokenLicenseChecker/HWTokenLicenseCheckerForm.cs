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

namespace HWTokenLicenseChecker
{
    public partial class HWTokenLicenseCheckerForm : Form
    {
        private String databasePath = String.Empty;
        private String xmlFile = String.Empty;
        private String folder = String.Empty;
        private String lmxconfigtool = String.Empty;

        private List<String> usersWithProblems = new List<String>();

        private const String GITHUB_REPO_URL = @"https://github.com/crivasg/HWTokenLicenseChecker";

        private int minHWPAFeatureId = -1;
        private int maxHWPAFeatureId = -2;

        private int selectedRow = -1;
        private bool isRunning = false;

        public HWTokenLicenseCheckerForm()
        {

            this.UpgradeSettings();
            InitializeComponent();

            GetLMXLicenseData();
        }

        private void CheckApplicationStatus(Status status)
        {
            String msg = String.Empty;

            switch (status)
            {
                case Status.ServerOffline:
                    msg = @"Server may be offline.";
                    break;
                case Status.LicenseServerOffline:
                    msg = @"LMX license server not running on server.";
                    break;
                case Status.LmxExecuteError:
                    msg = @"Error while executing the LMX end user utilities.";
                    break;
                case Status.EndUserUtilityNotFound:
                    msg = @"LMX end user utility not found.";
                    break;
                case Status.ConfigToolNotFound:
                    msg = @"LMX configuration tool not found.";
                    break;
                case Status.LmxToolsNotFound:
                    msg = @"LMX tools not found.";
                    break;
                case Status.FailedToFixXMLFile:
                    msg = @"Failed to fix the XML file.";
                    break;
            }
            msg += @" Contact your network administrator.";

            MessageBox.Show(msg, @"LMX Error");

            toolStripStatusLabel1.Text = msg;

        }

        private void GetLMXLicenseData()
        {

            isRunning = true;

            Setup setup = new Setup();
            setup.CheckAndCreateAppData();
            setup.RemoveTempFiles();
            databasePath = setup.DatabasePath;
            folder = setup.AppDataPath;
            xmlFile = setup.XMLPath;

            //UpdateLastPosition();

            lmxendutil lmx = new lmxendutil() { 
                XMLFile = xmlFile
            };

            lmx.ExecuteLMX();
            lmxconfigtool = lmx.LMXConfigTool;

            Status status = lmx.AppStatus;

            if (status != Status.OK)
            {
                CheckApplicationStatus(status);
                return;
            }
           
            //Clipboard.SetText(sqlPath);

            LMX2SQLite lmx2Sqlite = new LMX2SQLite {
                DatabasePath = databasePath,
                XMLFile = xmlFile
            };
            lmx2Sqlite.CreateDatabase();
            lmx2Sqlite.ReadXMLLicenseData();
            lmx2Sqlite.ImportToDatabase();

            lmx2Sqlite.CloseDatabase();
            LoadToDataGridView();

            // checks if there is user with locked tokens.
            CheckForLockedTokens();

            isRunning = false;

  
        }

        private void LoadToDataGridView()
        {

            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            String sqlQuery = @"SELECT LOWER(name) AS Username, host AS Hostname, MAX(used_licenses) AS Tokens, share_custom AS 'Custom String', feature_id as 'Feature Id' FROM user WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner = 0) AND isBorrow = 0 GROUP BY Username, Hostname UNION SELECT LOWER(name), host, MAX(used_licenses)||'-HWPA', share_custom, feature_id FROM user WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner != 0) AND isBorrow = 0 GROUP BY name, host, feature_id  UNION SELECT LOWER(name), host, MAX(used_licenses)||'-BRRW', share_custom, feature_id FROM user WHERE feature_id IN ( SELECT feature_id FROM feature WHERE isPartner = 0) AND isBorrow = 1 GROUP BY name, host ORDER BY Tokens DESC, Username ASC, Hostname ASC;";

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


            toolStripStatusLabel1.Text = String.Format(@"{0} User(s). Uptime: {1}. {2} of {3} license(s) used. Expiration Date: {4}", 
                numberOfUsers, uptime,used_licenses, total_licenses, end_date);

           
            this.Text += String.Format(@" {0}@{1} ",port,ip);
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

            String user = String.Empty;
            String host = String.Empty;
            int feature_id = -1;
            int tokens = -1;
            DataGridViewRow currentRow = dataGridView.CurrentRow;
            selectedRow = currentRow.Index;

            try
            {
                user = Convert.ToString(currentRow.Cells[0].Value).ToLower();
                host = Convert.ToString(currentRow.Cells[1].Value).ToUpper();
                String tmpTokens = Convert.ToString(currentRow.Cells[2].Value);
                borrowHWPATextBox.Text = String.Empty;
                borrowHWPATextBox.BackColor = Color.FromKnownColor(KnownColor.Window);
                if (tmpTokens.Contains(@"HWPA") || tmpTokens.Contains(@"BRRW"))
                {
                   
                    borrowHWPATextBox.BackColor = Color.MistyRose;

                    String[] tmpTokensArray = tmpTokens.Split(new Char[] {'-'});
                    tokens = int.Parse(tmpTokensArray[0]);

                    if (tmpTokens.Contains(@"HWPA"))
                    {
                        borrowHWPATextBox.Text = @"HWPA";
                    }
                    if (tmpTokens.Contains(@"BRRW"))
                    {
                        borrowHWPATextBox.Text = @"BORROW";
                    }
                }
                else
                {
                    tokens = Convert.ToInt32(currentRow.Cells[2].Value);;
                }

                
                feature_id = Convert.ToInt32(currentRow.Cells[4].Value);
            }
            catch { return; }

            userTextBox.Text = user;
            tokensTextBox.Text = tokens.ToString();
            //borrowHWPATextBox.Text = String.Empty;

            if (feature_id >= minHWPAFeatureId && feature_id <= minHWPAFeatureId)
            {
                // feature is HWPartner
                //borrowHWPATextBox.Text = @"HWPA";
                ProcessTokens(user, host, feature_id, @"SELECT DISTINCT feature_id FROM feature WHERE isPartner != 0");
            }
            else
            {
                // feature normal
                ProcessTokens(user, host, feature_id, @"SELECT DISTINCT feature_id FROM feature WHERE isPartner = 0");
            }

            //MessageBox.Show(@"Hello!");
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {

            GetUserTokenInfo();
            
        }

        private void ProcessTokens(String user, String host, int feature_id, String featureQuery)
        {
            SQLiteConnection cnn = new SQLiteConnection("Data Source=" + databasePath);
            cnn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cnn);

            // get the features used
            String sqlQuery = String.Format(@"SELECT DISTINCT feature.name,user.login_time,user.host||'/'||user.ip FROM user JOIN feature USING (feature_id) WHERE user.name = ""{0}"" AND user.host = ""{1}"" AND user.feature_id IN ({2});", user, host, featureQuery);

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
            String sqlQuery = @"SELECT user.share_custom FROM user JOIN feature USING (feature_id) WHERE feature.name = 'HyperWorks';";

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
                    String[] tmpArray = share_custom.Split(':');

                    String userString = String.Format(@"{0}:{1}",
                    tmpArray[0], tmpArray[1]);

                    if (tmpArray.Length == 3 && !usersWithProblems.Contains(userString))
                    {
                        usersWithProblems.Add(userString);
                    }
                }
                reader.Close();
            }
            cmd.Dispose();
            cnn.Close();     

        }

        private void ApplyStyleToCells()
        {
            // http://blog.csharphelper.com/2010/05/31/calculate-a-datagridview-columns-value-and-highlight-specific-values-in-c.asp

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                dataGridView.ReadOnly = false;
                String userData = (String)row.Cells["Username"].Value + @":" +
                    (String)row.Cells["Hostname"].Value;

                if (usersWithProblems.Contains(userData))
                {


                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.BackColor = Color.MistyRose;
                    }
                }
                this.Update();
                dataGridView.ReadOnly = true;
            }       
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

    }
}
