using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json.Linq;
using GlobalEnums;

// "D:\SteamLibrary\steamapps\common\Hollow Knight\BepInEx\LogOutput.log"

/*
 * How to use:
 *  - Run node C:\Users\Samuel\source\repos\HollowKnightFlagTracker\watcher.js
 *  - Start HK game. 
 *  - Pause the game to print $REPO/saveData.json
 *  - Save file differences are printed in $REPO/flag_output.json
 * */

[BepInPlugin("com.rainingchain.hollowknightflagtracker", "Hollow Knight Flag Tracker Mod", "1.0.0")]
public class HollowKnightFlagTracker : BaseUnityPlugin
{
    public static string ROOT = "C:\\Users\\Samuel\\source\\repos\\HollowKnightFlagTracker";
    public static string SAVE_DATA_OUTPUT = ROOT + "\\saveData.json";
    public static bool WAS_PAUSED = false;
    public static int COUNT = 0;

    public static ManualLogSource Log;
    public static GameManager gameManager;
    public static System.Timers.Timer timer;
    public void Awake()
    {
        Log = base.Logger;

        Logger.LogInfo("hollowknightmapper Plugin loaded and initialized.");
        Harmony.CreateAndPatchAll(typeof(HollowKnightFlagTracker), null);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), "Awake")]
    public static void AwakePostfix(GameManager __instance)
    {
        HollowKnightFlagTracker.Log.LogInfo("init start");

        gameManager = __instance;


        timer = new System.Timers.Timer();
        timer.Interval = 40;
        timer.AutoReset = true;
        timer.Elapsed += (a, b) => {
            if (COUNT < 1) {
                HollowKnightFlagTracker.Log.LogInfo("Update");
                COUNT = 1;
            }

            if (HollowKnightFlagTracker.gameManager.gameState == GameState.PAUSED)
            {
                if (WAS_PAUSED)
                    return;
                WAS_PAUSED = true;
                HollowKnightFlagTracker.Log.LogInfo("Saved");

                var saveDataStr = CreateSaveGameData();
                if (saveDataStr == null)
                {
                    HollowKnightFlagTracker.Log.LogInfo("saveData == null");
                    return;
                }
                File.WriteAllText(SAVE_DATA_OUTPUT, saveDataStr);

            } else
            {
                WAS_PAUSED = false;
            }
        };
        timer.Start();

        HollowKnightFlagTracker.Log.LogInfo("init end");
    }
    public static string CreateSaveGameData()
    {
        HollowKnightFlagTracker.gameManager.SaveLevelState();
        //PreparePlayerDataForSave.Invoke(HollowKnightFlagTracker.gameManager, new object[1] { HollowKnightFlagTracker.gameManager.profileID });
        //GameManager.SaveGame()
        string data = JsonUtility.ToJson((object)new SaveGameData(HollowKnightFlagTracker.gameManager.playerData, HollowKnightFlagTracker.gameManager.sceneData));
        return data;
    }
}
