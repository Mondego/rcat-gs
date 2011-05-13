using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Proxy
{
    public class ServerContext : IDisposable
    {
        /// <summary>
        /// The remote endpoint address.
        /// </summary>
        public EndPoint ClientAddress = null;

        public SocketFlags sflag = SocketFlags.None;

        public TcpClient serverConnection = null;

        public bool Connected = true;

        //public StringBuilder sb = new StringBuilder();
        public string[] sb = null;

        public bool IsTruncated = false;

        /// <summary>
        /// Maximum number of bytes that can be read at once from the TCP channel 
        /// </summary>
        public static int BufferSize = 4096;

        // buffer storing what has been received from the TCP channel
        public byte[] Buffer = new byte[BufferSize];

        // buffer that has to be sent through the TCP channel
        public byte[] SendBuffer = new byte[BufferSize];

        public SemaphoreSlim ReceiveReady = new SemaphoreSlim(1);

        public GameServer gameServer = null;

        /// <summary>
        /// create a ServerContext using a tcpclient tc and tied to the proxy's gameserver gs
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="tc"></param>
        /// <param name="ep"></param>
        public ServerContext(GameServer gs, TcpClient tc)
        {
            this.gameServer = gs;
            this.serverConnection = tc;
            this.ClientAddress = this.serverConnection.Client.RemoteEndPoint;
        }

        /// <summary>
        /// send a string to a servant
        /// </summary>
        /// <param name="Data"></param>
        public void Send(string Data)
        {
            string json = Data + '\0';
            Send(UTF8Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Send a byte array to a servant.
        /// </summary>
        /// <param name="Data">The data to be sent.</param>
        private void Send(byte[] Data)
        {
            try
            {
                Data.CopyTo(SendBuffer,0);
                serverConnection.Client.BeginSend(SendBuffer, 0, Data.Length, SocketFlags.None, EndSend, this);
            }
            catch (Exception e)
            {
                GameServer.Log.Info("[PROXY->SERVANT]: Exception in ServerContext while sending.", e);
                //AContext.SendReady.Release();
            }
        }

        /// <summary>
        /// Callback called when bytes have been sent from a proxy to a servant.
        /// </summary>
        /// <param name="AResult">The Async result.</param>
        public void EndSend(IAsyncResult AResult)
        {
            ServerContext SContext = (ServerContext)AResult.AsyncState;
            try
            {
                SContext.serverConnection.Client.EndSend(AResult);
                //GameServer.Log.Info("[PROXY->SERVANT]: Sent " + UTF8Encoding.UTF8.GetString(SContext.SendBuffer,0,1024));
                GameServer.Log.Info("[PROXY->SERVANT]: Sent " + UTF8Encoding.UTF8.GetString(SContext.SendBuffer, 0, 128)); //no msg should be longer than 128 in our case
                //AContext.SendReady.Release();
            }
            catch (Exception e)
            {
                GameServer.Log.Info("[PROXY->SERVANT]: Exception in ServerContext.EndSend", e);
                //AContext.SendReady.Release(); 
            }
        }

        /// <summary>
        /// Freeing, releasing, or resetting unmanaged resources. Use this method when you need to free a big object before the end of a function call. 
        /// This method is called at the end of a using{} block
        /// </summary>
        public void Dispose()
        {
            try
            {
                serverConnection.Client.Close();
                serverConnection = null;
                GameServer.Log.Warn("[PROXY->SERVANT]: in ServerContext: servant disconnected." + serverConnection.Client.RemoteEndPoint.ToString()); 
            }
            catch (Exception e) {
                GameServer.Log.Info("[PROXY->SERVANT]: in ServerContext: client already disconnected, ", e); 
            }
            finally
            {
                Connected = false;
            }
        }
    }
}
