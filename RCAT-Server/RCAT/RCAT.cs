using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Alchemy.Server.Classes;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCAT.Connectors;

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

    public class ServerMessage : Message
    {
        public long TimeStamp { get; set; }
    }
    
    /// <summary>
    /// Structure for sending broadcast information from Server to Clients. Contains the data to be sent and an array of clients that should receive it.
    /// </summary>
    public class ClientMessage : ServerMessage
    {
        public string[] clients;
    }

    class RCAT
    {
        /// <summary>
        /// Store the list of online users
        /// </summary>
        //protected static IList<User> OnlineUsers = new List<User>();
        //protected static Dictionary<String, UserContext> onlineUsers = new Dictionary<string,UserContext>();
        //public static List<string> onlineUsers = new List<string>();

        public static TcpClient proxy = null;

        public static JsonSerializer serializer = new JsonSerializer();

        protected static int _PROXYPORT = Properties.Settings.Default.proxyport;

        protected static string _PROXYURL = Properties.Settings.Default.proxyurl;

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
        /// Choose which data connector to use, the MySql or memory;
        /// </summary>
        public static DataConnector dataConnector = new MySqlConnector();
        //public static DataConnector dataConnector = new MemoryConnector();

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Choose which connector to start with
            dataConnector.Connect();

            // Servers register their existence and communicate with proxy through TCP
            try
            {
                LogConfigFile = "RCAT.config";
                LoggerName = "RCAT.Log";
                //"128.195.4.46", 82
                Thread.Sleep(2000);
                proxy = new TcpClient();
                proxy.BeginConnect(_PROXYURL, _PROXYPORT, RunServer, null);
                Log.Info("RCAT Server started!");
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
                                Log.Warn("[RCAT]: Server timed out connection with proxy.");
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

            try
            {
                if (received > 0)
                {
                    string values = UTF8Encoding.UTF8.GetString(RContext.buffer, 0, received);
                    if ((received == RCATContext.DefaultBufferSize && !values.EndsWith("\0")) || RContext.IsTruncated)
                    {
                        // There is more data to retrieve. Save current message and prepare for more
                        if (RContext.IsTruncated == false)
                        {
                            // Last message was not truncated
                            RContext.sb = values.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                            RContext.IsTruncated = true;
                            RContext.proxyConnection.Client.BeginReceive(RContext.buffer, 0, RCATContext.DefaultBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), RContext);
                        }
                        else
                        {
                            string[] tmp = values.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<string>();
                            list.AddRange(RContext.sb);
                            // Append last element in RContext.sb to first element of tmp array
                            list[RContext.sb.Length - 1] = list[RContext.sb.Length - 1] + tmp[0];
                            // Exclude the first element of tmp, and add it to the list
                            //var segment = new ArraySegment<string>(tmp,1,tmp.Length -1);
                            //list.AddRange(segment.Array);
                            for (int i = 1; i < tmp.Length; i++)
                            {
                                list.Add(tmp[i]);
                            }
                            
                            
                            RContext.sb = list.ToArray();
                            Log.Info("[RCAT]: Appended truncated message.");

                            if (values.EndsWith("\0"))
                            {
                                RContext.IsTruncated = false;
                                HandleRequest(RContext);
                                RContext.ReceiveReady.Release();
                            }
                            else
                            {
                                RContext.proxyConnection.Client.BeginReceive(RContext.buffer, 0, RCATContext.DefaultBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), RContext);
                            }
                        }
                    }
                    else
                    {
                        RContext.sb = values.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                        HandleRequest(RContext);
                        RContext.ReceiveReady.Release();
                    }
                }
                else
                {
                    // What do we do if lose connection to Proxy?
                    //RContext.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in DoReceive: ", e);
            }
        }

        // Handles the server request. Broadcast is the only server side functionality at this point. 
        protected static void HandleRequest(RCATContext server)
        {
            int i = 0;
            foreach (string s in server.sb)
            {
                try
                {
                    if (s != "")
                    {
                        Log.Info("[FROM PROXY]: Strings in SB" + s);
                        ServerMessage message = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerMessage>(s);
                        // a bug happens at this line when the servant has to concatenate multiple JSON msg together
                        server.message = message;

                        if (message.Type == ResponseType.Connection)
                            OnConnect(server);
                        else if (message.Type == ResponseType.Disconnect)
                            OnDisconnect(server);
                        else if (message.Type == ResponseType.Position)
                            SetPosition(server);
                        i++;
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("Error parsing JSON in RCAT.HandleRequest. JSON message was: " + server.sb[i]);
                    Log.Debug(e);
                }
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
                SendAllUsers(RContext, RContext.message.Data);
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

                User user = dataConnector.GetUser(RContext.message.Data);

                Message r = new Message();

                if (!String.IsNullOrEmpty(user.n))
                {
                    string[] clients = dataConnector.GetAllUsersNames();
                    RContext.Broadcast(user.n,clients,ResponseType.Disconnect, RContext.message.TimeStamp);
                    dataConnector.RemoveUser(user.n);
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
                dataConnector.SetPosition(user.n, user.p, RContext.message.TimeStamp);
                string[] clients = dataConnector.GetAllUsersNames();
                dynamic data = new { n = user.n, p = user.p };
                RContext.Broadcast(data, clients, ResponseType.Position, RContext.message.TimeStamp);
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
        /// Informs a user of all the existing clients. 
        /// </summary>
        private static void SendAllUsers(RCATContext RContext, string client)
        {
            try
            {
                ClientMessage r = new ClientMessage();
                r = new ClientMessage();
                r.Type = ResponseType.AllUsers;
                r.clients = new string[1];
                r.clients[0] = client;

                // Using database
                User[] arr = dataConnector.GetAllUsers();
                r.Data = new { Users = arr };
                RContext.Send(JsonConvert.SerializeObject(r));
            }
            catch (Exception e)
            {
                Log.Error("Exception in SendAllUsers:",e);
            }
        }
    }
}

