﻿using System;
using Alchemy.Server;
using System.Net;
using Alchemy.Server.Classes;
using log4net;

namespace Proxy
{
    public class ClientServer
    {
        WSServer clientListener = null;
        public ClientServer(ILog log)
        {
            // Client server uses Alchemy Websockets
            clientListener = new WSServer(81, IPAddress.Any);
            clientListener.Log.Logger.IsEnabledFor(log4net.Core.Level.Debug);
            clientListener.DefaultOnReceive = new OnEventDelegate(OnReceive);
            clientListener.DefaultOnSend = new OnEventDelegate(OnSend);
            clientListener.DefaultOnConnect = new OnEventDelegate(OnConnect);
            clientListener.DefaultOnDisconnect = new OnEventDelegate(OnDisconnect);
            clientListener.TimeOut = new TimeSpan(0, 5, 0);

            clientListener.Start();
        }

        // CLIENT SECTION
        // Events generated by client connections
        public static void OnConnect(UserContext AContext)
        {
        }

        public static void OnReceive(UserContext AContext)
        {
        }

        public static void OnSend(UserContext AContext)
        {
        }

        public static void OnDisconnect(UserContext AContext)
        {
        }


        public void Stop()
        {
        }
    }
}
