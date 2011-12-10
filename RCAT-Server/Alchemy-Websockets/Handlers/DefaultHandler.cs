﻿/*
Copyright 2011 Olivine Labs, LLC.
http://www.olivinelabs.com
*/

/*
This file is part of Alchemy Websockets.

Alchemy Websockets is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Alchemy Websockets is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with Alchemy Websockets.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Alchemy.Server.Classes;
using System.Net.Sockets;

namespace Alchemy.Server.Handlers
{
    /// <summary>
    /// When the protocol has not yet been determined the system defaults to this request handler.
    /// Singleton, just like the other handlers.
    /// </summary>
    class _defaultHandler : Handler
    {
        private static _defaultHandler _instance;

        private _defaultHandler() { }

        public static _defaultHandler Instance
        {
            get 
            {
                _createLock.Wait();
                if (_instance == null)
                {
                    _instance = new _defaultHandler();
                }
                _createLock.Release();
                return _instance;
            }
        }

        /// <summary>
        /// Handles the initial request.
        /// Attempts to process the header that should have been sent.
        /// Otherwise, through magic and wizardry, the client gets disconnected.
        /// </summary>
        /// <param name="context">The user context.</param>
        public override void HandleRequest(Context context)
        {
            if (context.IsSetup)
            {
                context.Dispose();
            }
            else
            {
                ProcessHeader(context);
            }
        }

        /// <summary>
        /// Processes the header.
        /// </summary>
        /// <param name="context">The user context.</param>
        public void ProcessHeader(Context context)
        {
            string data = context.UserContext.Encoding.GetString(context.Buffer, 0, context.ReceivedByteCount);
            //Check first to see if this is a flash socket XML request.
            if (data == "<policy-file-request/>\0")
            {
                try
                {
                    //if it is, we access the Access Policy Server instance to send the appropriate response.
                    context.Server.AccessPolicyServer.SendResponse(context.Connection);
                }
                catch { }
                context.Dispose();
            }
            else//If it isn't, process http/websocket header as normal.
            {
                context.Header = new Header(data);
                switch (context.Header.Protocol)
                {
                    case Protocol.WebSocketHybi00:
                        context.Handler = Alchemy.Server.Handlers.WebSocket.hybi00.WebSocketHandler.Instance;
                        context.UserContext.DataFrame = new Alchemy.Server.Handlers.WebSocket.hybi00.DataFrame();
                        break;
                    case Protocol.WebSocketHybi10:
                        context.Handler = Alchemy.Server.Handlers.WebSocket.hybi10.WebSocketHandler.Instance;
                        context.UserContext.DataFrame = new Alchemy.Server.Handlers.WebSocket.hybi10.DataFrame();
                        break;
                    case Protocol.FlashSocket:
                        context.Handler = Alchemy.Server.Handlers.WebSocket.hybi00.WebSocketHandler.Instance;
                        context.UserContext.DataFrame = new Alchemy.Server.Handlers.WebSocket.hybi00.DataFrame();
                        break;
                    default:
                        context.Header.Protocol = Protocol.None;
                        break;
                }
                if (context.Header.Protocol != Protocol.None)
                {
                    context.Handler.HandleRequest(context);
                }
                else
                {
                    context.UserContext.Send(Response.NotImplemented, true);
                }
            }
        }

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="context">The user context.</param>
        /// <param name="close">if set to <c>true</c> [close].</param>
        public override void Send(byte[] data, Context context, bool close = false)
        {
            AsyncCallback callback = EndSend;
            if (close)
                callback = EndSendAndClose;
            context.SendReady.Wait();
            try
            {
                context.Connection.Client.BeginSend(data, 0, data.Length, SocketFlags.None, callback, context);
            }
            catch
            {
                context.SendReady.Release();
            }
        }

        /// <summary>
        /// Ends the send.
        /// </summary>
        /// <param name="result">The Async result.</param>
        public override void EndSend(IAsyncResult result)
        {
            Context context = (Context)result.AsyncState;
            try
            {
                context.Connection.Client.EndSend(result);
                context.SendReady.Release();
            }
            catch
            {
                context.SendReady.Release();
            }
            context.UserContext.OnSend();
        }

        /// <summary>
        /// Ends the send and closes the connection.
        /// </summary>
        /// <param name="result">The Async result.</param>
        public override void EndSendAndClose(IAsyncResult result)
        {
            Context context = (Context)result.AsyncState;
            try
            {
                context.Connection.Client.EndSend(result);
                context.SendReady.Release();
            }
            catch
            {
                context.SendReady.Release();
            }
            context.UserContext.OnSend();
            context.Dispose();
        }
    }
}