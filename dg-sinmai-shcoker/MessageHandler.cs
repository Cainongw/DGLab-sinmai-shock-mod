using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MelonLoader;
using MiniJSON;
using WebSocketSharp;
namespace dg_sinmai_shcoker
{

    public class MessageHandler
    {
        
        public void Message_handler(string msg)
        {
            MelonLogger.Msg($"Received message: {msg}");
            var dict = Json.Deserialize(msg) as Dictionary<string, object>;
            if (!GlobalVars.isBinded && dict["type"].ToString() == "bind" && dict["message"].ToString()== "targetId") //not binded， gen code
            {
                string ws_url = ConfigManager.Instance.ws_url;
                try
                {
                    GlobalVars.clientId = dict["clientId"].ToString();
                    MelonLogger.Msg("扫描二维码以连接..");
                    QRGenerater.QRGen(ws_url, GlobalVars.clientId);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"输出二维码异常: {ex}");
                }
            }
            if (dict["type"].ToString() == "bind" && !dict["targetId"].ToString().IsNullOrEmpty() && dict["message"].ToString() == "200")
            {
                MelonLogger.Msg("绑定成功！设备uuid " + dict["targetId"].ToString());
                GlobalVars.isBinded = true;
                GlobalVars.targetId = dict["targetId"].ToString();
            }
        }
    }
}

