using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Text;
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
                //接收消息 缓存区 需要合理设置,太小websocket接收缓存不足时，会自行断开后，过大会造成浪费大量内存
                var buffer = new byte[1024 * 1];
                var offset = 0;
                var free = buffer.Length;         
                StringBuilder msgString = new StringBuilder();
                //监听数据 
                while (socket.State == WebSocketState.Open)
                {
                    //再次发送消息的时候 会将上一次的消息一起发过来
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    free -= result.Count;
                   
                    //当缓存池不足时,接收大文件
                    if (free <= 0)
                    {
                        
                        offset += result.Count;
                        // Resize the outgoing buffer  每次增加1024
                        var newSize = buffer.Length + 1024 * 1;
                        //设置缓存池最大限制  不超过8k-2.5mb 之间  计算方法https://stackoverflow.com/questions/2811006/what-is-a-good-buffer-size-for-socket-programming
                        if (newSize > 262144)
                        {
                            //避免浪费 用户自己检查问题
                            await _Handler.SendMessage(socket,"数据传输失败！请检查网络");
                            break;
                        }
                        //获取新缓存池
                        var newBuffer = new byte[newSize];
                        Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                        buffer = newBuffer;
                        free = buffer.Length - offset;
                        //将接收到的数据一块一块切割保存
                        msgString.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        //是否完整接收数据 
                        if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Text)
                        {
                            await _Handler.Receive(socket, msgString.ToString());
                            //释放
                            msgString.Clear();
                        };
                    }
                    //当缓存池接收没问题时不用增加
                    if (result.EndOfMessage&&free>0) {
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
                    };
                }    
            }
        }
    }
}
