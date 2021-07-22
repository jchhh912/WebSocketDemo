

using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketDemo.Sockets;

namespace WebSocketDemo
{
    /// <summary>
    /// WebSocket子类 管理全局消息
    /// </summary>
    public class WebSocketMessageHandler : SocketsHandler
    {
        public WebSocketMessageHandler(SocketsManager sockets) : base(sockets)
        {
        }
        //连接
        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            var socketId = Sockets.GetId(socket);
            await SendMessageToAll($"{socketId}已加入");
        }
        //断开
        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Sockets.GetId(socket);
            await base.OnDisconnected(socket);
            await SendMessageToAll($"{socketId}离开了");
        }
        //正常数据处理
        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {

            var msgString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Message msg = JsonConvert.DeserializeObject<Message>(msgString);
            msg.FromID = Sockets.GetId(socket);
            switch (msg.Action)
            {
                case "join":
                    //重新连接
                    await OnConnected(socket);
                    break;
                case "leave":
                    //断开
                    await OnDisconnected(socket);
                    break;
                case "Public":
                    //公开消息
                    await SendMessageToAll($"{msg.Name} 发送了公共消息：{msg.Msg}");
                    break;
                case "private":
                    //私聊消息
                    await SendMessage(msg.ToID, $"来自{msg.FromID}的消息:{msg.Msg}");
                    break;
                case "info":
                    //查询信息
                    break;

                default:
                    break;
            }
        }
        //传输大数据
        public override async Task Receive(WebSocket socket, string result)
        {

            Message msg = JsonConvert.DeserializeObject<Message>(result);
            msg.FromID = Sockets.GetId(socket);
            switch (msg.Action)
            {
                case "join":
                    //重新连接
                    await OnConnected(socket);
                    break;
                case "leave":
                    //断开
                    await OnDisconnected(socket);
                    break;
                case "Public":
                    //公开消息
                    await SendMessageToAll($"{msg.Name} 发送了公共消息：{msg.Msg}");
                    break;
                case "private":
                    //私聊消息
                    await SendMessage(msg.ToID, $"来自{msg.FromID}的消息:{msg.Msg}");
                    break;
                case "info":
                    //查询信息
                    break;

                default:
                    break;
            }
        }
    }
}
