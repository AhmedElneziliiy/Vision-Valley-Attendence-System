using MvcCoreProject.Middleware;

namespace MvcCoreProject.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configure WebSocket support for ESP32 lamp connections
        /// </summary>
        public static IApplicationBuilder UseWebSocketConfiguration(this IApplicationBuilder app)
        {
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            };
            app.UseWebSockets(webSocketOptions);

            return app;
        }

        /// <summary>
        /// Configure Swagger UI for API documentation
        /// </summary>
        public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vision Valley Attendance API v1");
                options.RoutePrefix = "api/swagger";
                options.DocumentTitle = "Vision Valley Attendance API";
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
            });

            return app;
        }

        /// <summary>
        /// Add middleware to read JWT from cookie and add to Authorization header
        /// </summary>
        public static IApplicationBuilder UseJwtCookieMiddleware(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["VisionValley_JWT"];
                if (!string.IsNullOrEmpty(token) && !context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Request.Headers.Append("Authorization", $"Bearer {token}");
                }
                await next();
            });

            return app;
        }

        /// <summary>
        /// Add WebSocket middleware for lamp device connections
        /// </summary>
        public static IApplicationBuilder UseLampWebSocket(this IApplicationBuilder app)
        {
            app.UseMiddleware<WebSocketMiddleware>();

            return app;
        }
    }
}
