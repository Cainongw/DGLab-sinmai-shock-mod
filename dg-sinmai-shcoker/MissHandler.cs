using MelonLoader;
using MelonLoader.Utils;
using MiniJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Manager.GameManager;

namespace dg_sinmai_shcoker
{
    public class MissHandler
    {
        public static void OnMissDetected()
        {

            MelonLogger.Msg("Miss Detected");
            // 检查是否已绑定设备
            if (!GlobalVars.isBinded)
            {
                MelonLogger.Error("未绑定设备，请先扫描二维码进行绑定。");
                return;
            }
            if (ConfigManager.Instance.type == "single")
            {
                string channel = ConfigManager.Instance.single_channel;
                string channel_number = "";
                if (channel == "Both")
                {
                    var both_payload = new Dictionary<string, object>
                    {
                        { "type", 4 },
                        { "strength", ConfigManager.Instance.single_intensity },
                        { "message", "set channel" },
                        { "channel", 1 },
                        { "clientId", GlobalVars.clientId },
                        { "targetId", GlobalVars.targetId }
                    };
                    string both_json = Json.Serialize(both_payload);
                    WebsocketHandler.Send(both_json);
                    both_payload["channel"] = 2;
                    string both_json2 = Json.Serialize(both_payload);
                    WebsocketHandler.Send(both_json2);
                }
                else if (channel == "B")
                {
                    channel_number = "2";
                }
                else if (channel == "A")
                {
                    channel_number = "1";
                }
                else
                {
                    MelonLogger.Error("未配置正确的通道，请检查配置文件。");
                    return;
                }
                var payload = new Dictionary<string, object>
                    {
                        { "type", 1 },
                        { "strength", ConfigManager.Instance.single_intensity },
                        { "message", "set channel" },
                        { "channel", channel_number },
                        { "clientId", GlobalVars.clientId },
                        { "targetId", GlobalVars.targetId }
                    };
                string json = Json.Serialize(payload);
                WebsocketHandler.Send(json);
            }
            else if (ConfigManager.Instance.type == "ramp")
            {
            }
            else
            {
                MelonLogger.Warning("未配置正确的发送类型，请检查配置文件。");

            }
        }
    }
}

