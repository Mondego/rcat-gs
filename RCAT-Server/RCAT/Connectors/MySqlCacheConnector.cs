using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using log4net;
using MySql.Data;
using MySql.Data.MySqlClient;
using RCAT.Connectors;
using System.Threading;
using System.Timers;
using System.Collections;

namespace RCAT
{
    class MySqlCacheConnector : DataConnector
    {

        public static String userTableName = "users";
        public static String connStr = "server=" + Properties.Settings.Default.mysql_server + ";user=" + Properties.Settings.Default.mysql_user + ";database=" + Properties.Settings.Default.mysql_database + ";port=" + Properties.Settings.Default.mysql_port + ";password=" + Properties.Settings.Default.mysql_pass + ";pooling=true;Min Pool Size=50;Max Pool Size=200;Connection Lifetime=0;";
        public String newTable = @"CREATE TABLE `"+ userTableName + @"` (
  `name` bigint(20) unsigned NOT NULL DEFAULT '0',
  `top` int(11) NOT NULL DEFAULT '0',
  `left` int(11) NOT NULL DEFAULT '0',
  `z` int(11) NOT NULL DEFAULT '0',
  `timestamp` bigint(20) NOT NULL DEFAULT '0',
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8";
        protected static ILog Log = null;
        protected static string[] allUserNames;
        protected static User[] allUsers;
        protected static Hashtable users = new Hashtable();
        private static Object userlock = new Object();
        private static System.Timers.Timer aTimer;
        private static int refreshing = 0;
        private static EventWaitHandle userRefresh = new EventWaitHandle(true, EventResetMode.ManualReset);

        public MySqlCacheConnector(ILog log)
        {
            Log = log;
            aTimer = new System.Timers.Timer(2000);
            aTimer.AutoReset = false;
            aTimer.Elapsed += new ElapsedEventHandler(RefreshAllUsers);
            aTimer.Enabled = true;
        }

        public override void Connect()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                string sql = string.Format("DROP TABLE IF EXISTS " + userTableName);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                // Perform databse operations
                MySqlCommand cmd2 = new MySqlCommand(newTable, conn);
                cmd2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            Console.WriteLine("Done.");
            Log.Info("MySql Connected");
        }

        public void RefreshUsers()
        {
            aTimer.Enabled = false;
            RefreshAllUsers(null, null);
            aTimer.Enabled = true;
        }

        public override void SetPosition(string userName, Position pos, long newstamp)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                // MySQL silently drops insert/update query if no new changes are made to the rows
                string sql = string.Format("INSERT INTO " + userTableName + @" (`name`, `top`, `left`,`timestamp`,`z`) VALUES ({0}, {1}, {2}, {3}, {4}) " +
                "ON DUPLICATE KEY UPDATE `top`=IF(timestamp <= VALUES(timestamp), {1}, `top`)," +
                "`left`=IF(timestamp < VALUES(timestamp), {2}, `left`),`timestamp`=IF(timestamp <= VALUES(timestamp), {3},`timestamp`),`z`=IF(timestamp <= VALUES(timestamp), {4}, `z`)", name, pos.t.ToString(), pos.l.ToString(), newstamp.ToString(), pos.z.ToString());
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                /*
                if (!users.Contains(name))
                {
                    User newUser = new User();
                    newUser.p = pos;
                    newUser.n = userName;
                    users[name] = newUser;
                    
                }
                 */
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        public override int GetCount()
        {
            return GetCountStatic();
        }

        public static int GetCountStatic()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            object result = null;
            try
            {
                conn.Open();

                string sql = "SELECT COUNT(*) FROM " + userTableName;
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                result = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();

            if (result != null)
                return Convert.ToInt32(result);
            else return -1;
        }


        public override void RemoveUser(string userName)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                string sql = string.Format("DELETE FROM " + userTableName + @" WHERE name = {0}", name);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();

        }

        public override User GetUser(string userName)
        {
            ulong name = IPStringToulong(userName);
            if (users.Contains(name))
                return (User)users[name];
            else
                return GetUserFromDatabase(userName);
        }

        public User GetUserFromDatabase(string userName)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            User user = null;
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                string sql = string.Format("SELECT * FROM " + userTableName + @" WHERE name = {0}", name);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                user = new User();

                if (rdr.Read())
                {
                    user.n = userName;
                    user.p.t = rdr.GetInt32(1);
                    user.p.l = rdr.GetInt32(2);
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return user;
        }

        public static void RefreshAllUsers(object source, ElapsedEventArgs e)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                int count = GetCountStatic();
                conn.Open();
                int i = 0;
                string[] tmpUserNames;
                User[] tmpUsers;
                tmpUsers = new User[count];
                tmpUserNames = new string[count];

                string sql = "SELECT `name`,`top`,`left`,`z` FROM " + userTableName;
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read() && i <= count)
                {
                    ulong userName = rdr.GetUInt64(0);
                    tmpUsers[i] = new User();
                    tmpUsers[i].n = IPulongToString(userName);
                    tmpUserNames[i] = tmpUsers[i].n;
                    users[userName] = tmpUsers[i];

                    tmpUsers[i].p.t = rdr.GetInt32(1);
                    tmpUsers[i].p.l = rdr.GetInt32(2);
                    tmpUsers[i].p.z = rdr.GetInt32(3);
                    i++;
                }
                rdr.Close();
                Interlocked.Increment(ref refreshing);
                allUsers = tmpUsers;
                allUserNames = tmpUserNames;
                aTimer.Enabled = true;
                Interlocked.Decrement(ref refreshing);
                userRefresh.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                aTimer.Enabled = true;
            }
            conn.Close();
            
        }

        public override string[] GetAllUsersNames()
        {
            if (refreshing != 0)
                userRefresh.WaitOne();
            return allUserNames;
        }

        public override User[] GetAllUsers()
        {
            if (refreshing != 0)
                userRefresh.WaitOne();
            return allUsers;
        }
    }
}
