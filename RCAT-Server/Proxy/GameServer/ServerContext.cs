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

        public StringBuilder sb = new StringBuilder();

        public static int BufferSize = 4096;

        public byte[] Buffer = new byte[BufferSize];

        public SemaphoreSlim ReceiveReady = new SemaphoreSlim(1);

        public GameServer gameServer = null;

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="Data">The data.</param>
        /// <param name="AContext">The user context.</param>
        /// <param name="Close">if set to <c>true</c> [close].</param>
        public void Send(byte[] Data)
        {
            AsyncCallback ACallback = EndSend;
            try
            {
                serverConnection.Client.BeginSend(Data, 0, Data.Length, SocketFlags.None, ACallback, this);
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
