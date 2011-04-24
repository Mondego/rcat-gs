using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Net;


namespace RCAT
{
    static class MySqlConnector
    {

        public static String connStr = "server=opensim.ics.uci.edu;user=rcat;database=rcat;port=3306;password=isnotamused;";
        //public static MySqlConnection conn;
        
        public static void Connect()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                string sql = string.Format("DELETE FROM users");
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                // Perform databse operations
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            Console.WriteLine("Done.");
        }

        public static Boolean CheckConnection()
        {
            return true;
        }

        public static void SetPosition(string userName, Position pos)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                string sql = string.Format("INSERT INTO users (`name`, `top`, `left`) VALUES ({0}, {1}, {2}) ON DUPLICATE KEY UPDATE `top`={1},`left`={2}", name, pos.top.ToString(), pos.left.ToString());
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        public static int GetCount()
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


        public static void RemoveUser(string userName)
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

        public static User GetUser(string userName)
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
                    user.Name = userName;
                    user.pos.top = rdr.GetInt32(1);
                    user.pos.left = rdr.GetInt32(2);
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

        public static string[] GetAllUsersNames()
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
        
        public static User[] GetAllUsers()
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            User[] allUsers = null;
            try
            {
                conn.Open();
                int count = GetCount();
                int i = 0;
                allUsers = new User[count];

                string sql = "SELECT `name`,`top`,`left` FROM users";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read() && i <= count)
                {
                    ulong userName = rdr.GetUInt64(0);
                    allUsers[i] = new User();
                    allUsers[i].Name = IPulongToString(userName);

                    //allUsers[i].Name = rdr.GetString(0);
                    allUsers[i].pos.top = rdr.GetInt32(1);
                    allUsers[i].pos.left = rdr.GetInt32(2);
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

        public static ulong IPStringToulong(String userName)
        {
            String[] splitted = userName.Split(':');
            IPAddress myIp = IPAddress.Parse(splitted[0]);
            byte[] myBytes = myIp.GetAddressBytes();
            
            int tmpAddress = BitConverter.ToInt32(myBytes, 0);
            ulong intAddress = (ulong)tmpAddress;

            intAddress = (intAddress << 32);
            return intAddress + ulong.Parse(splitted[1]);
        }

        public static String IPulongToString(ulong key)
        {
            ulong port = (key << 32) >> 32;
            int ip = (int)(key >> 32);

            string ipAddress = new IPAddress(BitConverter.GetBytes(ip)).ToString();

            return ipAddress + ":" + port.ToString();
        }
    }
}
