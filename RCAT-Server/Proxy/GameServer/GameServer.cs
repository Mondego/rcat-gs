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
            // Fill each SContext with information related to a particular servant and keep reading from the TCP pipe.
            ConnectReady.Release(); //decrease semaphore by 1
            if (TcpConnection != null)
            {
                using (ServerContext SContext = new ServerContext(this, TcpConnection)) 
                //each servant has its own context
                {
                    onlineServers.Add(SContext);
                    try
                    {
                        // When something has arrived in the TCP pipe, process it
                        while (SContext.serverConnection.Connected)
                        {
                            if (SContext.ReceiveReady.Wait(TimeOut)) //wait until timeout
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
                        Log.Error("[PROXY->SERVANT]: Game Server Forcefully Disconnected 1.", e); 
                    }
                }
                //at this point, the connexion with the servant has been lost, therefore ServerContext.Dispose() is called (because end of the using(){} block). 
            }
        }

        /// <summary>
        /// Callback called by the GameServer when a message has been received from a servant through the TCP pipe.
        /// 
        /// </summary>
        /// <param name="AResult"></param>
        private void DoReceive(IAsyncResult AResult)
        {
            ServerContext SContext = (ServerContext)AResult.AsyncState;
            int bytesReceived = 0;

            try
            {
                bytesReceived = SContext.serverConnection.Client.EndReceive(AResult);
            }
            catch (Exception e) { 
                Log.Error("[PROXY->SERVANT]: Game Server Forcefully Disconnected 2.", e);
            }

            // if something was received
            if (bytesReceived > 0)
            {
                string msgStr = UTF8Encoding.UTF8.GetString(SContext.Buffer, 0, bytesReceived);
                int maxBufSize = RCATContext.DefaultBufferSize; //servants and proxy have agree on the same MTU. If a packet is larger than maxBufSize, the sending servant flags it as truncated.
                // packets terminate by \0 and can be bundled by servants. If the last packet in the msg is cut, store it and read (at least) the following msg to get the rest of it.
                if ((bytesReceived == maxBufSize && !msgStr.EndsWith("\0")) || SContext.IsTruncated)
                {
                    if (SContext.IsTruncated == false) // Previous msg was not truncated
                    {
                        SContext.sb = msgStr.Split(new char[]{'\0'} , StringSplitOptions.RemoveEmptyEntries);
                        SContext.IsTruncated = true;
                        SContext.serverConnection.Client.BeginReceive(SContext.Buffer, 0, maxBufSize, SocketFlags.None, new AsyncCallback(DoReceive), SContext);
                    }
                    else // previous msg was truncated: I concat current msg to the previous one 
                    {
                        string[] tmp = msgStr.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                        var msgStrList = new List<string>();
                        msgStrList.AddRange(SContext.sb); //list now contains all previous msgStr that were truncated
                        // Append last element in RContext.sb to first element of tmp array (concat the beginning and the end of the packet in the middle of the truncation)
                        msgStrList[SContext.sb.Length - 1] = msgStrList[SContext.sb.Length - 1] + tmp[0];
                        // Exclude the first element of tmp, and add it to the list
                        var segment = new ArraySegment<string>(tmp, 1, tmp.Length - 1);
                        msgStrList.AddRange(segment.Array);

                        SContext.sb = msgStrList.ToArray();
                        Log.Info("[PROXY->SERVANT]: Appended truncated message.");

                        if (msgStr.EndsWith("\0")) // even though a servant had to send more data (and flagged a msg as truncated), in this case no packet in the msg is truncated. Same as non-truncated msg, really.
                        {
                            SContext.IsTruncated = false;
                            HandleRequest(SContext);
                            SContext.ReceiveReady.Release();
                        }
                        else
                        {
                            SContext.serverConnection.Client.BeginReceive(SContext.Buffer, 0, maxBufSize, SocketFlags.None, new AsyncCallback(DoReceive), SContext);
                        }
                    }
                }
                else //msg was smaller than max buffer size, no truncation, "normal" case
                {
                    SContext.sb = msgStr.Split('\0');
                    HandleRequest(SContext);
                    SContext.ReceiveReady.Release();
                }

            }
            else //if nothing was received yet doReceive was called, it's because the servant died
            {
                onlineServers.Remove(SContext);
                SContext.Dispose();
            }
        }



        /// <summary>
        /// Handles the server request. If position, message.data is a ClientBroadcast object. 
        /// </summary>
        /// <param name="server"></param>
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

        /// <summary>
        ///  Sends client data to the server
        /// </summary>
        /// <param name="client"></param>
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

        /// <summary>
        ///  notify a server that a client just connected
        /// </summary>
        /// <param name="client"></param>
        public void SendClientConnect(UserContext client)
        {
            ServerContext server = PickServer();

            Message resp = new Message();
            resp.Type = ResponseType.Connection;
            resp.Data = client.ClientAddress.ToString();

            server.Send(Newtonsoft.Json.JsonConvert.SerializeObject(resp));
        }

        /// <summary>
        ///  pick a server at round-robin to forward a packet to
        /// </summary>
        /// <returns></returns>
        protected ServerContext PickServer()
        {
            ServerContext server =  onlineServers[roundrobin];
            roundrobin++;
            if (roundrobin == onlineServers.Count)
                roundrobin = 0;
            return server;
        }

        /// <summary>
        ///  notify a client that another client disconnected
        /// </summary>
        /// <param name="client"></param>
        public void SendClientDisconnect(UserContext client)
        {
            ServerContext server = PickServer();

            Message resp = new Message();
            resp.Type = ResponseType.Disconnect;
            resp.Data = client.ClientAddress.ToString();

            server.Send(Newtonsoft.Json.JsonConvert.SerializeObject(resp));
        }

        /// <summary>
        ///  when proxy is stopped
        /// </summary>
        public void Stop()
        {
            serverListener.Stop();
        }
    }
}
