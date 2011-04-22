using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Proxy
{
    public class ServerContext : IDisposable
    {
        /// <summary>
        /// The remote endpoint address.
        /// </summary>
        public EndPoint ClientAddress = null;

        public TcpClient serverConnection = null;

        public ServerHandler Handler = new ServerHandler();

        public bool Connected = true;

        public byte[] Buffer = null;

        public StringBuilder sb = new StringBuilder();

        public int BufferSize = 4096;

        public GameServer gameServer = null;

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
