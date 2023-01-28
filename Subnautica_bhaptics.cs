using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MyBhapticsTactsuit;

namespace Subnautica_bhaptics
{
    [BepInPlugin("org.bepinex.plugins.Subnautica_bhaptics", "Subnautica bHaptics integration", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;


        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin Subnautica_bhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.Subnautica_bhaptics");
            harmony.PatchAll();
        }
    }

    #region Actions

    [HarmonyPatch(typeof(Inventory), "OnAddItem")]
    public class bhaptics_OnPickupItem
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("PickUpItem");
        }
    }
    #endregion

    #region Health

    [HarmonyPatch(typeof(Player), "OnKill")]
    public class bhaptics_OnDeath
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Death");
        }
    }

    [HarmonyPatch(typeof(uGUI_OxygenBar), "OnPulse")]
    public class bhaptics_OnLowOxygen
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_OxygenBar __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            float pulseDelay = Traverse.Create(__instance).Field("pulseDelay").GetValue<float>();
            if (pulseDelay >= 2 || !Utils.GetLocalPlayerComp().isUnderwaterForSwimming.value)
            {
                Plugin.tactsuitVr.StopLowOxygen();
                return;
            }
            if (pulseDelay < 2)
            {
                Plugin.tactsuitVr.StartLowOxygen();
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_FoodBar), "OnPulse")]
    public class bhaptics_OnLowFoodStart
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_FoodBar __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            float pulseDelay = Traverse.Create(__instance).Field("pulseDelay").GetValue<float>();
            if (pulseDelay <= 0.85f)
            {
                Plugin.tactsuitVr.StartLowFood();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_FoodBar), "OnEat")]
    public class bhaptics_OnLowFoodStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopLowFood();
        }
    }

    [HarmonyPatch(typeof(uGUI_HealthBar), "OnPulse")]
    public class bhaptics_OnLowHealthStart
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_HealthBar __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            float pulseDelay = Traverse.Create(__instance).Field("pulseDelay").GetValue<float>();
            if (pulseDelay <= 1.85f)
            {
                Plugin.tactsuitVr.StartHeartBeat();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_HealthBar), "OnHealDamage")]
    public class bhaptics_OnLowHealthStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopHeartBeat();
        }
    }

    #endregion
}

