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

        public TcpClient serverConnection = null;

        public bool Connected = true;

        //public StringBuilder sb = new StringBuilder();
        public string[] sb = null;

        public static int BufferSize = 4096;

        //Receive buffer
        public byte[] Buffer = new byte[BufferSize];

        //Send buffer
        public byte[] SendBuffer = new byte[BufferSize];

        public SemaphoreSlim ReceiveReady = new SemaphoreSlim(1);

        public GameServer gameServer = null;

        public void Send(string Data)
        {
            Send(UTF8Encoding.UTF8.GetBytes(Data + '\0'));
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
                Data.CopyTo(SendBuffer,0);
                serverConnection.Client.BeginSend(SendBuffer, 0, Data.Length, SocketFlags.None, ACallback, this);
            }
            catch
            {
                Console.WriteLine("[ServerContext]: Exception sending");
                //AContext.SendReady.Release();
            }
        }

        /// <summary>
        /// Ends the send.
        /// </summary>
        /// <param name="AResult">The Async result.</param>
        public void EndSend(IAsyncResult AResult)
        {
            ServerContext SContext = (ServerContext)AResult.AsyncState;
            try
            {
                SContext.serverConnection.Client.EndSend(AResult);
                Console.WriteLine("[PROXY->SERVER]: " + UTF8Encoding.UTF8.GetString(SContext.SendBuffer,0,1024));
                //AContext.SendReady.Release();
            }
            catch
            {
                Console.WriteLine("[ServerContext]: Exception end send");
                //AContext.SendReady.Release(); 
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                serverConnection.Client.Close();
                serverConnection = null;
            }
            catch (Exception e) { Console.WriteLine("Client Already Disconnected", e); }
            finally
            {
                if (Connected)
                {
                    Connected = false;
                }
            }
        }
    }
}
