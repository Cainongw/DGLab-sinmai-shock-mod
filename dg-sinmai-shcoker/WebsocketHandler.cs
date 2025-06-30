using MelonLoader;
using MelonLoader.TinyJSON;
using MiniJSON;
using QRCoder;
using System;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace dg_sinmai_shcoker
{
    public static class WebsocketHandler
    {
        public static WebSocket Socket { get; private set; }
        public static void Connect()
        {
            string ws_url = ConfigManager.Instance.ws_url;
            if (string.IsNullOrEmpty(ws_url))
            {
                MelonLogger.Error("WebSocket URL 未配置或为空！");
                return;
            }

            Socket = new WebSocket(ws_url);

            Socket.OnOpen += (sender, e) =>
            {
                MelonLogger.Msg("WebSocket connected");
            };

            Socket.OnMessage += (sender, e) =>
            {
                try
                {
                    MessageHandler m = new MessageHandler();
                    m.Message_handler(e.Data);
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"OnMessage总异常: {ex}");
                }
            };

            Socket.OnClose += (sender, e) =>
            {
                MelonLogger.Msg($"WebSocket closed: Code={e.Code}, Reason={e.Reason}");
            };

            Socket.OnError += (sender, e) =>
            {
                MelonLogger.Error($"WebSocket error: {e.Message}");
            };

            // 异步连接，避免阻塞游戏主线程
            Task.Run(() => Socket.Connect());
        }
        public static void Send(string message)
        {
            if (Socket != null && Socket.IsAlive)
            {
                MelonLogger.Msg($"Sending message: {message}");
                Socket.Send(message);
            }
            else
            {
                MelonLogger.Msg("WebSocket 未连接，无法发送");
            }
        }
    }
    }
    



