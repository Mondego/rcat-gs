using System;
using System.Collections.Generic;
using Alchemy.Server.Classes;
using log4net;
using System.IO;
using RCAT;

namespace Proxy
{
    public class Proxy
    {
        // Delegates from Server to Clients
        public static delegate void SendPosToClients(User[] users);

        // Delegates from Clients to Server
        public static delegate void SendSetPosToServer(User user);
        public static delegate void SendClientDisconnectToServer(UserContext client);
        public static delegate void SendClientConnectToServer(UserContext client);

        public static SendPosToClients sendPositionToClients;
        public static SendSetPosToServer sendSetPositionToServer;
        public static SendClientDisconnectToServer sendClientDisconnectToServer;
        public static SendClientConnectToServer sendClientConnectToServer;

        protected static int serverPort = 82;

        /// <summary>
        /// Sets the name of the logger.
        /// </summary>
        /// <value>
        /// The name of the logger.
        /// </value>
        public static string LoggerName
        {
            set
            {
                Log = LogManager.GetLogger(value);
            }
        }

        /// <summary>
        /// Sets the log config file name.
        /// </summary>
        /// <value>
        /// The log config file name.
        /// </value>
        public static string LogConfigFile
        {
            set
            {
                log4net.Config.XmlConfigurator.Configure(new FileInfo(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static ILog Log = LogManager.GetLogger("Proxy.Log");

        /// <summary>
        /// Store the list of online users
        /// </summary>
        //protected static IList<User> OnlineUsers = new List<User>();
        protected Dictionary<String, UserContext> onlineUsers = new Dictionary<string, UserContext>();

        /// <summary>
        /// Store the list of servers
        /// </summary>
        /// <param name="args"></param>
        protected Dictionary<String, UserContext> onlineServers = new Dictionary<string, UserContext>();

        static void Main(string[] args)
        {
            ClientServer cServer = new ClientServer(Log);
            GameServer gServer = new GameServer(Log);
            LogConfigFile = "Proxy.config";
            LoggerName = "Proxy.Log";

            string Command = string.Empty;
            while (Command != "exit")
            {
                Command = Console.ReadLine();
            }

            cServer.Stop();
            gServer.Stop();
        }

        
    }
}
