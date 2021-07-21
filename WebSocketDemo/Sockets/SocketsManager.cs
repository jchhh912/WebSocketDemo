using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo
{
    public class SocketsManager
    {
        private static readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();
        /// <summary>
        /// 获取所有集合
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, WebSocket> GetAllConnections() 
        {
            return _connections;
        }
        /// <summary>
        /// 获取指定Id WebSocket
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public WebSocket GetSocketId(string id) 
        {
            return _connections.FirstOrDefault(x=>x.Key==id).Value;
        }
        /// <summary>
        /// 获取指定websocket的id
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public string GetId(WebSocket socket) 
        {
            return _connections.FirstOrDefault(x=>x.Value==socket).Key;        
        }
        /// <summary>
        /// 删除指定socket
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task RemoveSocketAsync(string id)
        {
            _connections.TryRemove(id,out var socket);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure,"Closed Connection",CancellationToken.None);     
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="socket"></param>
        public void AddSocket(WebSocket socket)
        {
            _connections.TryAdd(CreateId(), socket);
        }
        private string CreateId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
