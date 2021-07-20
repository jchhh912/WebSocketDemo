using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo
{
    public class SocketsMiddleware
    {
        private readonly RequestDelegate _next;
        private SocketsHandler _Handler { get; }
        public SocketsMiddleware(RequestDelegate next, SocketsHandler handle)
        {
            _Handler = handle;
            _next = next;
        }
        /// <summary>
        /// 接收连接-----接收数据长度如果超过设置的buffer 程序会出错 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                //转换为ws连接
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await _Handler.OnConnected(socket);
                //接收消息
                var buffer = new byte[1024 * 1];
                var offset = 0;
                var free = buffer.Length;
                //判断连接
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    offset += result.Count;
                    free -= result.Count;
                    if (result.EndOfMessage) break;
                    if (free == 0)
                    {
                        // No free space
                        // Resize the outgoing buffer
                        var newSize = buffer.Length + 1024;
                        // Check if the new size exceeds a limit
                        // It should suit the data it receives
                        // This limit however has a max value of 2 billion bytes (2 GB)
                        if (newSize > 1024)
                        {
                            throw new Exception("Maximum size exceeded");
                        }
                        var newBuffer = new byte[newSize];
                        Array.Copy(buffer, 0, newBuffer, 0, offset);
                        buffer = newBuffer;
                        free = buffer.Length - offset;
                    }
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            await _Handler.Receive(socket, result, buffer);
                            break;
                        case WebSocketMessageType.Binary:
                            break;
                        case WebSocketMessageType.Close:
                            await _Handler.OnDisconnected(socket);
                            break;
                        default:
                            throw new AbandonedMutexException();
                    }
                }
            }
        }

    }
}
