using System;
using System.IO;
using System.Threading;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using WebSocketSharp;

[assembly: MelonInfo(typeof(DGShockMod.Main), "DGShockMod", "1.0.0", "Caiw")]
[assembly: MelonGame(null, null)]


namespace DGShockMod
{
    public class Main : MelonMod
    {
        private static WebSocket ws;
        private static string wsUrl = "";
        private static string sendContent = "";
        private static bool isConnecting = false;
        private static readonly object wsLock = new object();

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("DG Shock Miss Mod 已加载！");
            LoadConfig();
            ConnectWebSocketWithRetry();
        }

        private static void LoadConfig()
        {
            string configPath = Path.Combine(MelonEnvironment.MelonBaseDirectory, "DGShockMod.toml");
            if (!File.Exists(configPath))
            {
                MelonLogger.Warning("未找到配置文件 DGShockMod.toml！");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ws_url"))
                        wsUrl = line.Split('=')[1].Trim().Trim('"');
                    else if (line.StartsWith("send_content"))
                    {
                        // 读取多行 JSON 内容
                        int start = Array.IndexOf(lines, line);
                        sendContent = "";
                        for (int i = start + 1; i < lines.Length; i++)
                        {
                            if (lines[i].Trim() == "'''") break;
                            sendContent += lines[i] + "\n";
                        }
                    }
                }
                MelonLogger.Msg($"配置加载成功：ws_url={wsUrl}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("加载配置文件出错: " + ex.Message);
            }
        }

        private static void ConnectWebSocketWithRetry()
        {
            if (string.IsNullOrEmpty(wsUrl))
            {
                MelonLogger.Warning("ws_url 为空，WebSocket 不会连接。");
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (true)
                {
                    lock (wsLock)
                    {
                        if (ws != null)
                        {
                            try
                            {
                                ws.Close();
                            }
                            catch { }
                            ws = null;
                        }

                        ws = new WebSocket(wsUrl);

                        ws.OnOpen += (sender, e) =>
                        {
                            MelonLogger.Msg("WebSocket 已连接！");
                            isConnecting = false;
                        };

                        ws.OnClose += (sender, e) =>
                        {
                            MelonLogger.Warning($"WebSocket 已关闭，原因: {e.Reason}，{e.Code}");
                            isConnecting = false;
                        };

                        ws.OnError += (sender, e) =>
                        {
                            MelonLogger.Error("WebSocket 错误: " + e.Message);
                            isConnecting = false;
                        };

                        try
                        {
                            MelonLogger.Msg("尝试连接 WebSocket...");
                            isConnecting = true;
                            ws.Connect();
                            if (ws.IsAlive)
                            {
                                // 成功连接，跳出重连循环
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error("连接 WebSocket 异常: " + ex.Message);
                        }
                    }

                    MelonLogger.Msg("WebSocket 连接失败，5秒后重试...");
                    Thread.Sleep(5000);
                }
            });
        }

        private static int MissCounter = 0;

        private static void OnMissDetected()
        {
            MissCounter++;
            MelonLogger.Msg($"Miss 触发！当前 Miss 次数: {MissCounter}");

            lock (wsLock)
            {
                if (ws != null && ws.IsAlive)
                {
                    ws.Send(sendContent);
                    MelonLogger.Msg("已通过 WebSocket 发送 JSON 内容。");
                }
                else
                {
                    MelonLogger.Warning("WebSocket 未连接，无法发送 JSON！");
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(Manager.JudgeResultSt), "UpdateScore")]
        public class UpdateScorePatch
        {
            public static void Postfix(Manager.JudgeResultSt __instance)
            {
                if (__instance.Deluxe == 0U) // Miss判定
                {
                    OnMissDetected();
                }
            }
        }
    }
}