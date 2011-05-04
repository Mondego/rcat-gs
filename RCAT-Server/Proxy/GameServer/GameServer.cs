using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Alchemy.Server.Classes;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCAT;

namespace Proxy
{
    
    public class GameServer
    {
        protected static TcpListener serverListener = null;

        protected List<ServerContext> onlineServers = new List<ServerContext>();

        protected TimeSpan TimeOut = new TimeSpan(0, 30, 0);

        /// <summary>
        /// This Semaphore limits how many simultaneous handshake events we have active at a time.
        /// </summary>
        private static int _MAX_SIMULTANEOUS_HANDSHAKE = 5; //todo: put that in config
        private SemaphoreSlim ConnectReady = new SemaphoreSlim(_MAX_SIMULTANEOUS_HANDSHAKE);

        protected int roundrobin = 0;

        protected JsonSerializer serializer = new JsonSerializer();

        public static ILog Log;

        protected static int _SERVERLISTENERPORT = 82; //todo: put this in an external  config file

        protected void RegisterProxyMethods()
        {
            Proxy.sendSetPositionToServer = SendPosition;
            Proxy.sendClientDisconnectToServer = SendClientDisconnect;
            Proxy.sendClientConnectToServer = SendClientConnect;
        }

        public GameServer(ILog log)
        {
            Log = log;
            RegisterProxyMethods();

            if (serverListener == null)
            {
                try
                {
                    // Servers register their existence and communicate with proxy through TCP
                    serverListener = new TcpListener(IPAddress.Any, _SERVERLISTENERPORT);
                    ThreadPool.QueueUserWorkItem(serverListen, null);
                }
                catch (Exception e) {
                    Log.Error("[PROXY->SERVANT] Game Server failed to start", e); 
                }
            }
        }


        
        /// <summary>
        /// Listen for new servants connecting by TCP to port _SERVERLISTENERPORT
        /// </summary>
        /// <param name="State"></param>
        protected void serverListen(object State)
        {
            serverListener.Start();
            while (serverListener != null)
            {
                try
                {
                    serverListener.BeginAcceptTcpClient(RunServer, null);
                    ConnectReady.Wait();
                    // the semaphore ConnectReady allows up to _MAX_SIMULTANEOUS_HANDSHAKE TCP handshakes to happen simultaneously
                    // this is only the max number of simultaneous handshakes, NOT the max number of servant connections!
                }
                catch (Exception e) {
                    Log.Error("[PROXY->SERVANT]: Error while waiting for servants to connect in serverListen. ", e);
                }
            }
        }

        /// <summary>
        /// Handle TCP connections initiated by servants and receive data from the TCP pipe.
        /// </summary>
        /// <param name="AResult"></param>
        protected void RunServer(IAsyncResult AResult)
        {
            // Server connection
            TcpClient TcpConnection = null;
            // Accept connection between servant and proxy
            if (serverListener != null)
            {
                try
                {
                    TcpConnection = serverListener.EndAcceptTcpClient(AResult);
                }
                catch (Exception e)
                {
                    Log.Error("[PROXY->SERVANT]: Connection with servant failed. ", e);
                }
            }
            // Fill each SContext with information related to a particular servant and keep readin in the TCP pipe.
            ConnectReady.Release(); //decrease semaphore by 1
            if (TcpConnection != null)
            {
                using (ServerContext SContext = new ServerContext()) 
                //each server has its own context
                {
                    SContext.gameServer = this;
                    SContext.serverConnection = TcpConnection;
                    SContext.ClientAddress = SContext.serverConnection.Client.RemoteEndPoint;
                    onlineServers.Add(SContext);
                    try
                    {
                        // When something has arrived in the TCP pipe, process it
                        while (SContext.serverConnection.Connected)
                        {
                            if (SContext.ReceiveReady.Wait(TimeOut))
                            {
                                SContext.serverConnection.Client.BeginReceive(SContext.Buffer, 0, SContext.Buffer.Length, SContext.sflag, new AsyncCallback(DoReceive), SContext);
                            }
                            else
                            {
                                Log.Warn("[PROXY->SERVANT]: Game Server timed out. Disconnecting.");
                                break;
                            }
                        }
                    }
                    catch (Exception e) {
                        Log.Error("[PROXY->SERVANT]: Game Server Forcefully Disconnected.", e); 
                    }
                }
                //at this point, the connexion with the servant has been lost, therefore ServerContext.Dispose() is called (because end of the using(){} block). 
            }
        }

        /// <summary>
        /// Events generated by servers connecting to the proxy
        /// </summary>
        /// <param name="AResult"></param>
        private void DoReceive(IAsyncResult AResult)
        {
            ServerContext SContext = (ServerContext)AResult.AsyncState;
            int received = 0;

            try
            {
                received = SContext.serverConnection.Client.EndReceive(AResult);
            }
            catch (Exception e) { Log.Error("[GAMESERVER]: Game Server Forcefully Disconnected", e); }

            if (received > 0)
            {
                string values = UTF8Encoding.UTF8.GetString(SContext.Buffer, 0, received);
                if ((received == RCATContext.DefaultBufferSize && !values.EndsWith("\0")) || SContext.IsTruncated)
                {
                    // There is more data to retrieve. Save current message and prepare for more
                    if (SContext.IsTruncated == false)
                    {
                        // Last message was not truncated
                        SContext.sb = values.Split(new char[]{'\0'} , StringSplitOptions.RemoveEmptyEntries);
                        SContext.IsTruncated = true;
                        SContext.serverConnection.Client.BeginReceive(SContext.Buffer, 0, RCATContext.DefaultBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), SContext);
                    }
                    else
                    {
                        string[] tmp = values.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);


                        var list = new List<string>();
                        list.AddRange(SContext.sb);
                        // Append last element in RContext.sb to first element of tmp array
                        list[SContext.sb.Length - 1] = list[SContext.sb.Length - 1] + tmp[0];
                        // Exclude the first element of tmp, and add it to the list
                        var segment = new ArraySegment<string>(tmp, 1, tmp.Length - 1);
                        list.AddRange(segment.Array);

                        SContext.sb = list.ToArray();
                        Log.Info("[RCAT]: Appended truncated message.");

                        if (values.EndsWith("\0"))
                        {
                            SContext.IsTruncated = false;
                            HandleRequest(SContext);
                            SContext.ReceiveReady.Release();
                        }
                        else
                        {
                            SContext.serverConnection.Client.BeginReceive(SContext.Buffer, 0, RCATContext.DefaultBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), SContext);
                        }
                    }
                }
                else
                {
                    SContext.sb = values.Split('\0');
                    HandleRequest(SContext);
                    SContext.ReceiveReady.Release();
                }

            }
            else
            {
                onlineServers.Remove(SContext);
                SContext.Dispose();
            }
        }

        // Handles the server request. If position, message.data is a ClientBroadcast object. 
        protected void HandleRequest(ServerContext server)
        {
            int i = 0;
            //Newtonsoft.Json.Linq.JObject test = new Newtonsoft.Json.Linq.JObject();
            //test.Value<ClientBroadcast>(test)
             //   User Value<User>(message.Data)
            try
            {
                foreach (string s in server.sb)
                {
                    if (s != "")
                    {
                        Message message = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(s);
                        if (message.Type == ResponseType.Position)
                        {
                            ClientBroadcast cb = (ClientBroadcast)serializer.Deserialize(new JTokenReader(message.Data), typeof(ClientBroadcast));
                            Proxy.broadcastToClients(cb);
                            // TODO: Implement SendAllUsers
                        }
                        i++;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("Error parsing JSON in GameServer.HandleRequest. JSON: " + server.sb[i]);
                //Log.Error("Error parsing JSON in GameServer.HandleRequest",e);
                Log.Debug(e);
            }
        }

        // Sends client data to the server
        public void SendPosition(User client)
        {
            //ServerContext server = clientPerServer[client.Name];
            ServerContext server = PickServer();
            
            TimeStampedMessage resp = new TimeStampedMessage();
            resp.Type = ResponseType.Position;
            resp.Data = client;
            resp.TimeStamp = DateTime.Now.Ticks;

            Log.Info("Sending Client info: " + resp.Data.ToString());
            
            server.Send(Newtonsoft.Json.JsonConvert.SerializeObject(resp) + '\0');
        }

        // notify a server that a client just connected
        public void SendClientConnect(UserContext client)
        {
            ServerContext server = PickServer();

            Message resp = new Message();
            resp.Type = ResponseType.Connection;
            resp.Data = client.ClientAddress.ToString();

            server.Send(Newtonsoft.Json.JsonConvert.SerializeObject(resp));
        }

        // pick a server at round-robin to forward a packet to
        protected ServerContext PickServer()
        {
            ServerContext server =  onlineServers[roundrobin];
            roundrobin++;
            if (roundrobin == onlineServers.Count)
                roundrobin = 0;
            return server;
        }

        // notify a client that another client disconnected
        public void SendClientDisconnect(UserContext client)
        {
            ServerContext server = PickServer();

            Message resp = new Message();
            resp.Type = ResponseType.Disconnect;
            resp.Data = client.ClientAddress.ToString();

            server.Send(Newtonsoft.Json.JsonConvert.SerializeObject(resp));
        }

        // when proxy is stopped
        public void Stop()
        {
            serverListener.Stop();
        }
    }
}
