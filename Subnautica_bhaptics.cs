using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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
        public static ConfigEntry<bool> swimmingEffects;


        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin Subnautica_bhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            //config
            swimmingEffects = Config.Bind("General", "swimmingEffects", false);
            tactsuitVr.SwimmingEffectActive = swimmingEffects.Value;
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

    [HarmonyPatch(typeof(Player), "LateUpdate")]
    public class bhaptics_OnSwimming
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if(__instance.isUnderwaterForSwimming.value)
            {
                //start thread
                Plugin.tactsuitVr.StartSwimming();
                //is seaglide equipped ?
                Plugin.tactsuitVr.seaGlideEquipped = __instance.motorMode == Player.MotorMode.Seaglide;
                //calculate delay and intensity
                int delay = (int)((30 - Math.Truncate(__instance.movementSpeed * 10)) * 100);
                Plugin.tactsuitVr.SwimmingDelay = 
                    (delay > 0) 
                    ?
                    (delay < 3000) 
                        ? delay : 3000
                    : 1000;
                    
                float intensity = (float)(Math.Truncate(__instance.movementSpeed * 10) / 30);
                Plugin.tactsuitVr.SwimmingIntensity = intensity;
            }
            else
            {
                Plugin.tactsuitVr.StopSwimming();
            }
        }

        [HarmonyPatch(typeof(PrecursorTeleporter), "OnPlayerCinematicModeEnd")]
        public class bhaptics_OnTeleportingStart
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.tactsuitVr.suitDisabled)
                {
                    return;
                }
                Plugin.tactsuitVr.StartTeleportation();
            }
        }

        [HarmonyPatch(typeof(Player), "CompleteTeleportation")]
        public class bhaptics_OnTeleportingStop
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.tactsuitVr.suitDisabled)
                {
                    return;
                }
                Plugin.tactsuitVr.StopTeleportation();
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateIsUnderwaterForSwimming")]
        public class bhaptics_OnExitWater
        {
            public static bool isUnderwaterForSwimming = false;

            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                if (Plugin.tactsuitVr.suitDisabled)
                {
                    return;
                }
                isUnderwaterForSwimming = __instance.isUnderwaterForSwimming.value;
            }

            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (Plugin.tactsuitVr.suitDisabled)
                {
                    return;
                }
                if (__instance.isUnderwaterForSwimming.value != isUnderwaterForSwimming)
                {
                    Plugin.tactsuitVr.PlaybackHaptics(
                        (!isUnderwaterForSwimming) ? "EnterWater_Vest" : "ExitWater_Vest"); 
                    Plugin.tactsuitVr.PlaybackHaptics(
                        (!isUnderwaterForSwimming) ? "EnterWater_Arms" : "ExitWater_Arms");
                }
                isUnderwaterForSwimming = __instance.isUnderwaterForSwimming.value;
            }
        }
    }

    #endregion

    #region Equipments

    [HarmonyPatch(typeof(Knife), "IsValidTarget")]
    [HarmonyPatch(typeof(StasisRifle), "Fire")]
    public class bhaptics_OnKnifeAttack
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_L");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }

    [HarmonyPatch(typeof(ScannerTool), "OnRightHandDown")]
    [HarmonyPatch(typeof(AirBladder), "OnRightHandDown")]
    [HarmonyPatch(typeof(BuilderTool), "OnRightHandDown")]
    [HarmonyPatch(typeof(Constructor), "OnRightHandDown")]
    [HarmonyPatch(typeof(PropulsionCannonWeapon), "OnRightHandDown")]
    [HarmonyPatch(typeof(StasisRifle), "OnRightHandDown")]
    public class bhaptics_Onscanning
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
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
            Plugin.tactsuitVr.StopThreads();
        }
    }

    [HarmonyPatch(typeof(Player), "OnTakeDamage")]
    public class bhaptics_OnTakeDamage
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Impact", true, 2f);
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
            Plugin.tactsuitVr.PlaybackHaptics("Eating");
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
            Plugin.tactsuitVr.PlaybackHaptics("Heal");
        }
    }

    #endregion

    [HarmonyPatch(typeof(Player), "OnDestroy")]
    [HarmonyPatch(typeof(Player), "OnDisable")]
    public class bhaptics_OnPlayerDisabled
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopThreads();
        }
    }
}

