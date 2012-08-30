using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Collections;

using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace HWTokenLicenseChecker
{
    class LMX2SQLite
    {

        List<String> licensePathData = null;
        List<String> featureData = null;
        List<String> userData = null;

        private String[] _licenseData;
        private String _sqlitePath;

        private const String BORROW_EXPIRE_TIME = @"BORROW_EXPIRE_TIME";

        public String[] LicenseData { 
            get { return _licenseData; } 
            set { _licenseData = value;} 
        }
        public String SqlitePath { 
            get {return _sqlitePath; }
            set {_sqlitePath = value; }
        }

        private SQLiteConnection cnn;

        public LMX2SQLite()
        {

        }

        public void ReadXMLLicenseData()
        {
            String[] xmlNodes = {@"LICENSE_PATH",@"FEATURE","USER"};

            licensePathData =  new List<String> ();
            featureData = new List<String> ();
            userData = new List<String> ();

            int featureCounter = 0;
            int userCounter = 0;
            String xmlFile = Path.ChangeExtension(_sqlitePath, @"xml");
            String featureName = @"";
            int featureId = 0;

            XmlTextReader textReader = new XmlTextReader(xmlFile);
            
            while (textReader.Read())
            {
                if(!String.IsNullOrEmpty(textReader.Name))
                {
                    if (textReader.Name == @"LICENSE_PATH" && textReader.NodeType != XmlNodeType.EndElement)
                    {
                        String license_PathTypeAttribute = textReader["TYPE"].Trim();
                        String license_PathHostAttribute = textReader["HOST"].Trim();
                        String license_PathServer_VersionAttribute = textReader["SERVER_VERSION"].Trim();
                        String license_PathUptimeAttribute = textReader["UPTIME"].Trim();
                        //license_PathUptimeAttribute = license_PathUptimeAttribute.Replace(' ','=');

                        String[] hostArray = license_PathHostAttribute.Split(new Char[] {'@'});
                        String license_PathHostAttribute_Port = hostArray[0];
                        String license_PathHostAttribute_IP = hostArray[1];

                        String tmp = String.Format(@"{0},{1},{2},{3},{4}", 
                            license_PathServer_VersionAttribute, license_PathHostAttribute_IP,
                            license_PathHostAttribute_Port,license_PathTypeAttribute,
                            license_PathUptimeAttribute);
                        licensePathData.Add(tmp);
                        

                        //<LICENSE_PATH TYPE="xxxxxxx" HOST="####@###.###.###.###" SERVER_VERSION="#.##" 
                        // UPTIME="## day(s) ## hour(s) ## min(s) ## sec(s)">
                    }

                    if (textReader.Name == @"FEATURE"  && textReader.NodeType != XmlNodeType.EndElement)
                    {
                        ++featureCounter;
                        
                        String featureNameAttribute = textReader["NAME"].Trim();
                        String featureVersionAttribute = textReader["VERSION"].Trim();
                        String featureVendorAttribute = textReader["VENDOR"].Trim();
                        String featureStartAttribute = textReader["START"].Trim();
                        String featureEndAttribute = textReader["END"].Trim();
                        String featureUsedLicensesAttribute = textReader["USED_LICENSES"].Trim();
                        String featureTotalLicensesAttribute = textReader["TOTAL_LICENSES"].Trim();
                        String featureShareAttribute = textReader["SHARE"].Trim();
                        //featureShareAttribute = featureShareAttribute.Replace(',', '_');

                        int isPartner = 0;
                        featureName = featureNameAttribute;
                        if (featureNameAttribute.StartsWith("HWPartner"))
                        {
                            String tmp1 = featureNameAttribute.Substring(@"HWPartner".Length);

                            isPartner = int.Parse(tmp1);
                        }
                        ++featureId;

                        String tmp = String.Format(@"{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",
                            featureId, featureNameAttribute, featureVersionAttribute,
                            featureVendorAttribute, featureStartAttribute, featureEndAttribute, 
                            featureUsedLicensesAttribute, featureTotalLicensesAttribute,featureShareAttribute,isPartner);

                        featureData.Add(tmp);
                        //<FEATURE NAME="xxxxxxx" VERSION="##.#" VENDOR="xxxxxxx" START="yyyy-mm-dd"
                        // END="yyyy-mm-dd" USED_LICENSES="######" TOTAL_LICENSES="###" SHARE="xxxxxxx">
                    }
                    if (textReader.Name == @"USER" && textReader.NodeType != XmlNodeType.EndElement)
                    {
                        ++userCounter;
                        int isBorrow = 0;
                        // get the list of attributes for the <USER> item
                        List<String> attrList = new List<String>();
                        if (textReader.AttributeCount > 0)
                        {
                            while (textReader.MoveToNextAttribute())
                            {
                                attrList.Add(textReader.Name);
                            }   
                            textReader.MoveToElement();                            
                        }

                        //MessageBox.Show(String.Join(Environment.NewLine,attrList.ToArray()));

                        String userNameAttribute = textReader[attrList[0]].Trim().ToLower();
                        String userHostAttribute = textReader[attrList[1]].Trim().ToUpper();
                        String userIpAttribute = textReader[attrList[2]].Trim();
                        String userUsed_LicensesAttribute = textReader[attrList[3]].Trim();
                        String userLogin_TimeAttribute = textReader[attrList[4]].Trim();
                        String userCheckout_TimeAttribute = textReader[attrList[4]].Trim();
                        String userShare_CustomAttribute = textReader[attrList[5]].Trim();

                        if (attrList.Count == 7)
                        {
                            userCheckout_TimeAttribute = textReader[attrList[5]].Trim();
                            userShare_CustomAttribute = textReader[attrList[6]].Trim();                           
                        }

                        if(attrList.Contains(BORROW_EXPIRE_TIME))
                        {
                            isBorrow = 1;
                        }


                        String tmp = String.Format(@"{0},{1},{2},{3},""{4}"",""{5}"",{6},{7},{8}",
                            userNameAttribute, userHostAttribute,userIpAttribute, userUsed_LicensesAttribute,
                            userLogin_TimeAttribute, userCheckout_TimeAttribute, userShare_CustomAttribute, featureId,isBorrow);

                        //<USER NAME="xxxxxxx" HOST="xxxxxxx" IP="###.###.###.###" USED_LICENSES="####"
                        // LOGIN_TIME="yyyy-mm-dd hh:mm" CHECKOUT_TIME="yyyy-mm-dd hh:min" SHARE_CUSTOM="xxxxxxx:xxxxxxx"/>
                        //>
                        userData.Add(tmp);
                    }
                    
                }

            }

            textReader.Close();
            
        }

        public void CreateDatabase()
        {
            cnn = new SQLiteConnection("Data Source=" + _sqlitePath);
            cnn.Open();

            bool isSchemaCorrect = ValidateDatabaseSchema();
            DeleteContentsOfDatabase(@"");
        }

        public void ImportToDatabase()
        {

            String[] sqlStmtTextArray = {
                    @"INSERT INTO license_path (server_version, ip, port, type, uptime ) VALUES (?,?,?,?,?)",
                    @"INSERT INTO feature (feature_id, name, version ,vendor, start, end, used_licenses, total_licenses, share, isPartner ) VALUES (?,?,?,?,?,?,?,?,?,?)",
                    @"INSERT INTO user (name, host, ip, used_licenses, login_time, checkout_time, share_custom, feature_id, isBorrow ) VALUES (?,?,?,?,?,?,?,?,?)"};

            // insert to LICENSE_PATH table
            using (SQLiteTransaction sqlTransaction = cnn.BeginTransaction())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter typeParam = new SQLiteParameter();
                    SQLiteParameter ipParam = new SQLiteParameter();
                    SQLiteParameter portParam = new SQLiteParameter();
                    SQLiteParameter serverVersionParam = new SQLiteParameter();
                    SQLiteParameter uptimeParam = new SQLiteParameter(); // 

                    mycommand.CommandText = sqlStmtTextArray[0];

                    mycommand.Parameters.Add(typeParam);
                    mycommand.Parameters.Add(ipParam);
                    mycommand.Parameters.Add(portParam);
                    mycommand.Parameters.Add(serverVersionParam);
                    mycommand.Parameters.Add(uptimeParam);

                    // server_version, ip-, port-, type-, server_version, uptime

                    foreach (String licPath in licensePathData)
                    {
                        String[] tmpArray = licPath.Split(new Char[] { ',' });
                        typeParam.Value = tmpArray[3]; //
                        ipParam.Value = tmpArray[1];
                        portParam.Value = int.Parse(tmpArray[2]);
                        serverVersionParam.Value = Double.Parse(tmpArray[0]);
                        uptimeParam.Value = tmpArray[4];

                        mycommand.ExecuteNonQuery();


                    }
                }
                sqlTransaction.Commit();
            }

            // insert the featrues
            using (SQLiteTransaction sqlTransaction = cnn.BeginTransaction())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {

                    SQLiteParameter idParam = new SQLiteParameter();
                    SQLiteParameter nameParam = new SQLiteParameter();
                    SQLiteParameter versionParam = new SQLiteParameter();
                    SQLiteParameter vendorParam = new SQLiteParameter();
                    SQLiteParameter startParam = new SQLiteParameter();
                    SQLiteParameter endParam = new SQLiteParameter();
                    SQLiteParameter usedParam = new SQLiteParameter();
                    SQLiteParameter totalParam = new SQLiteParameter();
                    SQLiteParameter shareParam = new SQLiteParameter();
                    SQLiteParameter isPartnerParam = new SQLiteParameter();

                    mycommand.CommandText = sqlStmtTextArray[1];

                    mycommand.Parameters.Add(idParam);
                    mycommand.Parameters.Add(nameParam);
                    mycommand.Parameters.Add(versionParam);
                    mycommand.Parameters.Add(vendorParam);
                    mycommand.Parameters.Add(startParam);
                    mycommand.Parameters.Add(endParam);
                    mycommand.Parameters.Add(usedParam);
                    mycommand.Parameters.Add(totalParam);
                    mycommand.Parameters.Add(shareParam);
                    mycommand.Parameters.Add(isPartnerParam);

                    foreach (String featureStr in featureData)
                    {
                        String[] tmpArray = featureStr.Split(new Char[] { ';' });

                        idParam.Value = int.Parse(tmpArray[0]);
                        nameParam.Value = tmpArray[1];
                        versionParam.Value = double.Parse(tmpArray[2]);
                        vendorParam.Value = tmpArray[3];
                        startParam.Value = tmpArray[4];
                        endParam.Value = tmpArray[5];
                        usedParam.Value = int.Parse(tmpArray[6]);
                        totalParam.Value = int.Parse(tmpArray[7]);
                        shareParam.Value = tmpArray[8];
                        isPartnerParam.Value = int.Parse(tmpArray[9]);

                        mycommand.ExecuteNonQuery();
                    }
                }
                sqlTransaction.Commit();
            }


            // amount of tokens used by ...
            using (SQLiteTransaction sqlTransaction = cnn.BeginTransaction())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {

                    SQLiteParameter nameParam = new SQLiteParameter();
                    SQLiteParameter hostParam = new SQLiteParameter();
                    SQLiteParameter iPParam = new SQLiteParameter();
                    SQLiteParameter usedLicParam = new SQLiteParameter();
                    SQLiteParameter loginParam = new SQLiteParameter();
                    SQLiteParameter checkoutParam = new SQLiteParameter();
                    SQLiteParameter userShareParam = new SQLiteParameter();
                    SQLiteParameter featureIdParam = new SQLiteParameter();
                    SQLiteParameter isBorrowParam = new SQLiteParameter();

                    mycommand.CommandText = sqlStmtTextArray[2];

                    mycommand.Parameters.Add(nameParam);
                    mycommand.Parameters.Add(hostParam);
                    mycommand.Parameters.Add(iPParam);
                    mycommand.Parameters.Add(usedLicParam);
                    mycommand.Parameters.Add(loginParam);
                    mycommand.Parameters.Add(checkoutParam);
                    mycommand.Parameters.Add(userShareParam);
                    mycommand.Parameters.Add(featureIdParam);
                    mycommand.Parameters.Add(isBorrowParam);

                    foreach (String userStr in userData)
                    {
                        String[] tmpArray = userStr.Split(new Char[] { ',' });
                        nameParam.Value = tmpArray[0];
                        hostParam.Value = tmpArray[1];
                        iPParam.Value = tmpArray[2];
                        usedLicParam.Value = int.Parse(tmpArray[3]);
                        loginParam.Value = tmpArray[4];
                        checkoutParam.Value = tmpArray[5];
                        userShareParam.Value = tmpArray[6];
                        featureIdParam.Value = int.Parse(tmpArray[7]);
                        isBorrowParam.Value = int.Parse(tmpArray[8]);

                        mycommand.ExecuteNonQuery();
                    }


                }
                sqlTransaction.Commit();
            }

            licensePathData.Clear();
            featureData.Clear();
            userData.Clear();


        }

        public void CloseDatabase()
        {
            cnn.Close();

            licensePathData = null;
            featureData = null;
            userData = null;
        }

        private void DeleteContentsOfDatabase(String query)
        {
            // Get the tables of the database;
            const int _TABLE_INDEX_ = 2;
            DataTable dt = cnn.GetSchema(SQLiteMetaDataCollectionNames.Tables);
            List<String> sqlTables = new List<String>();
            foreach (DataRow dr in dt.Rows)
            {
                // 2 for tables....
                sqlTables.Add(dr.ItemArray[_TABLE_INDEX_].ToString());
            }

            String sqlStmt = @"";

            foreach (String sqlTable in sqlTables)
            {
                sqlStmt += String.Format(@"DELETE FROM {0};", sqlTable) + Environment.NewLine;
            }

            SQLiteCommand cmd = new SQLiteCommand(cnn);
            cmd.CommandText = sqlStmt;
            cmd.ExecuteNonQuery();
            cmd.Dispose();

        }

        private bool ValidateDatabaseSchema()
        {
            /* 
             * Maybe this code is to much for this application, but I 
             * want to learn how to validate a database schema.
             */

            // <--------------------------- DEFINE SCHEMA HERE --------------------->

            List<String> tablesList = new List<String>(new String[] { @"feature", @"license_path", @"user" });

            Hashtable featureHash = new Hashtable();
            featureHash.Add("feature_id", "INTEGER");
            featureHash.Add("name", "STRING");
            featureHash.Add("version", "REAL");
            featureHash.Add("vendor", "STRING");
            featureHash.Add("start", "STRING");
            featureHash.Add("end", "STRING");
            featureHash.Add("used_licenses", "INTEGER");
            featureHash.Add("total_licenses", "INTEGER");
            featureHash.Add("share", "STRING");
            featureHash.Add("isPartner", "INTEGER");

            Hashtable license_pathHash = new Hashtable();
            license_pathHash.Add("server_version", "STRING");
            license_pathHash.Add("ip", "STRING");
            license_pathHash.Add("port", "INTEGER");
            license_pathHash.Add("type", "STRING");
            license_pathHash.Add("uptime", "STRING");

            Hashtable userHash = new Hashtable();
            userHash.Add("name", "STRING");
            userHash.Add("host", "STRING");
            userHash.Add("ip", "STRING");
            userHash.Add("used_licenses", "INTEGER");
            userHash.Add("login_time", "STRING");
            userHash.Add("checkout_time", "STRING");
            userHash.Add("share_custom", "STRING");
            userHash.Add("feature_id", "INTEGER");
            userHash.Add("isBorrow", "INTEGER");
            // <--------------------------- END ------------------------------------>

            // validate tables.
            bool isValid = true;
            
            List<String> tablesInDatabase = new List<String>();
            List<String> wrongTablesList  = new List<String>();  //Tables that should be deleted
            List<String> missingTablesList = new List<String>(); //Tables that need to be created
            int numberOfTables = 0;

            DataTable dt = cnn.GetSchema(SQLiteMetaDataCollectionNames.Tables);
            foreach (DataRow dr in dt.Rows)
            {
                ++numberOfTables;
                String tableName = dr.ItemArray[2].ToString();
                tablesInDatabase.Add(tableName);
                if (!tablesList.Contains(tableName.ToLower().Trim()))
                {
                    wrongTablesList.Add(tableName);
                    isValid = false;
                }
            }

            foreach (String tableName in tablesList)
            {
                if (!tablesInDatabase.Contains(tableName.ToLower().Trim()))
                {
                    missingTablesList.Add(tableName);
                    isValid = false;
                }              
            }

            //
            // checkNames contains the table name plus
            // the columns and their type
            String checkNames = String.Empty;
            String columnName = String.Empty;
            String columnType = String.Empty;

            List<String> wrongColumnsList = new List<string>();

            foreach (String tableName in tablesList)
            {
                checkNames += tableName + "|";
                DataTable dc = cnn.GetSchema(SQLiteMetaDataCollectionNames.Columns, new String[] { "", "", tableName });

                foreach (DataRow dcr in dc.Rows)
                {
                    columnName = dcr.ItemArray[3].ToString().ToLower();
                    columnType = dcr.ItemArray[11].ToString().ToUpper();
                    String tmp1 = dcr.ItemArray[3].ToString() + ","+columnType + ":";
                    checkNames += tmp1;
                    
                }
                checkNames += Environment.NewLine;
            }

            columnType = String.Empty;
            columnName = String.Empty;
            columnType = String.Empty;

            // check names contains the table name plus
            // the columns and their type
            String[] schemaArray = checkNames.Split('\n');
            Hashtable hashtable = null;
            foreach (String schemaStr in schemaArray)
            {

                if (!schemaStr.Contains(':'))
                {
                    continue ;
                }

                if (schemaStr.Length != 0)
                {
                    String schema = schemaStr.Trim().Substring(0, schemaStr.Trim().Length - 1);
                    String[] schemaList = schemaStr.Split('|');
                    String tblName = schemaList[0].ToLower();
                    String[] columnNames = schema.Substring(tblName.Length+1).Split(':');
                    //MessageBox.Show(columnNames[2]);

                    if (tblName == tablesList[0].ToLower())
                    {
                        hashtable = featureHash;
                    }
                    else if (tblName == tablesList[1].ToLower())
                    {
                        hashtable = license_pathHash;
                    }
                    else if (tblName == tablesList[2].ToLower())
                    {
                        hashtable = userHash;
                    }

                    foreach (String columnItem in columnNames)
                    {
                        String[] splitted = columnItem.Split(',');
                        if (!hashtable.ContainsKey(splitted[0]))
                        {
                            wrongColumnsList.Add("Table:" + tblName + " Column:" + splitted[0] + " Type:" + splitted[1]);
                            wrongTablesList.Add(tblName); // since the name of the column does not match, 
                                                          // the table will be deleted
                            missingTablesList.Add(tblName);
                            isValid = false;
                            break;
                        }
                        if ((String)hashtable[splitted[0]] != splitted[1]) 
                        {
                            wrongColumnsList.Add("Table:" + tblName + " Column:" + splitted[0] + " Type:" + splitted[1]);
                            wrongTablesList.Add(tblName); // since the type of the column does not match, 
                                                            // the table will be deleted
                            missingTablesList.Add(tblName);
                            isValid = false;
                            break;
                        }
                    }
                }
            }

            String sqlQueryString = String.Empty;
            foreach (String tblName in wrongTablesList)
            {
                sqlQueryString += String.Format(@"DROP TABLE {0};",tblName) + Environment.NewLine;
            }

            foreach (String tblName in missingTablesList)
            {
                sqlQueryString += Environment.NewLine+  String.Format(@"CREATE TABLE {0} (", tblName);

                if (tblName == tablesList[0].ToLower())
                {
                    hashtable = featureHash;
                }
                else if (tblName == tablesList[1].ToLower())
                {
                    hashtable = license_pathHash;
                }
                else if (tblName == tablesList[2].ToLower())
                {
                    hashtable = userHash;
                }
                int entriesInHashTable = hashtable.Count;
                int counterHashKeys = 0;
                foreach (DictionaryEntry de in hashtable)
                {
                    sqlQueryString += Environment.NewLine + String.Format("\t{0} {1}", de.Key, de.Value);
                    ++counterHashKeys;
                    if (counterHashKeys < entriesInHashTable)
                    {
                        sqlQueryString += ",";   
                    }

                }
                sqlQueryString += Environment.NewLine + @");"; 
            }

            if (!String.IsNullOrEmpty(sqlQueryString))
            {
                SQLiteCommand cmd = new SQLiteCommand(cnn);
                cmd.CommandText = sqlQueryString;
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }

            return isValid;
        }
    }
}