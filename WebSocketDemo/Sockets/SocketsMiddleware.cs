using Microsoft.AspNetCore.Http;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo
{
    public class SocketsMiddleware
    {
        private SocketsHandler _Handler { get; }
        //如果不是websocket请求
        private readonly RequestDelegate _next;
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
                //接收消息 使用默认小缓存池 需要合理设置,太小websocket接收缓存不足时，会自行断开后，过大会造成浪费大量内存
                //var buffer = new byte[1024 * 4];
                //var offset = 0;
                //缓存池
                var samePool = ArrayPool<byte>.Shared;
                int newSize = 0;
                // var newBuffer = samePool.Rent(buffer.Length);
                var newBuffer = samePool.Rent(1024 * 4);
                //缓存池是否溢出
                var free = 1024*4;
                StringBuilder msgString = new StringBuilder();
                //监听数据 
                while (socket.State == WebSocketState.Open)
                {
                   
                    //监听消息 每次传输的大小为4096
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(newBuffer), CancellationToken.None);
                    //初始化 
                    free -= result.Count;
                    //当缓存池接收没问题时 直接发送消息
                    if (result.EndOfMessage && free > 0)
                    {
                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                await _Handler.Receive(socket, result, newBuffer);
                                free = newBuffer.Length;
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
                    //当缓存池不足时,对缓存池进行扩容
                    if (free <= 0)
                    {
                        // 每次消息的长度总和，超过将不发送
                        newSize += newBuffer.Length;
                        //超出缓存池最大限制  8k-2.5mb 之间  计算方法https://stackoverflow.com/questions/2811006/what-is-a-good-buffer-size-for-socket-programming
                        if (newSize > 15000 && result.EndOfMessage)
                        {
                            //用户自己检查问题 
                            await _Handler.SendMessage(socket, "数据传输失败！请检查网络!");
                            //避免浪费 
                            msgString.Clear();
                            //将使用的放回共享缓存池
                            samePool.Return(newBuffer,true);
                            newSize = 0;
                            free =newBuffer.Length;
                            continue;
                        }
                        //深拷贝 buffer效率更高 但要注意计算机字节序 可能会导致数据没有拷贝完整  https://blog.csdn.net/qq826364410/article/details/79729727
                        // Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                        Buffer.BlockCopy(newBuffer, 0,newBuffer,0,newBuffer.Length);
                       // buffer = newBuffer;
                        //将接收到的数据一块一块切割保存
                        msgString.Append(Encoding.UTF8.GetString(newBuffer, 0, result.Count));
                        //是否完整接收数据 
                        if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Text)
                        {
                            await _Handler.Receive(socket, msgString.ToString());
                            //将使用的放回共享缓存池
                            samePool.Return(newBuffer, true);
                            msgString.Clear();
                            newSize = 0;
                            //重置
                            free = newBuffer.Length;
                            continue;
                        };
                    }
                 
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
