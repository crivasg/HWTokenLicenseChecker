using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Collections;

using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace HWTokenLicenseChecker
{
    class LMX2SQLite
    {
        List<String> xmlElementNames = null;
        List<String> licensePathData = null;
        List<String> featureData = null;
        List<String> userData = null;

        private const String BORROW_EXPIRE_TIME = @"BORROW_EXPIRE_TIME";

        public String DatabasePath { private get; set ; }
        public String XMLFile { private get; set; }

        private SQLiteConnection cnn;

        public LMX2SQLite()
        {

        }

        public void Run()
        {
            CreateDatabase();

            GetElementsName();

            ReadXMLLicenseData();
            ImportToDatabase();       
        }


        private void GetElementsName()
        {
            XDocument xdoc = XDocument.Load(this.XMLFile);
            IEnumerable<String> childList = xdoc.Root.DescendantNodesAndSelf().OfType<XElement>()
                .Select(x => x.Name.LocalName).Distinct();

            xmlElementNames = new List<String>();

            foreach (String item in childList)
            {
                xmlElementNames.Add(item.ToUpper());
            }
 
        }

        private void ReadXMLLicenseData()
        {
            String[] xmlNodes = {@"LICENSE_PATH",@"FEATURE",@"USED", @"USER"};

            bool containsUsed = this.xmlElementNames.Contains(@"USED");

            licensePathData =  new List<String> ();
            featureData = new List<String> ();
            userData = new List<String> ();

            String featureName = String.Empty;
            int featureId = 0;

            // LINQ TO XML ...
            // get the license path...
            //<LICENSE_PATH TYPE="xxxxxxx" HOST="####@###.###.###.###" SERVER_VERSION="#.##" 
            // UPTIME="## day(s) ## hour(s) ## min(s) ## sec(s)">

            XElement xelement = XElement.Load(this.XMLFile);
            var licenseData = from nm in xelement.Elements(xmlNodes[0])
                       select nm;

            foreach (XElement xEle in licenseData)
            {
                String type = xEle.Attribute("TYPE").Value.ToString();
                String host = xEle.Attribute("HOST").Value.ToString();
                String version = xEle.Attribute("SERVER_VERSION").Value.ToString();
                String uptime = xEle.Attribute("UPTIME").Value.ToString();
                String[] tmpAray = host.Split('@');

                String tmp = String.Format(@"{0};{1};{2};{3};{4}", 
                                           version, tmpAray[1],tmpAray[0],type,uptime);
                licensePathData.Add(tmp);
                //MessageBox.Show(tmp);
            }
            // Get the feature data
            //<FEATURE NAME="xxxxxxx" VERSION="##.#" VENDOR="xxxxxxx" START="yyyy-mm-dd"
            // END="yyyy-mm-dd" USED_LICENSES="######" TOTAL_LICENSES="###" SHARE="xxxxxxx">

            Hashtable features = new Hashtable();
            var featureDataXML = from nm in licenseData.Elements(xmlNodes[1])
                              select nm;
            foreach (XElement xEle in featureDataXML)
            {
                ++featureId;
                String name = xEle.Attribute("NAME").Value.ToString();
                String version = xEle.Attribute("VERSION").Value.ToString();
                String vendor = xEle.Attribute("VENDOR").Value.ToString();
                String start = xEle.Attribute("START").Value.ToString();
                String end = xEle.Attribute("END").Value.ToString();
                String used = xEle.Attribute("USED_LICENSES").Value.ToString();
                String total = xEle.Attribute("TOTAL_LICENSES").Value.ToString();
                String share = xEle.Attribute("SHARE").Value.ToString();

                String tmp = String.Format(@"{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",
                    featureId, name, version,vendor, start, end,used, total, share, 0);

                featureData.Add(tmp);
                features.Add(name, featureId);
            }

            //MessageBox.Show(String.Join(Environment.NewLine,featureData.ToArray()));

            // Get the user data from the XML...
            //<USER NAME="xxxxxxx" HOST="xxxxxxx" IP="###.###.###.###" USED_LICENSES="####"
            // LOGIN_TIME="yyyy-mm-dd hh:mm" CHECKOUT_TIME="yyyy-mm-dd hh:min" SHARE_CUSTOM="xxxxxxx:xxxxxxx"/>
            //>

            IEnumerable<XElement> userDataXML;

            if (containsUsed)
            {
                IEnumerable<XElement> usedData = from nm in featureDataXML.Elements(xmlNodes[2])
                                                 select nm;
                userDataXML = from nm in usedData.Elements(xmlNodes[3])
                              select nm;
            }
            else 
            {
                userDataXML = from nm in featureDataXML.Elements(xmlNodes[3])
                              select nm;
            }

            foreach (XElement xEle in userDataXML)
            {
                int numOfAttributes = xEle.Attributes().Count();
                int isBorrow = numOfAttributes == 6 ? 1 : 0;
                String name = xEle.Attribute("NAME").Value.ToString().ToLower();
                String host = xEle.Attribute("HOST").Value.ToString().ToUpper();
                String ip = xEle.Attribute("IP").Value.ToString();
                String used = xEle.Attribute("USED_LICENSES").Value.ToString();
                String login = String.Empty;
                String checkout = String.Empty;

                if (isBorrow == 0)
                {
                    login = xEle.Attribute("LOGIN_TIME").Value.ToString();
                    checkout = xEle.Attribute("CHECKOUT_TIME").Value.ToString();
                }
                else if (isBorrow == 1)
                {
                    login = xEle.Attribute("BORROW_EXPIRE_TIME").Value.ToString();
                    checkout = xEle.Attribute("BORROW_EXPIRE_TIME").Value.ToString();
                }

                String share = xEle.Attribute("SHARE_CUSTOM").Value.ToString();

                String parentName = String.Empty;
                if (containsUsed)
                {
                    parentName = xEle.Parent.Parent.Attribute("NAME").Value.ToString();
                }
                else
                {
                    parentName = xEle.Parent.Attribute("NAME").Value.ToString();
                }

                int featId = (int)features[parentName];

                String tmp = String.Format(@"{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                    name, host, ip, used,login, checkout, share, featId, isBorrow);
                userData.Add(tmp);

            }
       
        }

        private void CreateDatabase()
        {
            cnn = new SQLiteConnection("Data Source=" + this.DatabasePath);
            cnn.Open();

            bool isSchemaCorrect = ValidateDatabaseSchema();
            DeleteContentsOfDatabase();
        }

        private void ImportToDatabase()
        {
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

                    mycommand.CommandText = Queries.InsertIntoLicensePath;

                    mycommand.Parameters.Add(serverVersionParam);
                    mycommand.Parameters.Add(ipParam);
                    mycommand.Parameters.Add(portParam);
                    mycommand.Parameters.Add(typeParam);
                    mycommand.Parameters.Add(uptimeParam);

                    // server_version, ip-, port-, type-, server_version, uptime

                    foreach (String licPath in licensePathData)
                    {
                        String[] tmpArray = licPath.Split(new Char[] { ';' });
                        typeParam.Value = tmpArray[3]; //
                        ipParam.Value = tmpArray[1];
                        portParam.Value = int.Parse(tmpArray[2]);
                        serverVersionParam.Value = tmpArray[0];
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

                    mycommand.CommandText = Queries.InsertIntoFeature;

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

                    mycommand.CommandText = Queries.InsertIntoUser;

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
                        String[] tmpArray = userStr.Split(new Char[] { ';' });
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

            // -- set isPartner = 1 where the feature name starts with 'HWPartner'
            SQLiteCommand cmd = new SQLiteCommand(cnn);
            cmd.CommandText = Queries.UpdateFeatureWithPartners;
            cmd.ExecuteNonQuery();
            cmd.Dispose();

        }

        public void CloseDatabase()
        {
            cnn.Close();

            licensePathData = null;
            featureData = null;
            userData = null;
        }

        private void DeleteContentsOfDatabase()
        {
            // Get the tables of the database;
            String sqlStmt = String.Empty;
            const int _TABLE_INDEX_ = 2;

            DataTable dt = cnn.GetSchema(SQLiteMetaDataCollectionNames.Tables);
            foreach (DataRow dr in dt.Rows)
            {
                // 2 for tables....
                String tableName = dr.ItemArray[_TABLE_INDEX_].ToString();
                if( !tableName.StartsWith(@"view_")) // when I create a view it always start with the word 'view_'
                {
                    sqlStmt += String.Format(@"DELETE FROM {0};", tableName) + Environment.NewLine;
                }   
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

            Hashtable featureHash = new Hashtable()
            {
                {"feature_id", "INTEGER"},
                {"name", "STRING"},
                {"version", "REAL"},
                {"vendor", "STRING"},
                {"start", "STRING"},
                {"end", "STRING"},
                {"used_licenses", "INTEGER"},
                {"total_licenses", "INTEGER"},
                {"share", "STRING"},
                {"isPartner", "INTEGER"}
            };

            Hashtable license_pathHash = new Hashtable()  
            {
                {"server_version", "STRING"},
                {"ip", "STRING"},
                {"port", "INTEGER"},
                {"type", "STRING"},
                {"uptime", "STRING"}
            };

            Hashtable userHash = new Hashtable()
            {
                {"name", "STRING"},
                {"host", "STRING"},
                {"ip", "STRING"},
                {"used_licenses", "INTEGER"},
                {"login_time", "STRING"},
                {"checkout_time", "STRING"},
                {"share_custom", "STRING"},
                {"feature_id", "INTEGER"},
                {"isBorrow", "INTEGER"}
            };
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