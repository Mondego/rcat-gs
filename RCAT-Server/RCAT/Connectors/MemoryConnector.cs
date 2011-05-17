using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCAT.Connectors
{
    class MemoryConnector : DataConnector
    {
        class DataStore
        {
            public User user;
            public long timestamp;
        }
        private Dictionary<string, DataStore> onlineUsers = new Dictionary<string, DataStore>();
        private Dictionary<string, User> userList = new Dictionary<string, User>();

        public override void Connect()
        {
            Console.WriteLine("Memory Data Connector Started!");
        }

        public override void SetPosition(string userName, Position pos, long timestamp)
        {
            if (onlineUsers.ContainsKey(userName))
            {
                DataStore update = onlineUsers[userName];
                if (timestamp >= update.timestamp)
                {
                    update.user.p = pos;
                    // Do I need this last line? 
                    onlineUsers[userName] = update;
                }
            }
            else
            {
                DataStore ds = new DataStore();
                User newUser = new User();
                newUser.p = pos;
                newUser.n = userName;
                ds.user = newUser;
                ds.timestamp = timestamp;
                onlineUsers.Add(userName, ds);
                userList.Add(userName, newUser);
            }
        }

        public override int GetCount()
        {
            return onlineUsers.Count;
        }

        public override void RemoveUser(string userName)
        {
            if (onlineUsers.ContainsKey(userName))
            {
                onlineUsers.Remove(userName);
                userList.Remove(userName);
            }
        }

        public override User GetUser(string userName)
        {
            return userList[userName];
        }

        public override string[] GetAllUsersNames()
        {
            return onlineUsers.Keys.ToArray<string>();
        }

        public override User[] GetAllUsers()
        {
            return userList.Values.ToArray<User>();
        }
    }
}

