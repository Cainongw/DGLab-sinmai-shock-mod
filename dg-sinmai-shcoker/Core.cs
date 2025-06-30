using HarmonyLib;
using MAI2System;
using MelonLoader;
using MelonLoader.Utils;
using Monitor.Game;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Tomlet;
using UnityEngine;

[assembly: MelonInfo(typeof(dg_sinmai_shcoker.Main), "DGShockMod", "1.0.0", "Caiw")]
[assembly: MelonGame(null, null)]

namespace dg_sinmai_shcoker
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ConfigManager.Load();
            MelonLogger.Msg("DG Shock Miss Mod 已加载！");
            WebsocketHandler.Connect();
        }
        public override void OnUpdate()
        {
            // 按下 M 键模拟 Miss
            if (Input.GetKeyDown(KeyCode.M))
            {
                MelonLogger.Msg("手动触发 Miss！");
                MissHandler.OnMissDetected(); 
            }
        }
    };

            [HarmonyLib.HarmonyPatch(typeof(Manager.JudgeResultSt), "UpdateScore")]
        public class UpdateScorePatch
        {
            public static void Postfix(Manager.JudgeResultSt __instance)
            {
                if (__instance.Deluxe == 0U) // Miss判定
                {
                 MissHandler.OnMissDetected();
            }
            }
        }

}
