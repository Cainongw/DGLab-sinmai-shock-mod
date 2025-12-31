using MelonLoader;
using MiniJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Timer = System.Timers.Timer;

namespace dg_sinmai_shcoker
{
    public class DGLabWebSocketServer
    {
        private WebSocketServer _server;
        private Timer _heartbeatTimer;
        private const int Port = 9999;
        private const int PunishmentDuration = 5; // 默认发送时间5秒
        private const int PunishmentTime = 1; // 默认一秒发送1次

        // 存储已连接的客户端
        public static ConcurrentDictionary<string, DGLabBehavior> Clients = new ConcurrentDictionary<string, DGLabBehavior>();
        // 存储绑定关系
        public static ConcurrentDictionary<string, string> Relations = new ConcurrentDictionary<string, string>();
        // 存储客户端计时器
        public static ConcurrentDictionary<string, Timer> ClientTimers = new ConcurrentDictionary<string, Timer>();

        public void Start()
        {
            _server = new WebSocketServer($"ws://0.0.0.0:{Port}");
            _server.AddWebSocketService<DGLabBehavior>("/");

            _server.Start();
            MelonLogger.Msg($"WebSocket 服务器已启动，监听端口: {Port}");

            // 启动心跳定时器
            _heartbeatTimer = new Timer(60 * 1000);
            _heartbeatTimer.Elapsed += SendHeartbeat;
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Start();
        }

        public void Stop()
        {
            _heartbeatTimer?.Stop();
            _server?.Stop();
            MelonLogger.Msg("WebSocket 服务器已停止");
        }

        private void SendHeartbeat(object sender, ElapsedEventArgs e)
        {
            if (Clients.Count > 0)
            {
                MelonLogger.Msg($"发送心跳消息: {DateTime.Now}");
                foreach (var kvp in Clients)
                {
                    var clientId = kvp.Key;
                    Relations.TryGetValue(clientId, out string targetId);
                    var heartbeat = new Dictionary<string, object>
                    {
                        { "type", "heartbeat" },
                        { "clientId", clientId },
                        { "targetId", targetId ?? "" },
                        { "message", "200" }
                    };
                    kvp.Value.SendMessage(Json.Serialize(heartbeat));
                }
            }
        }
    }

    public class DGLabBehavior : WebSocketBehavior
    {
        private string _clientId;

        protected override void OnOpen()
        {
            _clientId = Guid.NewGuid().ToString();
            MelonLogger.Msg($"新的 WebSocket 连接已建立，标识符为: {_clientId}");

            DGLabWebSocketServer.Clients.TryAdd(_clientId, this);

            // 发送绑定消息给客户端
            var bindMsg = new Dictionary<string, object>
            {
                { "type", "bind" },
                { "clientId", _clientId },
                { "message", "targetId" },
                { "targetId", "" }
            };
            Send(Json.Serialize(bindMsg));
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            MelonLogger.Msg($"收到消息: {e.Data}");

            Dictionary<string, object> data;
            try
            {
                data = Json.Deserialize(e.Data) as Dictionary<string, object>;
            }
            catch
            {
                SendError("403", "", "");
                return;
            }

            if (data == null || !data.ContainsKey("type") || !data.ContainsKey("clientId") ||
                !data.ContainsKey("message") || !data.ContainsKey("targetId"))
            {
                SendError("403", "", "");
                return;
            }

            string clientId = data["clientId"]?.ToString() ?? "";
            string targetId = data["targetId"]?.ToString() ?? "";
            string message = data["message"]?.ToString() ?? "";
            var type = data["type"];

            // 验证消息来源
            if (!DGLabWebSocketServer.Clients.ContainsKey(clientId) &&
                !DGLabWebSocketServer.Clients.ContainsKey(targetId))
            {
                SendError("404", clientId, targetId);
                return;
            }

            // 处理不同类型的消息
            if (type.ToString() == "bind")
            {
                HandleBind(clientId, targetId);
            }
            else if (type.ToString() == "1" || type.ToString() == "2" || type.ToString() == "3")
            {
                HandleStrengthAdjust(data, clientId, targetId, Convert.ToInt32(type));
            }
            else if (type.ToString() == "4")
            {
                HandleSetStrength(clientId, targetId, message);
            }
            else if (type.ToString() == "clientMsg")
            {
                HandleClientMsg(data, clientId, targetId, message);
            }
            else
            {
                HandleDefaultMsg(clientId, targetId, message, type.ToString());
            }
        }

        private void HandleBind(string clientId, string targetId)
        {
            if (DGLabWebSocketServer.Clients.ContainsKey(clientId) &&
                DGLabWebSocketServer.Clients.ContainsKey(targetId))
            {
                // 检查是否已有绑定关系
                if (!DGLabWebSocketServer.Relations.ContainsKey(clientId) &&
                    !DGLabWebSocketServer.Relations.ContainsKey(targetId) &&
                    !DGLabWebSocketServer.Relations.Values.Contains(clientId) &&
                    !DGLabWebSocketServer.Relations.Values.Contains(targetId))
                {
                    DGLabWebSocketServer.Relations.TryAdd(clientId, targetId);

                    var response = new Dictionary<string, object>
                    {
                        { "type", "bind" },
                        { "clientId", clientId },
                        { "targetId", targetId },
                        { "message", "200" }
                    };
                    string responseJson = Json.Serialize(response);

                    // 通知双方
                    Send(responseJson);
                    if (DGLabWebSocketServer.Clients.TryGetValue(clientId, out var client))
                    {
                        client.SendMessage(responseJson);
                    }
                }
                else
                {
                    SendBindError("400", clientId, targetId);
                }
            }
            else
            {
                SendBindError("401", clientId, targetId);
            }
        }

        private void HandleStrengthAdjust(Dictionary<string, object> data, string clientId, string targetId, int type)
        {
            if (!ValidateRelation(clientId, targetId)) return;

            if (DGLabWebSocketServer.Clients.TryGetValue(targetId, out var target))
            {
                int sendType = type - 1;
                int sendChannel = data.ContainsKey("channel") ? Convert.ToInt32(data["channel"]) : 1;
                int sendStrength = type >= 3 && data.ContainsKey("strength") ? Convert.ToInt32(data["strength"]) : 1;

                string msg = $"strength-{sendChannel}+{sendType}+{sendStrength}";
                var response = new Dictionary<string, object>
                {
                    { "type", "msg" },
                    { "clientId", clientId },
                    { "targetId", targetId },
                    { "message", msg }
                };
                target.SendMessage(Json.Serialize(response));
            }
        }

        private void HandleSetStrength(string clientId, string targetId, string message)
        {
            if (!ValidateRelation(clientId, targetId)) return;

            if (DGLabWebSocketServer.Clients.TryGetValue(targetId, out var target))
            {
                var response = new Dictionary<string, object>
                {
                    { "type", "msg" },
                    { "clientId", clientId },
                    { "targetId", targetId },
                    { "message", message }
                };
                target.SendMessage(Json.Serialize(response));
            }
        }

        private void HandleClientMsg(Dictionary<string, object> data, string clientId, string targetId, string message)
        {
            if (!ValidateRelation(clientId, targetId)) return;

            if (!data.ContainsKey("channel"))
            {
                var errorResponse = new Dictionary<string, object>
                {
                    { "type", "error" },
                    { "clientId", clientId },
                    { "targetId", targetId },
                    { "message", "406-channel is empty" }
                };
                Send(Json.Serialize(errorResponse));
                return;
            }

            string channel = data["channel"].ToString();

            if (DGLabWebSocketServer.Clients.TryGetValue(targetId, out var target))
            {
                int sendTime = data.ContainsKey("time") ? Convert.ToInt32(data["time"]) : 5;
                int totalSends = 1 * sendTime;
                int timeSpace = 1000;

                var sendData = new Dictionary<string, object>
                {
                    { "type", "msg" },
                    { "clientId", clientId },
                    { "targetId", targetId },
                    { "message", $"pulse-{message}" }
                };

                string timerKey = $"{clientId}-{channel}";

                // 检查是否存在正在发送的计时器
                if (DGLabWebSocketServer.ClientTimers.TryRemove(timerKey, out var existingTimer))
                {
                    existingTimer.Stop();
                    existingTimer.Dispose();

                    // 发送清除指令
                    string clearChannel = channel == "A" ? "1" : "2";
                    var clearData = new Dictionary<string, object>
                    {
                        { "type", "msg" },
                        { "clientId", clientId },
                        { "targetId", targetId },
                        { "message", $"clear-{clearChannel}" }
                    };
                    target.SendMessage(Json.Serialize(clearData));

                    // 延迟150ms后发送新消息
                    System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
                    {
                        StartSendingMessages(clientId, target, sendData, totalSends, timeSpace, timerKey);
                    });
                }
                else
                {
                    StartSendingMessages(clientId, target, sendData, totalSends, timeSpace, timerKey);
                }
            }
            else
            {
                SendError("404", clientId, targetId);
            }
        }

        private void StartSendingMessages(string clientId, DGLabBehavior target, Dictionary<string, object> sendData,
            int totalSends, int timeSpace, string timerKey)
        {
            string json = Json.Serialize(sendData);
            target.SendMessage(json);
            totalSends--;

            if (totalSends > 0)
            {
                var timer = new Timer(timeSpace);
                int remaining = totalSends;

                timer.Elapsed += (s, e) =>
                {
                    if (remaining > 0)
                    {
                        target.SendMessage(json);
                        remaining--;
                    }
                    if (remaining <= 0)
                    {
                        timer.Stop();
                        timer.Dispose();
                        DGLabWebSocketServer.ClientTimers.TryRemove(timerKey, out _);
                        SendMessage("发送完毕");
                    }
                };

                DGLabWebSocketServer.ClientTimers.TryAdd(timerKey, timer);
                timer.Start();
            }
        }

        private void HandleDefaultMsg(string clientId, string targetId, string message, string type)
        {
            if (!ValidateRelation(clientId, targetId)) return;

            if (DGLabWebSocketServer.Clients.TryGetValue(clientId, out var client))
            {
                var response = new Dictionary<string, object>
                {
                    { "type", type },
                    { "clientId", clientId },
                    { "targetId", targetId },
                    { "message", message }
                };
                client.SendMessage(Json.Serialize(response));
            }
            else
            {
                SendError("404", clientId, targetId);
            }
        }

        private bool ValidateRelation(string clientId, string targetId)
        {
            if (!DGLabWebSocketServer.Relations.TryGetValue(clientId, out string boundTarget) ||
                boundTarget != targetId)
            {
                SendBindError("402", clientId, targetId);
                return false;
            }
            return true;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            MelonLogger.Msg($"WebSocket 连接已关闭: {_clientId}");

            DGLabWebSocketServer.Clients.TryRemove(_clientId, out _);

            // 处理绑定关系
            foreach (var kvp in DGLabWebSocketServer.Relations)
            {
                if (kvp.Key == _clientId)
                {
                    // 通知 APP
                    if (DGLabWebSocketServer.Clients.TryGetValue(kvp.Value, out var appClient))
                    {
                        var breakMsg = new Dictionary<string, object>
                        {
                            { "type", "break" },
                            { "clientId", _clientId },
                            { "targetId", kvp.Value },
                            { "message", "209" }
                        };
                        appClient.SendMessage(Json.Serialize(breakMsg));
                        appClient.Close();
                    }
                    DGLabWebSocketServer.Relations.TryRemove(kvp.Key, out _);
                }
                else if (kvp.Value == _clientId)
                {
                    // 通知网页
                    if (DGLabWebSocketServer.Clients.TryGetValue(kvp.Key, out var webClient))
                    {
                        var breakMsg = new Dictionary<string, object>
                        {
                            { "type", "break" },
                            { "clientId", kvp.Key },
                            { "targetId", _clientId },
                            { "message", "209" }
                        };
                        webClient.SendMessage(Json.Serialize(breakMsg));
                        webClient.Close();
                    }
                    DGLabWebSocketServer.Relations.TryRemove(kvp.Key, out _);
                }
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            MelonLogger.Error($"WebSocket 异常: {e.Message}");

            foreach (var kvp in DGLabWebSocketServer.Relations)
            {
                if (kvp.Key == _clientId && DGLabWebSocketServer.Clients.TryGetValue(kvp.Value, out var appClient))
                {
                    var errorMsg = new Dictionary<string, object>
                    {
                        { "type", "error" },
                        { "clientId", _clientId },
                        { "targetId", kvp.Value },
                        { "message", "500" }
                    };
                    appClient.SendMessage(Json.Serialize(errorMsg));
                }
                else if (kvp.Value == _clientId && DGLabWebSocketServer.Clients.TryGetValue(kvp.Key, out var webClient))
                {
                    var errorMsg = new Dictionary<string, object>
                    {
                        { "type", "error" },
                        { "clientId", kvp.Key },
                        { "targetId", _clientId },
                        { "message", e.Message }
                    };
                    webClient.SendMessage(Json.Serialize(errorMsg));
                }
            }
        }

        public void SendMessage(string message)
        {
            if (this != null && this.ReadyState == WebSocketState.Open)
            {
                Send(message);
            }
        }

        private void SendError(string code, string clientId, string targetId)
        {
            var response = new Dictionary<string, object>
            {
                { "type", "msg" },
                { "clientId", clientId },
                { "targetId", targetId },
                { "message", code }
            };
            Send(Json.Serialize(response));
        }

        private void SendBindError(string code, string clientId, string targetId)
        {
            var response = new Dictionary<string, object>
            {
                { "type", "bind" },
                { "clientId", clientId },
                { "targetId", targetId },
                { "message", code }
            };
            Send(Json.Serialize(response));
        }
    }
}