using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace RCAT
{
    public class RCATContext : IDisposable
    {
        // TODO: this constant should be set in a config file
        public static int DefaultBufferSize = 4096;
        //public StringBuilder sb = new StringBuilder();
        /// <summary>
        /// stores a list of json msg received from proxy through tcp pipe 
        /// when msg are not bundled, this array contains only one element
        /// </summary>
        public string[] receivedMessages = null;

        public string leftover = "";
        public byte[] buffer = new byte[DefaultBufferSize];
        public TcpClient proxyConnection;
        //public ServerMessage message;
        public SemaphoreSlim ReceiveReady = new SemaphoreSlim(1);
        public bool IsTruncated = false;

        public void Send(string Data)
        {
            Send(UTF8Encoding.UTF8.GetBytes(Data + '\0'));
        }

        public void Broadcast(dynamic data, string[] clients, ResponseType type, long timestamp)
        {

            ClientMessage cb = new ClientMessage();
            cb.clients = clients;
            cb.Data = data;
            cb.Type = type;
            cb.TimeStamp = timestamp;

            Send(Newtonsoft.Json.JsonConvert.SerializeObject(cb));
        }

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="Data">The data.</param>
        /// <param name="AContext">The user context.</param>
        /// <param name="Close">if set to <c>true</c> [close].</param>
        private void Send(byte[] Data)
        {
            AsyncCallback ACallback = EndSend;
            try
            {
                SocketFlags sf = (Data.Length > RCATContext.DefaultBufferSize) ? SocketFlags.Truncated : SocketFlags.None;
                proxyConnection.Client.BeginSend(Data, 0, Data.Length, sf, ACallback, this);
                //if (Data.Length > RCATContext.DefaultBufferSize)
                //    proxyConnection.Client.BeginSend(Data, 0, Data.Length, SocketFlags.Truncated, ACallback, this);
                //else
                //    proxyConnection.Client.BeginSend(Data, 0, Data.Length, SocketFlags.None, ACallback, this);
            }
            catch
            {
                RCAT.Log.Warn("[ServerContext]: Exception sending");
                //AContext.SendReady.Release();
            }
        }

        /// <summary>
        /// Ends the send.
        /// </summary>
        /// <param name="AResult">The Async result.</param>
        public void EndSend(IAsyncResult AResult)
        {
            RCATContext RContext = (RCATContext)AResult.AsyncState;
            try
            {
                RContext.proxyConnection.Client.EndSend(AResult);
                //AContext.SendReady.Release();
            }
            catch
            {
                RCAT.Log.Warn("[ServerContext]: Exception end send");
                //AContext.SendReady.Release(); 
            }
        }

        public void Dispose()
        {

        }
    }
}
