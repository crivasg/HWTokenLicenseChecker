using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml;


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
            String xmlFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"licenses.xml");
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
                        license_PathUptimeAttribute = license_PathUptimeAttribute.Replace(' ','=');

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
                        featureShareAttribute = featureShareAttribute.Replace(',', ' ');

                        int isPartner = 0;
                        featureName = featureNameAttribute;
                        if (featureNameAttribute.StartsWith("HWPartner"))
                        {
                            String tmp1 = featureNameAttribute.Substring(@"HWPartner".Length);

                            isPartner = int.Parse(tmp1);
                        }
                        ++featureId;

                        String tmp = String.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},""{8}"",{9}",
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
                        String userNameAttribute = textReader["NAME"].Trim();
                        String userHostAttribute = textReader["HOST"].Trim();
                        String userIpAttribute = textReader["IP"].Trim();
                        String userUsed_LicensesAttribute = textReader["USED_LICENSES"].Trim();
                        String userLogin_TimeAttribute = textReader["LOGIN_TIME"].Trim();
                        String userCheckout_TimeAttribute = textReader["CHECKOUT_TIME"].Trim();
                        String userShare_CustomAttribute = textReader["SHARE_CUSTOM"].Trim();

                        String tmp = String.Format(@"{0},{1},{2},{3},""{4}"",""{5}"",{6},{7}",
                            userNameAttribute, userHostAttribute,userIpAttribute, userUsed_LicensesAttribute,
                            userLogin_TimeAttribute, userCheckout_TimeAttribute, userShare_CustomAttribute, featureId);

                        //<USER NAME="xxxxxxx" HOST="xxxxxxx" IP="###.###.###.###" USED_LICENSES="####"
                        // LOGIN_TIME="yyyy-mm-dd hh:mm" CHECKOUT_TIME="yyyy-mm-dd hh:min" SHARE_CUSTOM="xxxxxxx:xxxxxxx"/>
                        //>
                        userData.Add(tmp);
                    }
                    
                }

            }
            
        }

        public void CreateDatabase()
        {
            cnn = new SQLiteConnection("Data Source=" + _sqlitePath);
            cnn.Open();

            String sqlStmt = @"CREATE TABLE license_path (server_version STRING,ip STRING,port INTEGER,type STRING,uptime STRING);";
            sqlStmt += Environment.NewLine + Environment.NewLine;
            //<LICENSE_PATH TYPE="xxxxxxx" HOST="####@###.###.###.###" SERVER_VERSION="#.##" 
            // UPTIME="## day(s) ## hour(s) ## min(s) ## sec(s)">

            sqlStmt += @"CREATE TABLE feature (feature_id INTEGER, name STRING,version REAL,vendor STRING,start STRING,end STRING,used_licenses INTEGER,total_licenses INTEGER,share STRING,isPartner INTEGER);";
            sqlStmt += Environment.NewLine + Environment.NewLine;
            //<FEATURE NAME="xxxxxxx" VERSION="##.#" VENDOR="xxxxxxx" START="yyyy-mm-dd"
            // END="yyyy-mm-dd" USED_LICENSES="######" TOTAL_LICENSES="###" SHARE="xxxxxxx">

            // ::TODO:: prepare the database for when licenses are borrowed
            sqlStmt += @"CREATE TABLE user (name STRING, host STRING, ip STRING, used_licenses INTEGER, login_time STRING, checkout_time STRING,share_custom STRING, feature_id INTEGER);";
            sqlStmt += Environment.NewLine + Environment.NewLine;

            //<USER NAME="xxxxxxx" HOST="xxxxxxx" IP="###.###.###.###" USED_LICENSES="####"
            // LOGIN_TIME="yyyy-mm-dd hh:mm" CHECKOUT_TIME="yyyy-mm-dd hh:min" SHARE_CUSTOM="xxxxxxx:xxxxxxx"/>
            //>

            SQLiteCommand cmd = new SQLiteCommand(cnn);
            cmd.CommandText = sqlStmt;
            cmd.ExecuteNonQuery();
            cmd.Dispose();


            //CheckDatabaseSchema();
        }

        public void ImportToDatabase()
        {

            String[] sqlStmtTextArray = {
                    @"INSERT INTO license_path (server_version, ip, port, type, uptime ) VALUES (?,?,?,?,?)",
                    @"INSERT INTO feature (feature_id, name, version ,vendor, start, end, used_licenses, total_licenses, share, isPartner ) VALUES (?,?,?,?,?,?,?,?,?,?)",
                    @"INSERT INTO user (name, host, ip, used_licenses, login_time, checkout_time, share_custom, feature_id ) VALUES (?,?,?,?,?,?,?,?)"};

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
                        String[] tmpArray = featureStr.Split(new Char[] { ',' });

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

                    mycommand.CommandText = sqlStmtTextArray[2];

                    mycommand.Parameters.Add(nameParam);
                    mycommand.Parameters.Add(hostParam);
                    mycommand.Parameters.Add(iPParam);
                    mycommand.Parameters.Add(usedLicParam);
                    mycommand.Parameters.Add(loginParam);
                    mycommand.Parameters.Add(checkoutParam);
                    mycommand.Parameters.Add(userShareParam);
                    mycommand.Parameters.Add(featureIdParam);

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

        private void CheckDatabaseSchema()
        {

        }
    }
}
