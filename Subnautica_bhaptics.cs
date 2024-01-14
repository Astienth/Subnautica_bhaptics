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
        public static List<TechType> oneShotTypes = new List<TechType>();

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
            //init types
            oneShotTypes.Add(TechType.ExosuitClawArmModule);
            oneShotTypes.Add(TechType.ExosuitGrapplingArmModule);
            oneShotTypes.Add(TechType.ExosuitTorpedoArmModule);
            oneShotTypes.Add(TechType.ExosuitPropulsionArmModule);
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
            if (__instance.isUnderwaterForSwimming.value)
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

    [HarmonyPatch(typeof(Player), "UpdateIsUnderwater")]
    public class bhaptics_OnEnterExitWater
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
                Plugin.tactsuitVr.PlaybackHaptics("waterVisor");
            }
            isUnderwaterForSwimming = __instance.isUnderwaterForSwimming.value;
        }
    }

    #endregion

    #region Equipments

    [HarmonyPatch(typeof(Knife), "IsValidTarget")]
    public class bhaptics_OnKnifeAttack
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }

    [HarmonyPatch(typeof(StasisRifle), "Fire")]
    public class bhaptics_OnStatisAttack
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }

    [HarmonyPatch(typeof(ScannerTool), "OnRightHandDown")]
    public class bhaptics_Onscanning
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
        }
    }
    [HarmonyPatch(typeof(AirBladder), "OnRightHandDown")]
    public class bhaptics_OnAirBladder
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
        }
    }

    [HarmonyPatch(typeof(Welder), "Weld")]
    public class bhaptics_Welder
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StartDrilling("right");
        }
    }

    [HarmonyPatch(typeof(Welder), "StopWeldingFX")]
    public class bhaptics_StopWeldingFX
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopDrilling("right");
        }
    }

    [HarmonyPatch(typeof(LaserCutter), "StartLaserCuttingFX")]
    public class bhaptics_LaserCutterStart
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StartDrilling("right");
        }
    }

    [HarmonyPatch(typeof(LaserCutter), "StopLaserCuttingFX")]
    public class bhaptics_LaserCutterStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopDrilling("right");
        }
    }

    [HarmonyPatch(typeof(BuilderTool), "OnRightHandDown")]
    public class bhaptics_OnBuilderTool
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
        }
    }

    [HarmonyPatch(typeof(Constructor), "OnRightHandDown")]
    public class bhaptics_OnConstructor
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
        }
    }
    [HarmonyPatch(typeof(PropulsionCannon), "GrabObject")]
    public class bhaptics_OnPropulsionCannon
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
    
    [HarmonyPatch(typeof(PropulsionCannon), "OnShoot")]
    public class bhaptics_OnPropulsionCannonShoot
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }

    [HarmonyPatch(typeof(StasisRifle), "OnRightHandDown")]
    public class bhaptics_OnStatisCharging
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (Plugin.tactsuitVr.suitDisabled || !__result)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("Scanning_Arm_R");
        }
    }

    #endregion

    #region Vehicles

    [HarmonyPatch(typeof(CyclopsExternalDamageManager), "OnTakeDamage")]
    public class bhaptics_OnCyclopDamage
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Arms");
            Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("impactVisor");
        }
    }

    [HarmonyPatch(typeof(CyclopsEngineChangeState), "OnClick")]
    public class bhaptics_OnCyclopEngineStart
    {
        [HarmonyPrefix]
        public static void Prefix(CyclopsEngineChangeState __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (!__instance.motorMode.engineOn)
            {
                Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump", true, 1f, 4f);
            }
        }
    }

    [HarmonyPatch(typeof(Vehicle), "PlaySplashSound")]
    public class bhaptics_OnSplash
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Splash_Arms");
            Plugin.tactsuitVr.PlaybackHaptics("Splash_Vest");
            Plugin.tactsuitVr.PlaybackHaptics("waterVisor");
        }
    }
    
    [HarmonyPatch(typeof(uGUI_SeamothHUD), "Update")]
    public class bhaptics_OnDamageVehicle
    {
        public static int health;

        [HarmonyPrefix]
        public static void Prefix(uGUI_SeamothHUD __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            health = Traverse.Create(__instance).Field("lastHealth").GetValue<int>();
        }

        [HarmonyPostfix]
        public static void Postfix(uGUI_SeamothHUD __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (health > Traverse.Create(__instance).Field("lastHealth").GetValue<int>())
            {
                Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Arms");
                Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Vest");
                Plugin.tactsuitVr.PlaybackHaptics("impactVisor");
                health = Traverse.Create(__instance).Field("lastHealth").GetValue<int>();
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_ExosuitHUD), "Update")]
    public class bhaptics_OnDamageExosuit
    {
        public static int health;

        [HarmonyPrefix]
        public static void Prefix(uGUI_ExosuitHUD __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            health = Traverse.Create(__instance).Field("lastHealth").GetValue<int>();
        }

        [HarmonyPostfix]
        public static void Postfix(uGUI_ExosuitHUD __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (health != Traverse.Create(__instance).Field("lastHealth").GetValue<int>())
            {
                Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Arms");
                Plugin.tactsuitVr.PlaybackHaptics("VehicleImpact_Vest");
                Plugin.tactsuitVr.PlaybackHaptics("impactVisor");
                health = Traverse.Create(__instance).Field("lastHealth").GetValue<int>();
            }
        }
    }
    
    [HarmonyPatch(typeof(Exosuit), "OnLand")]
    public class bhaptics_OnExosuitLanding
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump");
        }
    }

    [HarmonyPatch(typeof(Exosuit), "ApplyJumpForce")]
    public class bhaptics_OnExosuitJumping
    {
        [HarmonyPrefix]
        public static void Prefix(Exosuit __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            bool grounded = Traverse.Create(__instance).Field("onGround").GetValue<bool>();
            double timeLastJumped = (double)(Traverse.Create(__instance)
                .Field("timeLastJumped").GetValue<float>()
                + 1.0);
            Plugin.Log.LogWarning("JUMP "+grounded+" "+timeLastJumped+" "+ (double)Time.time);
            if (grounded && timeLastJumped <= (double)Time.time)
            {
                Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump");
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit), "SlotLeftDown")]
    public class bhaptics_OnExosuitArmDown
    {
        [HarmonyPostfix]
        public static void Postfix(Exosuit __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (__instance.mainAnimator.GetBool("use_tool_left"))
            {
                TechType type = Traverse.Create(__instance).Field("currentLeftArmType")
                    .GetValue<TechType>();
                if (Plugin.oneShotTypes.Contains(type))
                {
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_L");
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_L");
                }
                if(type == TechType.ExosuitDrillArmModule)
                {
                    Plugin.tactsuitVr.StartDrilling("left");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit), "SlotLeftUp")]
    public class bhaptics_OnExosuitArmUp
    {
        [HarmonyPostfix]
        public static void Postfix(Exosuit __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
             TechType type = Traverse.Create(__instance).Field("currentLeftArmType")
                    .GetValue<TechType>();
            if (type == TechType.ExosuitDrillArmModule)
            {
                Plugin.tactsuitVr.StopDrilling("left");
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit), "SlotRightDown")]
    public class bhaptics_OnExosuitArmDownRight
    {
        [HarmonyPostfix]
        public static void Postfix(Exosuit __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (__instance.mainAnimator.GetBool("use_tool_right"))
            {
                TechType type = Traverse.Create(__instance).Field("currentRightArmType")
                    .GetValue<TechType>();
                if (Plugin.oneShotTypes.Contains(type))
                {
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
                }
                if (type == TechType.ExosuitDrillArmModule)
                {
                    Plugin.tactsuitVr.StartDrilling("right");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit), "SlotRightUp")]
    public class bhaptics_OnExosuitArmUpRight
    {
        [HarmonyPostfix]
        public static void Postfix(Exosuit __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            TechType type = Traverse.Create(__instance).Field("currentRightArmType")
                   .GetValue<TechType>();
            if (type == TechType.ExosuitDrillArmModule)
            {
                Plugin.tactsuitVr.StopDrilling("right");
            }
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
            Plugin.tactsuitVr.PlaybackHaptics("impactVisor");
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

    [HarmonyPatch(typeof(uGUI_WaterBar), "OnPulse")]
    public class bhaptics_OnLowWaterStart
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_WaterBar __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            float pulseDelay = Traverse.Create(__instance).Field("pulseDelay").GetValue<float>();
            if (pulseDelay <= 0.85f)
            {
                Plugin.tactsuitVr.StartLowWater();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_WaterBar), "OnDrink")]
    public class bhaptics_OnLowWaterStop
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopLowWater();
            Plugin.tactsuitVr.PlaybackHaptics("Eating");
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
    public class bhaptics_OnPlayerDestroy
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

