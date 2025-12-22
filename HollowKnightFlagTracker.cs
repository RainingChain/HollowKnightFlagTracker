using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json.Linq;

// "D:\SteamLibrary\steamapps\common\Hollow Knight\BepInEx\LogOutput.log"

/*
 *  Press F2 to print differences
 * */

[BepInPlugin("com.rainingchain.hollowknightflagtracker", "Hollow Knight Flag Tracker Mod", "1.0.0")]
public class HollowKnightFlagTracker : BaseUnityPlugin
{
    public static ManualLogSource Log;
    public static GameManager gameManager;
    public static System.Timers.Timer timer;
    private void Awake()
    {
        Log = base.Logger;

        Logger.LogInfo("hollowknightmapper Plugin loaded and initialized.");
        Harmony.CreateAndPatchAll(typeof(HollowKnightFlagTracker), null);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), "Awake")]
    private static void AwakePostfix(GameManager __instance)
    {
        gameManager = __instance;

        HollowKnightFlagTracker.Log.LogInfo("gameManager.gameObject.AddComponent<GameManagerModdedComponent>");
        //gameManager.CreateSaveGameData(1);

        timer = new System.Timers.Timer();
        timer.Interval = 40;
        timer.AutoReset = true;
        timer.Elapsed += (a, b) => {
            File.AppendAllText("c:\\silksong_map\\flag_output.json", DateTime.Now.ToString() + "\n");
            HollowKnightFlagTracker.Log.LogInfo("timer end2");

            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyUp(KeyCode.D))
            {
                HollowKnightFlagTracker.Log.LogInfo("Save");
                var saveDataStr = CreateSaveGameData();
                if (saveDataStr == null)
                {
                    HollowKnightFlagTracker.Log.LogInfo("saveData == null");
                    return;
                }

                if (LastSaveDataStr == "")
                {
                    LastSaveDataStr = saveDataStr;
                    return;
                }

                HollowKnightFlagTracker.Log.LogInfo("timer end4");
                JObject LastSaveData = JObject.Parse(LastSaveDataStr);
                JObject saveData = JObject.Parse(saveDataStr);

                if (LastSaveData == null || saveData == null)
                    return;

                HollowKnightFlagTracker.Log.LogInfo("CompareSaveDatas");
                var logs = CompareSaveDatas(LastSaveData, saveData);
                if (logs.Count > 0)
                {
                    File.AppendAllText(filePath, DateTime.Now.ToString() + "\n");
                    foreach (var log in logs)
                    {
                        HollowKnightFlagTracker.Log.LogInfo(log);
                        File.AppendAllText(filePath, log + "\n");
                    }
                    File.AppendAllText(filePath, "\n");
                }

            }

            HollowKnightFlagTracker.Log.LogInfo("timer end3");
        };
        timer.Start();

        HollowKnightFlagTracker.Log.LogInfo("timer end");
    }

    static string filePath = "c:\\silksong_map\\flag_output.json";

    static string LastSaveDataStr = "";

    //private static MethodInfo PreparePlayerDataForSave = AccessTools.Method(typeof(GameManager), "PreparePlayerDataForSave", new Type[1] { typeof(int) });

    private static string CreateSaveGameData()
    {
        HollowKnightFlagTracker.gameManager.SaveLevelState();
        //PreparePlayerDataForSave.Invoke(HollowKnightFlagTracker.gameManager, new object[1] { HollowKnightFlagTracker.gameManager.profileID });
        //GameManager.SaveGame()
        string data = JsonUtility.ToJson((object)new SaveGameData(HollowKnightFlagTracker.gameManager.playerData, HollowKnightFlagTracker.gameManager.sceneData));
        return data;
    }

    public static void Update2()
    {
        HollowKnightFlagTracker.Log.LogInfo("Update");

        if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyUp(KeyCode.I))
        {
            PlayerData.instance.isInvincible = true;
            PlayerData.instance.infiniteAirJump = true;
        }
        if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyUp(KeyCode.U))
        {
            PlayerData.instance.isInvincible = false;
            PlayerData.instance.infiniteAirJump = false;
        }


    }

    private static List<string> CompareSaveDatas(JObject oldJson, JObject newJson)
    {
        List<string> list = new List<string>();
        list.AddRange(CompareSaveDatas_GeoInt(oldJson, newJson, true));
        list.AddRange(CompareSaveDatas_GeoInt(oldJson, newJson, false));
        list.AddRange(CompareSaveDatas_Bool(oldJson, newJson));


        JObject playerDataDiff = new JObject();
        JObjectComparer.FindDeepDifferences(
            oldJson.GetValue("playerData").ToObject<JObject>(),
            newJson.GetValue("playerData").ToObject<JObject>(),
        "playerData", GetIgnoredProps(), list);

        return list;
    }
    private static List<string> CompareSaveDatas_GeoInt(JObject oldJson, JObject newJson, bool isGeo)
    {
        var geoRocksOld = oldJson
        .GetValue("sceneData").ToObject<JObject>()
            .GetValue(isGeo ? "geoRocks" : "persistentIntItems") as JArray;

        var geoRocksNew = newJson
        .GetValue("sceneData").ToObject<JObject>()
            .GetValue(isGeo ? "geoRocks" : "persistentIntItems") as JArray;

        List<string> list = new List<string>();

        foreach (JToken itemNewToken in geoRocksNew)
        {
            JObject itemNew = itemNewToken.ToObject<JObject>();
            if (itemNew["sceneName"].ToString().Length == 0)
                continue;

            var isNew = true;
            foreach (JToken itemOld in geoRocksOld)
            {
                if (JToken.DeepEquals(itemNewToken, itemOld))
                {
                    isNew = false;
                    break;
                }
            }
            if (isNew)
            {
                list.Add((isGeo ? "@geo," : "@int,")
                    + itemNew["sceneName"].ToString() + ","
                    + itemNew["id"].ToString() + ","
                    + (itemNew[isGeo ? "hitsLeft" : "value"].ToString()));
            }
        }
        return list;
    }
    private static List<string> CompareSaveDatas_Bool(JObject oldJson, JObject newJson)
    {
        var geoRocksOld = oldJson
            .GetValue("sceneData").ToObject<JObject>()
            .GetValue("persistentBoolItems") as JArray;

        var geoRocksNew = newJson
            .GetValue("sceneData").ToObject<JObject>()
            .GetValue("persistentBools") as JArray;

        List<string> list = new List<string>();

        foreach (JToken itemNewToken in geoRocksNew)
        {
            JObject itemNew = itemNewToken.ToObject<JObject>();
            if (itemNew["sceneName"].ToString().Length == 0)
                continue;

            var isNew = true;
            foreach (JToken itemOld in geoRocksOld)
            {
                if (JToken.DeepEquals(itemNewToken, itemOld))
                {
                    isNew = false;
                    break;
                }
            }
            if (isNew)
            {
                list.Add("@bool,"
                    + itemNew["sceneName"].ToString() + ","
                    + itemNew["id"].ToString() + ","
                    + (itemNew["activated"].ToString().ToLower()));
            }
        }

        return list;
    }

    private static HashSet<string> GetIgnoredProps()
    {
        var lines = File.ReadAllLines("C:\\Users\\Samuel\\source\\repos\\HollowKnightFlagTracker\\ignore_json_changed_properties.txt");
        var set = new HashSet<string>();
        foreach (var line in lines)
            set.Add(line.Trim());
        return set;
    }
}


public static class JObjectComparer
{
    public static void FindDeepDifferences(JObject oldValue, JObject newValue, string parentPath, HashSet<string> ignoreProps, List<string> outList)
    {
        void add(string key, string value)
        {
            HollowKnightFlagTracker.Log.LogInfo("add " + key);
            var parentKey = parentPath + "." + key;
            if (ignoreProps.Contains(parentKey))
                return;
            if (value == "True")
                value = "true";
            if (value == "False")
                value = "false";
            outList.Add("@," + parentKey + "," + value);
        };

        // Compare properties
        var oldProperties = new HashSet<string>(oldValue.Properties().Select(p => p.Name));
        var newProperties = new HashSet<string>(newValue.Properties().Select(p => p.Name));

        // Added properties
        foreach (var key in newProperties.Except(oldProperties))
        {
            add(key, newValue[key].ToString());
        }

        // Modified or nested properties
        foreach (var key in newProperties.Intersect(oldProperties))
        {
            HollowKnightFlagTracker.Log.LogInfo("test " + key);
            var currentToken = newValue[key];
            var modelToken = oldValue[key];

            if (currentToken is JObject currentObj && modelToken is JObject modelObj)
            {
                FindDeepDifferences(modelObj, currentObj, parentPath + "." + key, ignoreProps, outList);
            }
            else if (currentToken is JArray currentArr && modelToken is JArray modelArr)
            {
                var addedItems = new JArray(currentArr.Except(modelArr, JToken.EqualityComparer));
                var removedItems = new JArray(modelArr.Except(currentArr, JToken.EqualityComparer));

                if (addedItems.HasValues) add(key, addedItems.ToString());
            }
            else
            {
                // Primitive value change
                if (!JToken.DeepEquals(currentToken, modelToken))
                    add(key, currentToken.ToString());
            }
        }
    }
}