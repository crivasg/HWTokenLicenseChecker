using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HWTokenLicenseChecker
{
    public static class Queries
    {
        public readonly static String FillDataGridView =
    @"SELECT LOWER(name) AS Username, host AS Hostname, MAX(used_licenses) AS Tokens, 
    share_custom AS 'Custom String','' AS Type, login_time AS Date, feature_id as 'Feature Id' 
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

        public readonly static String GatherHyperworksTokensData = @"SELECT used_licenses,total_licenses,end FROM feature WHERE name = 'HyperWorks';";

        public readonly static String GatherLicenseServerData = @"SELECT port,ip,uptime FROM license_path;";

        public readonly static String GetMinMaxPartnerFeaturesIds = @"SELECT MIN(feature_id),MAX(feature_id) FROM feature WHERE name LIKE '%Partner%';";

        public readonly static String UsersWithLockedTokens = @"SELECT DISTINCT  user.name||':'||user.host||':'||user.used_licenses  
                    FROM user JOIN feature USING (feature_id) 
                    WHERE user.share_custom LIKE '%:%:%' AND feature.name = 'HyperWorks';";

        public readonly static String GatherTokenUsagePerUsernameAndHostname = @"SELECT DISTINCT feature.name,user.login_time,user.host||'/'||user.ip 
                FROM user JOIN feature USING (feature_id) 
                WHERE user.name = '{0}' AND user.host = '{1}' AND user.feature_id IN ( 
                    SELECT DISTINCT feature_id FROM feature WHERE isPartner = {2}
                );";

        public readonly static String InsertIntoLicensePath = @"INSERT INTO license_path (server_version, ip, port, type, uptime ) VALUES (?,?,?,?,?)";

        public readonly static String InsertIntoFeature = @"INSERT INTO feature (feature_id, name, version ,vendor, start, end, used_licenses, total_licenses, share, isPartner ) VALUES (?,?,?,?,?,?,?,?,?,?)";

        public readonly static String InsertIntoUser = @"INSERT INTO user (name, host, ip, used_licenses, login_time, checkout_time, share_custom, feature_id, isBorrow ) VALUES (?,?,?,?,?,?,?,?,?)";

        public readonly static String UpdateFeatureWithPartners = @"UPDATE feature SET isPartner = 1 WHERE name LIKE 'HWPartner%';";

    }
}
