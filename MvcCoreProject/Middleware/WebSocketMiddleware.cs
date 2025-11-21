using CoreProject.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MvcCoreProject.Middleware
{
    /// <summary>
    /// Middleware to handle WebSocket connections for ESP32 lamp devices
    /// Listens on /ws endpoint
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LampWebSocketHandler _handler;

        public WebSocketMiddleware(RequestDelegate next, LampWebSocketHandler handler)
        {
            _next = next;
            _handler = handler;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a WebSocket request to /ws endpoint
            if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
            {
                // Accept the WebSocket connection
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                // Handle the WebSocket connection
                await _handler.HandleWebSocketAsync(webSocket);
            }
            else
            {
                // Not a WebSocket request, pass to next middleware
                await _next(context);
            }
        }
    }
}
