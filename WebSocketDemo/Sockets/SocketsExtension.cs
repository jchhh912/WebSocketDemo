using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebSocketDemo.Sockets
{
    public static class SocketsExtension
    {
        //注入
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<SocketsManager>();
            var exportedTypes = Assembly.GetEntryAssembly()?.ExportedTypes;
            if (exportedTypes == null) return services;

            foreach (var type in exportedTypes)
            {
                if (type.GetTypeInfo().BaseType==typeof(SocketsHandler))
                {
                    services.AddSingleton(type);
                }
            }
            return services;
        }
        public static IApplicationBuilder MapSockets(this IApplicationBuilder app, PathString path, SocketsHandler sockets)
        {
            return app.Map(path,x=>x.UseMiddleware<SocketsMiddleware>(sockets));
        }
    }
}
