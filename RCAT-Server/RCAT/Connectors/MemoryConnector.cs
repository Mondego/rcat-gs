using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCAT.Connectors
{
    class MemoryConnector : DataConnector
    {
        protected Dictionary<string, User> onlineUsers = new Dictionary<string, User>();

        public override void Connect()
        {
            Console.WriteLine("Memory Data Connector Started!");
        }

        public override void SetPosition(string userName, Position pos, long timestamp)
        {
            if (onlineUsers.ContainsKey(userName))
            {
                User update = onlineUsers[userName];
                update.pos = pos;
                // Do I need this last line? 
                onlineUsers[userName] = update;
            }
            else
            {
                User newUser = new User();
                newUser.pos = pos;
                newUser.Name = userName;
                onlineUsers.Add(userName, newUser);
            }
        }

        public override int GetCount()
        {
            return onlineUsers.Count;
        }

        public override void RemoveUser(string userName)
        {
            if (onlineUsers.ContainsKey(userName))
                onlineUsers.Remove(userName);
        }

        public override User GetUser(string userName)
        {
            return onlineUsers[userName];
        }

        public override string[] GetAllUsersNames()
        {
            return onlineUsers.Keys.ToArray<string>();
        }

        public override User[] GetAllUsers()
        {
            return onlineUsers.Values.ToArray<User>();
        }
    }
}
