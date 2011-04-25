using System;
using System.Collections.Generic;
using Alchemy.Server.Classes;
using Newtonsoft.Json;

using System.Threading;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using log4net;
using System.IO;

namespace RCAT
{
    /// <summary>
    /// Defines the type of response to send back to the client for parsing logic
    /// </summary>
    public enum ResponseType : int
    {
        Connection = 0,
        Disconnect = 1,
        Position = 2,
        AllUsers = 3,
        Error = 255
    }

    /// <summary>
    /// Defines the response object to send back to the client
    /// </summary>
    public class Message
    {
        public ResponseType Type { get; set; }
        public dynamic Data { get; set; }
    }

    /// <summary>
    /// Structure for sending broadcast information from Server to Clients. Contains the data to be sent and an array of clients that should receive it.
    /// </summary>
    public struct ClientBroadcast
    {
        public dynamic data;
        public string[] clients;
        public ResponseType type;
    }

    class RCAT
    {
        /// <summary>
        /// Defines a type of command that the client sends to the server
        /// </summary>
        public enum CommandType : int
        {
            Register = 0,
            Position
        }

        /// <summary>
        /// Store the list of online users
        /// </summary>
        //protected static IList<User> OnlineUsers = new List<User>();
        //protected static Dictionary<String, UserContext> onlineUsers = new Dictionary<string,UserContext>();
        public static List<string> onlineUsers = new List<string>();

        public static TcpClient proxy = null;

        public static JsonSerializer serializer = new JsonSerializer();

        public static int Port = 82;

        public static TimeSpan TimeOut = new TimeSpan(0, 30, 0);

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
        public static ILog Log = LogManager.GetLogger("RCAT.Log");

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            MySqlConnector.Connect();

            /*
            int workerThreads;
            int portThreads;

            ThreadPool.GetMaxThreads(out workerThreads, out portThreads);
            Console.WriteLine("\nMaximum worker threads: \t{0}" +
                "\nMaximum completion port threads: {1}",
                workerThreads, portThreads);
            */

            // Servers register their existence and communicate with proxy through TCP
            try
            {
                LogConfigFile = "RCAT.config";
                LoggerName = "RCAT.Log";
                //"128.195.4.46", 882
                Thread.Sleep(2000);
                proxy = new TcpClient();
                proxy.BeginConnect("opensim.ics.uci.edu", 882, RunServer, null);
                Log.Info("RCAT Server started!");

                //Listener = new TcpListener(IPAddress.Any, Port);
                //ThreadPool.QueueUserWorkItem(serverListen, null);
            }
            catch { Log.Error("Game Server failed to start"); }

            // Accept commands on the console and keep it alive

            // Accept commands on the console and keep it alive
            string Command = string.Empty;
            while (Command != "exit")
            {
                Command = Console.ReadLine();
            }
        }

        protected static void RunServer(IAsyncResult AResult)
        {
            // Server connection
            //TcpClient TcpConnection = null;
            try
            {
                proxy.EndConnect(AResult);
            }
            catch (Exception e) { Log.Error("Connect Failed", e); }

            if (proxy != null)
            {
                using (RCATContext RContext = new RCATContext())
                {
                    try
                    {
                        RContext.proxyConnection = proxy;
                        while (proxy.Connected)
                        {
                            if (RContext.ReceiveReady.Wait(TimeOut))
                            {
                                proxy.Client.BeginReceive(RContext.buffer, 0, RCATContext.DefaultBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), RContext);
                            }
                            else
                            {
                                Log.Warn("TIMED OUT - RCAT");
                                break;
                            }
                        }
                    }
                    catch (Exception e) { Log.Warn("Server Forcefully Disconnected", e); }
                }
            }
        }

        // Events generated by servers connecting to proxy
        private static void DoReceive(IAsyncResult AResult)
        {
            RCATContext RContext = (RCATContext)AResult.AsyncState;
            int received = 0;

            try
            {
                received = RContext.proxyConnection.Client.EndReceive(AResult);
            }
            catch (Exception e) { Log.Error("[RCATSERVER]: RCAT Server Forcefully Disconnected. Exception: {0}", e); }

            // TODO: No packets bigger then BufferSize are allowed at this time
            if (received > 0)
            {
                //RContext.sb.Append(UTF8Encoding.UTF8.GetString(RContext.buffer, 0, received));
                RContext.sb = UTF8Encoding.UTF8.GetString(RContext.buffer, 0, received);
                Log.Info("Received from client user info: " + RContext.sb);
                if (received == RCATContext.DefaultBufferSize)
                    throw new Exception("[RCATSERVER]: HTTP Connect packet reached maximum size. FIXME!!");
                HandleRequest(RContext);
                //RContext.sb.Clear();
                RContext.ReceiveReady.Release();
              
            }
            else
            {
                // What do we do if lose connection to Proxy?
                //RContext.Dispose();
            }
        }

        // Handles the server request. Broadcast is the only server side functionality at this point. 
        protected static void HandleRequest(RCATContext server)
        {
            try
            {
                Message message = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(server.sb.ToString());
                server.message = message;

                if (message.Type == ResponseType.Connection)
                    OnConnect(server);
                else if (message.Type == ResponseType.Disconnect)
                    OnDisconnect(server);
                else if (message.Type == ResponseType.Position)
                    SetPosition(server);
            }
            catch (Exception e)
            {
                Log.Warn("JSON message was: " + server.sb.ToString());
                Log.Error(e);
            }
        }

        /// <summary>
        /// Event fired when a client connects to the Alchemy Websockets server instance.
        /// Adds the client to the online users list.
        /// </summary>
        /// <param name="AContext">The user's connection context</param>
        public static void OnConnect(RCATContext RContext)
        {
            try
            {
                Log.Info("Client Connection From : " + (string)RContext.message.Data);

                onlineUsers.Add(RContext.message.Data);
                SendAllUsers(RContext);
            }
            catch (Exception e)
            {
                Log.Error("Exception in OnConnect", e);
            }

            
        }

        // NOTE: This is not safe code. You may end up broadcasting to people who
        // disconnected. Luckily for us, Alchemy handles exceptions in its event methods, so we don't
        // have random, catastrophic changes.
        /// <summary>
        /// Event fired when a client disconnects from the Alchemy Websockets server instance.
        /// Removes the user from the online users list and broadcasts the disconnection message
        /// to all connected users.
        /// </summary>
        /// <param name="AContext">The user's connection context</param>
        public static void OnDisconnect(RCATContext RContext)
        {
            try
            {
                Log.Info("Client Disconnected : " + RContext.message.Data);

                User user = MySqlConnector.GetUser(RContext.message.Data);

                Message r = new Message();

                if (!String.IsNullOrEmpty(user.Name))
                {
                    string[] clients = MySqlConnector.GetAllUsersNames();
                    RContext.Broadcast(user.Name,clients,ResponseType.Disconnect);
                    MySqlConnector.RemoveUser(user.Name);
                }
                else
                    Log.Warn("ERROR: User not found!");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Broadcasts a position message to all online users
        /// </summary>
        /// <param name="Message">The chat message to be broadcasted</param>
        /// <param name="AContext">The user's connection context</param>
        private static void SetPosition(RCATContext RContext)
        {
            try
            {
                User user = new User();
                user = (User)serializer.Deserialize(new JTokenReader(RContext.message.Data), typeof(User));
                MySqlConnector.SetPosition(user.Name, user.pos);
                string[] clients = MySqlConnector.GetAllUsersNames();
                RContext.Broadcast(user, clients, ResponseType.Position);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        /// <summary>
        /// Sends an error message to the client who caused the error
        /// </summary>
        /// <param name="ErrorMessage">Details of the error</param>
        /// <param name="AContext">The user's connection context</param>
        private static void SendError(string ErrorMessage, UserContext AContext)
        {
            Log.Warn("Error Message: " + ErrorMessage);
            Message r = new Message();

            r = new Message();
            r.Type = ResponseType.Error;
            r.Data = new { Message = ErrorMessage };

            //AContext.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Informs a user of all the existing clients, and informing all existing cliets of new user. 
        /// </summary>
        private static void SendAllUsers(RCATContext RContext)
        {
            try
            {
                Message r = new Message();
                r = new Message();
                r.Type = ResponseType.AllUsers;

                // Using database
                User[] arr = MySqlConnector.GetAllUsers();
                r.Data = new { Users = arr };
                //RContext.Send(JsonConvert.SerializeObject(r));
            }
            catch (Exception e)
            {
                Log.Error("Exception in SendAllUsers:",e);
            }
        }
    }
}

