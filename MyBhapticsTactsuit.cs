using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Bhaptics.Tact;
using Subnautica_bhaptics;
using UnityEngine;

namespace MyBhapticsTactsuit
{

    public class TactsuitVR
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);
        private static ManualResetEvent LowOxygen_mrse = new ManualResetEvent(false);
        private static ManualResetEvent LowFood_mrse = new ManualResetEvent(false);
        private static ManualResetEvent LowWater_mrse = new ManualResetEvent(false);
        private static ManualResetEvent Swimming_mrse = new ManualResetEvent(false);
        private static ManualResetEvent Teleportation_mrse = new ManualResetEvent(false);

        public int SwimmingDelay = 1000;
        public float SwimmingIntensity = 0.1f;
        public bool SwimmingEffectActive = false;
        public bool SwimmingEffectStarted = false;
        public bool seaGlideEquipped = false;

        // dictionary of all feedback patterns found in the bHaptics directory
        public Dictionary<String, FileInfo> FeedbackMap = new Dictionary<String, FileInfo>();

#pragma warning disable CS0618 // remove warning that the C# library is deprecated
        public HapticPlayer hapticPlayer;
#pragma warning restore CS0618 

        private static RotationOption defaultRotationOption = new RotationOption(0.0f, 0.0f);

        public void HeartBeatFunc()
        {
            while (true)
            {
                // Check if reset event is active
                HeartBeat_mrse.WaitOne();
                PlaybackHaptics("HeartBeat");
                Thread.Sleep(1000);
            }
        }
        public void LowOxygenFunc()
        {
            while (true)
            {
                // Check if reset event is active
                LowOxygen_mrse.WaitOne();
                PlaybackHaptics("lowOxygen");
                Thread.Sleep(1000);
            }
        }
        public void LowFoodFunc()
        {
            while (true)
            {
                // Check if reset event is active
                LowFood_mrse.WaitOne();
                PlaybackHaptics("lowFood");
                Thread.Sleep(1000);
            }
        }
        public void LowWaterFunc()
        {
            while (true)
            {
                // Check if reset event is active
                LowWater_mrse.WaitOne();
                PlaybackHaptics("Eating", true, 0.5f);
                Thread.Sleep(1000);
            }
        }
        public void SwimmingFunc()
        {
            while (true)
            {
                // Check if reset event is active
                Swimming_mrse.WaitOne();
                PlaybackHaptics("Swimming", true, SwimmingIntensity);
                if (seaGlideEquipped)
                {
                    PlaybackHaptics("RecoilArm_L", true, SwimmingIntensity);
                    PlaybackHaptics("RecoilArm_R", true, SwimmingIntensity);
                }
                Thread.Sleep(SwimmingDelay);
            }
        }
        public void TeleportationFunc()
        {
            while (true)
            {
                // Check if reset event is active
                Teleportation_mrse.WaitOne();
                PlaybackHaptics("Teleporting");
                Thread.Sleep(1000);
            }
        }

        public TactsuitVR()
        {

            LOG("Initializing suit");
            try
            {
#pragma warning disable CS0618 // remove warning that the C# library is deprecated
                hapticPlayer = new HapticPlayer("Compound_bhaptics", "Compound_bhaptics");
#pragma warning restore CS0618
                suitDisabled = false;
            }
            catch { LOG("Suit initialization failed!"); }
            RegisterAllTactFiles();
            LOG("Starting HeartBeat thread...");
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
            Thread LowOxygentThread = new Thread(LowOxygenFunc);
            LowOxygentThread.Start();
            Thread LowFoodThread = new Thread(LowFoodFunc);
            LowFoodThread.Start();
            Thread LowWaterThread = new Thread(LowFoodFunc);
            LowWaterThread.Start(); 
            Thread SwimmingThread = new Thread(SwimmingFunc);
            SwimmingThread.Start();
            Thread TeleportationThread = new Thread(TeleportationFunc);
            TeleportationThread.Start();
        }

        public void LOG(string logStr)
        {
            Plugin.Log.LogMessage(logStr);
        }


        void RegisterAllTactFiles()
        {
            if (suitDisabled) { return; }
            // Get location of the compiled assembly and search through "bHaptics" directory and contained patterns
            string assemblyFile = Assembly.GetExecutingAssembly().Location;
            string myPath = Path.GetDirectoryName(assemblyFile);
            LOG("Assembly path: " + myPath);
            string configPath = myPath + "\\bHaptics";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.tact", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    hapticPlayer.RegisterTactFileStr(prefix, tactFileStr);
                    LOG("Pattern registered: " + prefix);
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(prefix, Files[i]);
            }
            systemInitialized = true;
        }

        public void PlaybackHaptics(String key, bool forced = true, float intensity = 1.0f, float duration = 1.0f)
        {
            if (suitDisabled) { return; }
            if (FeedbackMap.ContainsKey(key))
            {
                ScaleOption scaleOption = new ScaleOption(intensity, duration);
                if (hapticPlayer.IsPlaying() && !forced)
                {
                    return;
                }
                else
                {
                    hapticPlayer.SubmitRegisteredVestRotation(key, key, defaultRotationOption, scaleOption);
                }
            }
            else
            {
                LOG("Feedback not registered: " + key);
            }
        }

        public void StartHeartBeat()
        {
            HeartBeat_mrse.Set();
        }

        public void StopHeartBeat()
        {
            HeartBeat_mrse.Reset();
        }

        public void StartLowOxygen()
        {
            LowOxygen_mrse.Set();
        }

        public void StopLowOxygen()
        {
            LowOxygen_mrse.Reset();
        }

        public void StartLowFood()
        {
            LowFood_mrse.Set();
        }

        public void StopLowFood()
        {
            LowFood_mrse.Reset();
        }

        public void StartLowWater()
        {
            LowWater_mrse.Set();
        }

        public void StopLowWater()
        {
            LowWater_mrse.Reset();
        }
        public void StartSwimming()
        {
            if (SwimmingEffectActive && !SwimmingEffectStarted)
            {
                Swimming_mrse.Set();
                SwimmingEffectStarted = true;
            }
        }

        public void StopSwimming()
        {
            if (SwimmingEffectActive && SwimmingEffectStarted)
            {
                Swimming_mrse.Reset();
                SwimmingEffectStarted = false;
            }
        }
        public void StartTeleportation()
        {
            Teleportation_mrse.Set();
        }

        public void StopTeleportation()
        {
            Teleportation_mrse.Reset();
        }

        public void StopHapticFeedback(String effect)
        {
            hapticPlayer.TurnOff(effect);
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();
            foreach (String key in FeedbackMap.Keys)
            {
                hapticPlayer.TurnOff(key);
            }
        }

        public void StopThreads()
        {
            StopHeartBeat();
            StopLowFood();
            StopLowOxygen();
            StopLowWater();
            StopSwimming();
            StopTeleportation();
        }
    }
}
