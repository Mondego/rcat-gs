using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Net;
using RCAT.Connectors;

namespace RCAT
{
    class MySqlConnector : DataConnector
    {

        public String connStr = "server="+Properties.Settings.Default.mysql_server+";user="+Properties.Settings.Default.mysql_user+";database="+Properties.Settings.Default.mysql_database+";port="+Properties.Settings.Default.mysql_port+";password="+Properties.Settings.Default.mysql_pass+";";
        public String newTable = @"CREATE TABLE `users` (
  `name` bigint(20) unsigned NOT NULL DEFAULT '0',
  `top` int(11) NOT NULL DEFAULT '0',
  `left` int(11) NOT NULL DEFAULT '0',
  `z` int(11) NOT NULL DEFAULT '0',
  `timestamp` bigint(20) NOT NULL DEFAULT '0',
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8";

        public override void Connect()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                string sql = string.Format("DROP TABLE users");
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
        }

        public Boolean CheckConnection()
        {
            return true;
        }

        public override void SetPosition(string userName, Position pos, long newstamp)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                // MySQL silently drops insert/update query if no new changes are made to the rows
                string sql = string.Format("INSERT INTO users (`name`, `top`, `left`,`timestamp`,`z`) VALUES ({0}, {1}, {2}, {3}, {4}) "+
                "ON DUPLICATE KEY UPDATE `top`=IF(timestamp <= VALUES(timestamp), {1}, `top`)," +
                "`left`=IF(timestamp < VALUES(timestamp), {2}, `left`),`timestamp`=IF(timestamp <= VALUES(timestamp), {3},`timestamp`),`z`=IF(timestamp <= VALUES(timestamp), {4}, `z`)", name, pos.t.ToString(), pos.l.ToString(), newstamp.ToString(), pos.z.ToString());
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        public override int GetCount()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            object result = null;
            try
            {
                conn.Open();

                string sql = "SELECT COUNT(*) FROM users";
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
                string sql = string.Format("DELETE FROM users WHERE name = {0}", name);
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
            MySqlConnection conn = new MySqlConnection(connStr);
            User user = null;
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                string sql = string.Format("SELECT * FROM users WHERE name = {0}", name);
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

        public override string[] GetAllUsersNames()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            string[] allUsers = null;
            try
            {
                conn.Open();
                int count = GetCount();
                int i = 0;
                allUsers = new string[count];

                string sql = "SELECT `name` FROM users";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read() && i <= count)
                {
                    ulong userName = rdr.GetUInt64(0);
                    allUsers[i] = IPulongToString(userName);
                    i++;
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return allUsers;
        }
        
        public override User[] GetAllUsers()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            User[] allUsers = null;
            try
            {
                conn.Open();
                int count = GetCount();
                int i = 0;
                allUsers = new User[count];

                string sql = "SELECT `name`,`top`,`left`,`z` FROM users";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read() && i <= count)
                {
                    ulong userName = rdr.GetUInt64(0);
                    allUsers[i] = new User();
                    allUsers[i].n = IPulongToString(userName);

                    //allUsers[i].Name = rdr.GetString(0);
                    allUsers[i].p.t = rdr.GetInt32(1);
                    allUsers[i].p.l = rdr.GetInt32(2);
                    allUsers[i].p.z = rdr.GetInt32(3);
                    i++;
                }
                rdr.Close();                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return allUsers;
        }

        
    }
}
