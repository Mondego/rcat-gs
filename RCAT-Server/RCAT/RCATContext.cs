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
        // TODO: Default buffer size. Should set this somewhere global?
        public static int DefaultBufferSize = 4096;
        //public StringBuilder sb = new StringBuilder();
        public string[] sb = null;
        public byte[] buffer = new byte[DefaultBufferSize];
        public TcpClient proxyConnection;
        public Message message;
        public SemaphoreSlim ReceiveReady = new SemaphoreSlim(1);
        public bool IsTruncated = false;

        public void Send(string Data)
        {
            Send(UTF8Encoding.UTF8.GetBytes(Data + '\0'));
        }

        public void Broadcast(dynamic data, string[] clients, ResponseType type)
        {

            ClientBroadcast cb = new ClientBroadcast();
            cb.clients = clients;
            cb.data = data;
            cb.type = type;

            Message r = new Message();
            r.Type = type;
            r.Data = cb;

            Send(Newtonsoft.Json.JsonConvert.SerializeObject(r));
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
                if (Data.Length > RCATContext.DefaultBufferSize)
                    proxyConnection.Client.BeginSend(Data, 0, Data.Length, SocketFlags.Truncated, ACallback, this);
                else
                    proxyConnection.Client.BeginSend(Data, 0, Data.Length, SocketFlags.None, ACallback, this);
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
