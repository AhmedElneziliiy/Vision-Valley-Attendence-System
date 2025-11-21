using CoreProject.Context;
using CoreProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    /// <summary>
    /// WebSocket handler for ESP32 lamp devices
    /// Manages connections, state changes, and telemetry from lamp devices
    /// </summary>
    public class LampWebSocketHandler
    {
        private readonly ILogger<LampWebSocketHandler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        // Store active WebSocket connections: DeviceID -> WebSocket
        private static readonly ConcurrentDictionary<string, WebSocket> _activeConnections = new();

        public LampWebSocketHandler(
            ILogger<LampWebSocketHandler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Handle incoming WebSocket connection from ESP32
        /// </summary>
        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[4096];
            string? deviceId = null;

            try
            {
                _logger.LogInformation("New WebSocket connection established");

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by client",
                            CancellationToken.None);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation("Received WebSocket message: {Message}", message);

                        deviceId = await HandleMessageAsync(webSocket, message, deviceId);
                    }
                }
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogWarning(wsEx, "WebSocket error for device {DeviceId}", deviceId ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket for device {DeviceId}", deviceId ?? "Unknown");
            }
            finally
            {
                // Clean up connection
                if (!string.IsNullOrEmpty(deviceId))
                {
                    _activeConnections.TryRemove(deviceId, out _);
                    await UpdateLampConnectionStatusAsync(deviceId, false);
                    _logger.LogInformation("Device {DeviceId} disconnected", deviceId);
                }

                if (webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Handler exit",
                        CancellationToken.None);
                }
                webSocket.Dispose();
            }
        }

        /// <summary>
        /// Handle incoming JSON message from ESP32
        /// Returns the updated device ID if registered
        /// </summary>
        private async Task<string?> HandleMessageAsync(WebSocket webSocket, string message, string? deviceId)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement))
                {
                    _logger.LogWarning("Message missing 'type' field");
                    return deviceId;
                }

                var messageType = typeElement.GetString();

                // Get device_id from message
                if (root.TryGetProperty("device_id", out var deviceIdElement))
                {
                    var msgDeviceId = deviceIdElement.GetInt32().ToString();

                    // On first hello message, register the connection
                    if (messageType == "hello" && string.IsNullOrEmpty(deviceId))
                    {
                        deviceId = msgDeviceId;
                        _activeConnections[deviceId] = webSocket;
                        await UpdateLampConnectionStatusAsync(deviceId, true);
                        _logger.LogInformation("Device {DeviceId} registered", deviceId);
                    }
                }

                // Handle different message types
                switch (messageType)
                {
                    case "hello":
                        _logger.LogInformation("Device {DeviceId} says hello", deviceId);
                        // Device connected, connection status already updated above
                        break;

                    case "ack":
                        await HandleAckMessageAsync(root);
                        break;

                    case "telemetry":
                        await HandleTelemetryMessageAsync(root);
                        break;

                    default:
                        _logger.LogWarning("Unknown message type: {Type}", messageType);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON message: {Message}", message);
            }

            return deviceId;
        }

        /// <summary>
        /// Handle acknowledgment message from ESP32 after state change
        /// </summary>
        private async Task HandleAckMessageAsync(JsonElement root)
        {
            if (!root.TryGetProperty("device_id", out var deviceIdElement))
                return;

            var deviceId = deviceIdElement.GetInt32().ToString();
            var applied = root.TryGetProperty("applied", out var appliedElement) && appliedElement.GetBoolean();
            var power = root.TryGetProperty("power", out var powerElement) ? powerElement.GetString() : null;

            _logger.LogInformation("ACK from device {DeviceId}: applied={Applied}, power={Power}",
                deviceId, applied, power);

            // Update lamp state in database if applied successfully
            if (applied && !string.IsNullOrEmpty(power))
            {
                var newState = power == "ON" ? 1 : 0;
                await UpdateLampStateAsync(deviceId, newState);
            }
        }

        /// <summary>
        /// Handle telemetry message from ESP32
        /// </summary>
        private async Task HandleTelemetryMessageAsync(JsonElement root)
        {
            if (!root.TryGetProperty("device_id", out var deviceIdElement))
                return;

            var deviceId = deviceIdElement.GetInt32().ToString();
            var reason = root.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : "unknown";
            var power = root.TryGetProperty("power", out var powerElement) ? powerElement.GetString() : "UNKNOWN";

            _logger.LogInformation("Telemetry from device {DeviceId}: reason={Reason}, power={Power}",
                deviceId, reason, power);

            // Update database with current state
            if (!string.IsNullOrEmpty(power) && power != "UNKNOWN")
            {
                var newState = power == "ON" ? 1 : 0;
                await UpdateLampStateAsync(deviceId, newState);
            }
        }

        /// <summary>
        /// Send state change command to ESP32
        /// </summary>
        public async Task<bool> SendStateChangeAsync(string deviceId, bool turnOn)
        {
            if (!_activeConnections.TryGetValue(deviceId, out var webSocket))
            {
                _logger.LogWarning("Device {DeviceId} is not connected via WebSocket", deviceId);
                return false;
            }

            if (webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("WebSocket for device {DeviceId} is not open", deviceId);
                _activeConnections.TryRemove(deviceId, out _);
                return false;
            }

            try
            {
                var command = new
                {
                    type = "state_change",
                    device_id = int.Parse(deviceId),
                    power = turnOn ? "ON" : "OFF"
                };

                var json = JsonSerializer.Serialize(command);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);

                _logger.LogInformation("Sent state_change to device {DeviceId}: power={Power}",
                    deviceId, turnOn ? "ON" : "OFF");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send state_change to device {DeviceId}", deviceId);
                _activeConnections.TryRemove(deviceId, out _);
                return false;
            }
        }

        /// <summary>
        /// Update lamp connection status in database
        /// </summary>
        private async Task UpdateLampConnectionStatusAsync(string deviceId, bool isConnected)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var lamp = await context.Lamps
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.DeviceID == deviceId);

            if (lamp != null)
            {
                lamp.IsConnected = isConnected;
                lamp.UpdatedAt = DateTime.UtcNow;

                if (isConnected)
                {
                    lamp.LastConnectionTime = DateTime.UtcNow;
                }
                else
                {
                    lamp.LastDisconnectionTime = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Updated connection status for device {DeviceId}: {Status}",
                    deviceId, isConnected ? "Connected" : "Disconnected");
            }
        }

        /// <summary>
        /// Update lamp state in database
        /// </summary>
        private async Task UpdateLampStateAsync(string deviceId, int newState)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var lamp = await context.Lamps
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.DeviceID == deviceId);

            if (lamp != null && lamp.CurrentState != newState)
            {
                lamp.CurrentState = newState;
                lamp.LastStateChange = DateTime.UtcNow;
                lamp.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
                _logger.LogInformation("Updated state for device {DeviceId}: {State}",
                    deviceId, newState == 1 ? "ON" : "OFF");
            }
        }

        /// <summary>
        /// Get all connected device IDs
        /// </summary>
        public static string[] GetConnectedDevices()
        {
            return _activeConnections.Keys.ToArray();
        }

        /// <summary>
        /// Check if a specific device is connected
        /// </summary>
        public static bool IsDeviceConnected(string deviceId)
        {
            return _activeConnections.ContainsKey(deviceId);
        }
    }
}
