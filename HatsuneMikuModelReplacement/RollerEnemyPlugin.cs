using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx;
using Unity;
using UnityEngine;
using System.IO;
using System.Reflection;
using HarmonyLib;
using BepInEx.Logging;
using Roller;
using Unity.Netcode;
using LethalLib.Modules;
using UnityEngine.AI;

namespace RollerEnemy
{
    [BepInPlugin("RollerEnemy", "RollerEnemy", "1.0.0")]    
    public class RollerEnemyPlugin: BaseUnityPlugin
    {
        public static EnemyType EnemyType;
        internal static ManualLogSource Log;

        const string EnemyTypePath = "Assets/Roller/RollerType.asset";
        private void Awake()
        {            
            Log = Logger;
            Logger.LogInfo($"Plugin {"RollerEnemy"} START!");
            LoadAssets();
            Logger.LogInfo($"Plugin {"RollerEnemy"} LOADED!");
        }

        private void LoadAssets()
        {
            Assets.PopulateAssets();
            LoadNetWeaver();
            Logger.LogInfo($"Plugin {"RollerEnemy"} ASSETS POPULATED!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            if (Assets.MainAssetBundle.Contains(EnemyTypePath))
            {
                Logger.LogInfo($"Plugin {"RollerEnemy"} VALID PATH!");
                
                EnemyType = Assets.MainAssetBundle.LoadAsset<EnemyType>(EnemyTypePath);

                if (EnemyType == null)
                {
                    Logger.LogInfo($"Plugin {"RollerEnemy"} ENEMYTYPE IS NULL!");                    
                }
                else
                {
                    if(EnemyType.enemyPrefab == null)
                    {
                        Logger.LogInfo($"Plugin {"RollerEnemy"} ROLLER ENEMY PREFAB MISSING!");
                    }
                    else
                    {                        
                        AddScripts(EnemyType);

                        Logger.LogInfo("Registering Roller as Enemy");
                        Levels.LevelTypes levelFlags = Levels.LevelTypes.All;
                        Enemies.SpawnType spawnType = Enemies.SpawnType.Default;

                        TerminalNode snailNode = ScriptableObject.CreateInstance<TerminalNode>();
                        snailNode.displayText = "Roller \n\nDanger level: 65%\n\n" +
                            "Spiky roller which rolls whenever it sees danger."+
                            "They are shy, but very dangerous creatures. They close themselves in their shells, " +
                            "but check regurarly for any dangers. Beware of their scream, as it might mean a certain death!" +
                        "\n\n";

                        snailNode.clearPreviousText = true;
                        snailNode.maxCharactersToType = 2000;
                        snailNode.creatureName = "Roller";
                        snailNode.creatureFileID = 28888;

                        TerminalKeyword snailKeyword = TerminalUtils.CreateTerminalKeyword("roller", specialKeywordResult: snailNode);                        
                        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EnemyType.enemyPrefab);
                        Enemies.RegisterEnemy(EnemyType, 25, levelFlags, spawnType, snailNode, snailKeyword); //25

                        Logger.LogInfo($"Plugin {"RollerEnemy"} ROLLER ENEMY TYPE LOADED CORRECTLY!");
                    }                    
                }                
            }
            else
            {
                Logger.LogInfo($"Plugin {"RollerEnemy"} INVALID PATH!");
            }
            //NetworkPatches.RegisterPrefab(EnemyType.enemyPrefab);            
        }


        private void AddScripts(EnemyType rollerType)
        {             
            GameObject rollerPrefab = rollerType.enemyPrefab;

            //Add enemyAI and fill fields
            rollerPrefab.AddComponent<RollerAI>();
            rollerPrefab.GetComponent<RollerAI>().enemyType= rollerType;
            rollerPrefab.GetComponent<RollerAI>().eye = rollerPrefab.transform.Find("Roller Armature").Find("Eye");
            rollerPrefab.GetComponent<RollerAI>().secondEye = rollerPrefab.transform.Find("Roller Armature").Find("Eye").Find("EyeOther");

            //add wall checking
            rollerPrefab.transform.Find("WallSensor").gameObject.AddComponent<WallSensor>();
            rollerPrefab.transform.Find("WallSensor").gameObject.GetComponent<WallSensor>().roller = rollerPrefab.GetComponent<RollerAI>();

            //fill collision detect
            rollerPrefab.GetComponent<EnemyAICollisionDetect>().mainScript = rollerPrefab.GetComponent<RollerAI>();

        }



        private void LoadNetWeaver()
        {            
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {                
                // ? prevents the compatibility layer from crashing the plugin loading
                try
                {
                    var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                        if (attributes.Length > 0)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }
                catch
                {
                    //Log.LogWarning($"NetWeaver is skipping {type.FullName}");
                }
            }
        }
    }

    public static class Assets
    {
        // Replace mbundle with the Asset Bundle Name from your unity project 
        public static string mainAssetBundleName = "rollerbundle";
        public static AssetBundle MainAssetBundle = null;

        private static string GetAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name;
        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {                
                Console.WriteLine(GetAssemblyName() + "." + mainAssetBundleName);
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetAssemblyName() + "." + mainAssetBundleName))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }

            }
        }
    }
}
