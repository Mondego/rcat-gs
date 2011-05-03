using System;
using System.Net;

namespace RCAT.Connectors
{
    public abstract class DataConnector
    {
        public abstract void Connect();
        public abstract void SetPosition(string userName, Position pos, long timestamp);

        public abstract int GetCount();
        public abstract void RemoveUser(string userName);
        public abstract User GetUser(string userName);
        public abstract string[] GetAllUsersNames();
        public abstract User[] GetAllUsers();
        public ulong IPStringToulong(String userName)
        {
            String[] splitted = userName.Split(':');
            IPAddress myIp = IPAddress.Parse(splitted[0]);
            byte[] myBytes = myIp.GetAddressBytes();

            int tmpAddress = BitConverter.ToInt32(myBytes, 0);
            ulong intAddress = (ulong)tmpAddress;

            intAddress = (intAddress << 32);
            return intAddress + ulong.Parse(splitted[1]);
        }

        public String IPulongToString(ulong key)
        {
            ulong port = (key << 32) >> 32;
            int ip = (int)(key >> 32);

            string ipAddress = new IPAddress(BitConverter.GetBytes(ip)).ToString();

            return ipAddress + ":" + port.ToString();
        }

    }
}
