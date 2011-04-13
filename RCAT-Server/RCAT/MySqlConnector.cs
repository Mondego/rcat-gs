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

        public static MySqlConnection conn;

        public static void Connect()
        {
            string connStr = "server=opensim.ics.uci.edu;user=rcat;database=rcat;port=3306;password=isnotamused;";
            conn = new MySqlConnection(connStr);
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
            try
            {
                conn.Open();

                string sql = "SELECT COUNT(*) FROM users";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                object result = cmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return -1;
        }


        public static void RemoveUser(string userName)
        {
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
            try
            {
                conn.Open();
                ulong name = IPStringToulong(userName);
                string sql = string.Format("SELECT * FROM users WHERE name = {0}", name);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                User user = new User();

                if (rdr.Read())
                {
                    user.Name = userName;
                    user.pos.top = rdr.GetInt32(1);
                    user.pos.left = rdr.GetInt32(2);
                }
                
                rdr.Close();
                conn.Close();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return null;
        }

        public static User[] GetAllUsers()
        {
            try
            {
                int count = GetCount();
                int i = 0;
                User[] allUsers = new User[count];

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
                conn.Close();
                return allUsers;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            return null;
        }

        public static ulong IPStringToulong(String userName)
        {
            String[] splitted = userName.Split(':');
            IPAddress myIp = IPAddress.Parse(splitted[0]);
            byte[] myBytes = myIp.GetAddressBytes();
            
            Console.WriteLine("Size of myBytes: " + myBytes.Length.ToString());
            Console.WriteLine(myBytes.ToString());

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
