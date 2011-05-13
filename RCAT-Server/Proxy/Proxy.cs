using System;
using System.Collections.Generic;
using System.Threading;
using Alchemy.Server.Classes;
using log4net;
using System.IO;
using RCAT;

namespace Proxy
{
    public class Proxy
    {
        /// <summary>
        /// Disk lock for flushing data to file
        /// </summary>
        public static SemaphoreSlim DiskLock = new SemaphoreSlim(1);

        //
        // Delegates from Server to Clients
        //

        /// <summary>
        /// Sends data from server to specified clients. 
        /// </summary>
        /// <param name="users"></param>
        public delegate void BroadcastToClients(ClientMessage broadcast);

        //
        // Delegates from Clients to Server
        //

        /// <summary>
        /// Sends a new client position to update the server
        /// </summary>
        /// <param name="user"></param>
        public delegate void SendSetPosToServer(User user, long timestamp);

        /// <summary>
        /// Informs server of a client disconnection
        /// </summary>
        /// <param name="client"></param>
        public delegate void SendClientDisconnectToServer(UserContext client);

        /// <summary>
        /// Informs server of a client connection
        /// </summary>
        /// <param name="client"></param>
        public delegate void SendClientConnectToServer(UserContext client);

        // Clientserver implements this method
        public static BroadcastToClients broadcastToClients;

        // Gameserver implements these methods
        public static SendSetPosToServer sendSetPositionToServer;
        public static SendClientDisconnectToServer sendClientDisconnectToServer;
        public static SendClientConnectToServer sendClientConnectToServer;

        //protected static int serverPort = 82;

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

        public static long startTime = DateTime.Now.Ticks;

        /// <summary>
        /// 
        /// </summary>
        public static ILog Log = LogManager.GetLogger("Proxy.Log");

        /// <summary>
        /// Store the list of online users
        /// </summary>
        //protected static IList<User> OnlineUsers = new List<User>();
        public static Dictionary<String, UserContext> onlineUsers = new Dictionary<string, UserContext>();

        /// <summary>
        /// Store the list of servers
        /// </summary>
        /// <param name="args"></param>
        public static Dictionary<String, UserContext> onlineServers = new Dictionary<string, UserContext>();

        static void Main(string[] args)
        {
            SetUpLogging();
            
            ClientServer cServer = new ClientServer(Log); // handles communication with clients through websocket protocol
            GameServer gServer = new GameServer(Log); //handles communication with servants through TCP
            Console.WriteLine("Proxy up and running. Type 'exit' to terminate it.");
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

        private static void SetUpLogging()
        {
            //Create a new subfolder under the current active folder
            string newPath = @"C:\Temp\";
            string file = newPath + Properties.Settings.Default.log_roundtrip; 
            // Create the subfolder
            System.IO.Directory.CreateDirectory(newPath);

            UserContext.DefaultSentCounter = Properties.Settings.Default.log_frequency;
            if (!System.IO.File.Exists(newPath))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(file))
                {
                    fs.Close();
                }
            }
        }
    }
}
