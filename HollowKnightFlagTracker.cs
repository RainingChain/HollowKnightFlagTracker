using Modding;
using Modding.Converters;
using System.Reflection;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using System;
using System.IO;
using GlobalEnums;

namespace HollowKnightFlagTracker {
    public class HollowKnightFlagTracker : Mod
    {
        public static string ROOT = "C:\\Users\\Samuel\\source\\repos\\HollowKnightFlagTracker";
        public static string SAVE_DATA_OUTPUT = ROOT + "\\saveData.json";
        public static bool WAS_PAUSED = false;
        public static int COUNT = 0;

        public static System.Timers.Timer timer;

        public static string CreateSaveGameData()
        {
            GameManager.instance.SaveLevelState();
            //PreparePlayerDataForSave.Invoke(HollowKnightFlagTracker.gameManager, new object[1] { HollowKnightFlagTracker.gameManager.profileID });
            //GameManager.SaveGame()
            string data = JsonUtility.ToJson((object)new SaveGameData(GameManager.instance.playerData, GameManager.instance.sceneData));
            return data;
        }


        public HollowKnightFlagTracker(): base ("HollowKnightFlagTracker") { 

        }

        public override void Initialize() {
            Log("Initializing");

            timer = new System.Timers.Timer();
            timer.Interval = 40;
            timer.AutoReset = true;
            timer.Elapsed += (a, b) => {
                if (COUNT < 1)
                {
                    //HollowKnightFlagTracker.Log.LogInfo("Update");
                    COUNT = 1;
                }

                if (GameManager.instance.gameState == GameState.PAUSED)
                {
                    if (WAS_PAUSED)
                        return;
                    WAS_PAUSED = true;
                    //HollowKnightFlagTracker.Log.LogInfo("Saved");

                    var saveDataStr = CreateSaveGameData();
                    if (saveDataStr == null)
                    {
                        //HollowKnightFlagTracker.Log.LogInfo("saveData == null");
                        return;
                    }
                    File.WriteAllText(SAVE_DATA_OUTPUT, saveDataStr);

                }
                else
                {
                    WAS_PAUSED = false;
                }
            };
            timer.Start();

            Log("Initialized");
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();
    }
}