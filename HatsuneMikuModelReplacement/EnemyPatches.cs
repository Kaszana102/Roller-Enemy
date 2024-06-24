using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roller
{
    [HarmonyPatch]
    internal class EnemyPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void RegisterEnemy(StartOfRound __instance)
        {
            var levels = __instance.levels;

            HandleSpawnInAny(levels);
        }

        private static void HandleSpawnInAny(SelectableLevel[] levels)
        {
            /*
            //Plugin.Log.LogMessage($"[HandleSpawnInAny] Adding enemies to all levels");            
            foreach (var level in levels)
            {
                try
                {                    
                    level.Enemies.Add(new SpawnableEnemyWithRarity
                    {
                        enemyType = RollerEnemy.RollerEnemyPlugin.EnemyType,
                        rarity = 1000
                    });

                    level.OutsideEnemies.Add(new SpawnableEnemyWithRarity
                    {
                        enemyType = RollerEnemy.RollerEnemyPlugin.EnemyType,
                        rarity = 1000
                    }); 

                    level.DaytimeEnemies.Add(new SpawnableEnemyWithRarity
                    {
                        enemyType = RollerEnemy.RollerEnemyPlugin.EnemyType,
                        rarity = 1000
                    });
                    RollerEnemy.RollerEnemyPlugin.Log.LogInfo("ROLLER ENEMY ADDED TO LEVEL: " + level.PlanetName);

                }
                catch (System.Exception e)
                {
                    RollerEnemy.RollerEnemyPlugin.Log.LogInfo($"Failed to add enemy to {level.PlanetName}!\n{e}");                    
                }
            }
            */
        }

    }
}
