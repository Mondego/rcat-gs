using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web;
using Alchemy.Server;
using Alchemy.Server.Classes;
using Newtonsoft.Json;
using System.Net;
using System.Collections;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Threading;
namespace RCAT
{
    public struct ClientBroadcast
    {
        public string data;
        public string[] clients;
    }

    public struct ClientConnection
    {
        public bool connected;
        public string client;
    }

    class Program
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
        protected static Dictionary<String, UserContext> onlineUsers = new Dictionary<string,UserContext>();

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Initialize the server on port 81, accept any IPs, and bind events.
            WSServer AServer = new WSServer(81, IPAddress.Any);
            AServer.Log.Logger.IsEnabledFor(log4net.Core.Level.Debug);
            AServer.DefaultOnReceive = new OnEventDelegate(OnReceive);
            AServer.DefaultOnSend = new OnEventDelegate(OnSend);
            AServer.DefaultOnConnect = new OnEventDelegate(OnConnect);
            AServer.DefaultOnDisconnect = new OnEventDelegate(OnDisconnect);
            AServer.TimeOut = new TimeSpan(0, 5, 0);

            AServer.Start();

            MySqlConnector.Connect();

            int workerThreads;
            int portThreads;

            ThreadPool.GetMaxThreads(out workerThreads, out portThreads);
            Console.WriteLine("\nMaximum worker threads: \t{0}" +
                "\nMaximum completion port threads: {1}",
                workerThreads, portThreads);

            // Accept commands on the console and keep it alive
            string Command = string.Empty;
            while (Command != "exit")
            {
                Command = Console.ReadLine();
            }

            AServer.Stop();
        }

        /// <summary>
        /// Event fired when a client connects to the Alchemy Websockets server instance.
        /// Adds the client to the online users list.
        /// </summary>
        /// <param name="AContext">The user's connection context</param>
        public static void OnConnect(UserContext AContext)
        {
            Console.WriteLine("Client Connection From : " + AContext.ClientAddress.ToString());

            User me = new User();
            me.Name = AContext.ClientAddress.ToString();
            me.Context = AContext;

            onlineUsers.Add(me.Name, me.Context);

            SendAllUsers(AContext);
        }

        /// <summary>
        /// Event fired when a data is received from the Alchemy Websockets server instance.
        /// Parses data as JSON and calls the appropriate message or sends an error message.
        /// </summary>
        /// <param name="AContext">The user's connection context</param>
        public static void OnReceive(UserContext AContext)
        {
            Console.WriteLine("Received Data From :" + AContext.ClientAddress.ToString()+ " | " + AContext.DataFrame.ToString());

            try
            {
                string json = AContext.DataFrame.ToString();

                // <3 dynamics
                Position pos = JsonConvert.DeserializeObject<Position>(json);

//                if ((int)obj.Type == (int)CommandType.Position)
//                {
                    SetPosition(pos, AContext.ClientAddress.ToString());
//                }
            }
            catch (Exception e) // Bad JSON! For shame.
            {
                Console.WriteLine("Failed to parse JSON");
                Response r = new Response();
                r.Type = ResponseType.Error;
                r.Data = new { Message = e.Message };

                AContext.Send(JsonConvert.SerializeObject(r));
            }
        }

        /// <summary>
        /// Event fired when the Alchemy Websockets server instance sends data to a client.
        /// Logs the data to the console and performs no further action.
        /// </summary>
        /// <param name="AContext">The user's connection context</param>
        public static void OnSend(UserContext AContext)
        {
            Console.WriteLine("Data Send To : " + AContext.ClientAddress.ToString() + " | " + AContext.DataFrame.ToString());
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
        public static void OnDisconnect(UserContext AContext)
        {
            try
            {
                Console.WriteLine("Client Disconnected : " + AContext.ClientAddress.ToString());

                User user = MySqlConnector.GetUser(AContext.ClientAddress.ToString());

                Response r = new Response();

                if (!String.IsNullOrEmpty(user.Name))
                {
                    r = new Response();
                    r.Type = ResponseType.Disconnect;
                    r.Data = new { Name = user.Name };

                    Broadcast(JsonConvert.SerializeObject(r), MySqlConnector.GetAllUsers());
                    MySqlConnector.RemoveUser(user.Name);
                }
                else
                    Console.WriteLine("ERROR: User not found!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
            }
        }
        /// <summary>
        /// Broadcasts a chat message to all online usrs
        /// </summary>
        /// <param name="Message">The chat message to be broadcasted</param>
        /// <param name="AContext">The user's connection context</param>
        private static void SetPosition(Position pos, string userName)
        {
            Response r = new Response();

            r.Type = ResponseType.Position;
            r.Data = new { Name = userName, Position = pos };

            MySqlConnector.SetPosition(userName, pos);

            Broadcast(JsonConvert.SerializeObject(r), MySqlConnector.GetAllUsers());

        }
        /// <summary>
        /// Sends an error message to the client who caused the error
        /// </summary>
        /// <param name="ErrorMessage">Details of the error</param>
        /// <param name="AContext">The user's connection context</param>
        private static void SendError(string ErrorMessage, UserContext AContext)
        {
            Console.WriteLine("Error Message: " + ErrorMessage);
            Response r = new Response();

            r = new Response();
            r.Type = ResponseType.Error;
            r.Data = new { Message = ErrorMessage };

            //AContext.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Informs a user of all the existing clients
        /// </summary>
        private static void SendAllUsers(UserContext AContext)
        {
            Response r = new Response();
            r = new Response();
            r.Type = ResponseType.AllUsers;

            // Using database
            User[] arr = MySqlConnector.GetAllUsers();
            r.Data = new { Users = arr };
            AContext.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts a message to all users, or if users is populated, a select list of users
        /// </summary>
        /// <param name="message">Message to be broadcast</param>
        /// <param name="users">Optional list of users to broadcast to. If null, broadcasts to all. Defaults to null.</param>
        private static void Broadcast(string message, User[] users)
        {
            foreach (User u in users)
            {
                // TODO: UserContext has to be saved on the server. In order to be RESTFul, UserContext should be 
                // obtainable from the Alchemy server per request
                UserContext user = onlineUsers[u.Name];
                user.Send(message);
            }
        }

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
        public class Response
        {
            public ResponseType Type { get; set; }
            public dynamic Data { get; set; }
        }   
    }
}

