using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo
{
    public abstract class SocketsHandler
    {
        public SocketsManager Sockets;
        protected SocketsHandler(SocketsManager sockets) 
        {
            Sockets = sockets;
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public virtual async Task OnConnected(WebSocket socket) 
        {
            await Task.Run(()=> { Sockets.AddSocket(socket); });
        }
        /// <summary>
        /// 断开
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public virtual async Task OnDisconnected(WebSocket socket) 
        {
            await Sockets.RemoveSocketAsync(Sockets.GetId(socket));
        }
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(WebSocket socket,string message)
        {
            if (socket.State != WebSocketState.Open) return;
             
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),WebSocketMessageType.Text,true,CancellationToken.None);
        }

        /// <summary>
        /// 发送消息给指定 id 的 socket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(string id, string message)
        {
            await SendMessage(Sockets.GetSocketId(id), message);
        }

        /// <summary>
        /// 给所有 sockets 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessageToAll(string message)
        {
              foreach (var connection in Sockets.GetAllConnections())
                await SendMessage(connection.Value, message);
        }

        /// <summary>
        /// 正常大小数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="result"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract Task Receive(WebSocket socket, WebSocketReceiveResult result,
            byte[] buffer);
        /// <summary>
        /// 大数据接收
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="result"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract Task Receive(WebSocket socket, string result);
    }
}
