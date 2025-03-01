﻿using STROOP.Forms;
using STROOP.Managers;
using STROOP.Models;
using STROOP.Structs.Configurations;
using STROOP.Ttc;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STROOP.Structs
{
    public static class WaterMain
    {
        public static Random r = new Random();

        public static void FindAllBubbleConfigurations()
        {
            ObjSlotManager objSlotManager = new ObjSlotManager(new List<Input>());
            for (int i = 0; i < 1000; i++)
            {
                objSlotManager.Update();
            }

            HashSet<(int numBubbles, bool bubbleSpawnerPresent)> alreadySeen =
                new HashSet<(int numBubbles, bool bubbleSpawnerPresent)>();
            while (true)
            {
                objSlotManager.Update();
                var bubbleConfiguration = objSlotManager.GetBubbleConfiguration();
                if (!alreadySeen.Contains(bubbleConfiguration))
                {
                    Config.Print(bubbleConfiguration);
                    alreadySeen.Add(bubbleConfiguration);
                }
            }
        }

        public static void BruteForce2()
        {
            Input.USE_TAS_INPUT_Y = false;

            List<int> rngIndexes = LoadingZoneMain.GetRngIndexes();
            bool[] rngIndexSuccesses = new bool[65114];
            rngIndexes.ForEach(rngIndex => rngIndexSuccesses[rngIndex] = true);

            HashSet<int> seenAlready = new HashSet<int>();
            for (int count = 0; true; count++)
            {
                List<Input> inputs = GenerateInputs();
                ObjSlotManager objSlotManager = Simulate(inputs, false);
                if (objSlotManager != null)
                {
                    int rngIndex = objSlotManager.Rng.GetIndex();

                    if (!seenAlready.Contains(rngIndex))
                    {
                        Config.Print("just saw " + rngIndex);
                        seenAlready.Add(rngIndex);
                    }

                    bool success = rngIndexSuccesses[rngIndex];
                    if (success)
                    {
                        Config.Print();
                        Config.Print("SUCCESS AFTER " + count);
                        Config.Print();
                        Config.Print(string.Join("\r\n", inputs));
                        Config.Print();
                        Simulate(inputs, true);
                        Config.Print();
                        return;
                    }
                }
            }
        }

        public static void BruteForce()
        {
            Input.USE_TAS_INPUT_Y = false;
            HashSet<int> seenRngIndexes = new HashSet<int>();
            for (int count = 0; true; count++)
            {
                List<Input> inputs = GenerateInputs();
                ObjSlotManager objSlotManager = Simulate(inputs, false);
                if (objSlotManager != null)
                {
                    int rngIndex = objSlotManager.Rng.GetIndex();
                    (int numInitialBubbles, bool isBubbleSpawnerPresent) = objSlotManager.GetBubbleConfiguration();
                    int numTries = 1;
                    seenRngIndexes.Add(rngIndex);
                    Config.Print("CHECKING SECOND SUCCESS {0} {1} {2} {3}", rngIndex, isBubbleSpawnerPresent, numInitialBubbles, seenRngIndexes.Count);
                    bool success2 = LoadingZoneMain.Run(rngIndex, isBubbleSpawnerPresent, numInitialBubbles, numTries, true);
                    if (success2)
                    {
                        Config.Print();
                        Config.Print("SUCCESS AFTER " + count);
                        Config.Print();
                        Config.Print(string.Join("\r\n", inputs));
                        Config.Print();
                        Simulate(inputs, true);
                        Config.Print();
                        return;
                    }
                }
            }
        }

        public static List<Input> GenerateInputs()
        {
            List<Input> inputs = new List<Input>();
            bool movingDown = true;
            while (true)
            {
                if (movingDown)
                {
                    int x = r.Next(-30, -10);
                    int y = r.Next(30, 127);
                    Input input = new Input(x, y);
                    int times = r.Next(5, 20);
                    for (int i = 0; i < times; i++)
                    {
                        inputs.Add(input);
                    }
                }
                else
                {
                    int x = r.Next(-30, -10);
                    int y = r.Next(-128, -30);
                    Input input = new Input(x, y);
                    int times = r.Next(5, 20);
                    for (int i = 0; i < times; i++)
                    {
                        inputs.Add(input);
                    }
                }
                movingDown = !movingDown;
                if (inputs.Count > 80) break;
            }
            return inputs;
        }

        public static ObjSlotManager Simulate(List<Input> inputs, bool print)
        {
            ObjSlotManager objSlotManager = new ObjSlotManager(inputs);
            if (print) Config.Print(objSlotManager);
            while (objSlotManager.GlobalTimer < 7798)
            {
                objSlotManager.Update();
                if (print) Config.Print(objSlotManager);
            }

            bool success =
                objSlotManager.HasBubbleConfiguration(5, true) ||
                objSlotManager.HasBubbleConfiguration(6, false);

            return success ? objSlotManager : null;
        }

        public class ObjSlotManager
        {
            public int GlobalTimer;
            public int WaterLevelIndex;
            public int WaterLevel;
            public int FutureWaterLevelIndex;
            public int FutureWaterLevel;

            public TtcRng Rng;

            public List<List<WaterObject>> ObjectLists;
            public List<WaterObject> YorangeObjects;
            public List<WaterObject> GreenObjects;
            public List<WaterObject> PurpleObjects;
            public List<WaterObject> BrownObjects;

            public ObjSlotManager(List<Input> inputs)
            {
                GlobalTimer = Config.Stream.GetInt(MiscConfig.GlobalTimerAddress);
                WaterLevelIndex = WaterLevelCalculator.GetWaterLevelIndex();
                WaterLevel = WaterLevelCalculator.GetWaterLevelFromIndex(WaterLevelIndex);
                FutureWaterLevelIndex = WaterLevelCalculator.GetWaterLevelIndex() + 1;
                FutureWaterLevel = WaterLevelCalculator.GetWaterLevelFromIndex(FutureWaterLevelIndex);

                YorangeObjects = new List<WaterObject>();
                GreenObjects = new List<WaterObject>();
                PurpleObjects = new List<WaterObject>();
                BrownObjects = new List<WaterObject>();
                ObjectLists =
                    new List<List<WaterObject>>()
                    {
                        YorangeObjects, GreenObjects, PurpleObjects, BrownObjects,
                    };

                Rng = new TtcRng();

                MarioObject marioObject = new MarioObject(this, Rng, inputs);
                YorangeObjects.Add(marioObject);

                List<ObjectDataModel> bobombBuddyObjs = Config.ObjectSlotsManager.GetLoadedObjectsWithName("Bob-omb Buddy (Opens Cannon)");
                foreach (var bobombBuddyObj in bobombBuddyObjs)
                {
                    int blinkingTimer = Config.Stream.GetInt(bobombBuddyObj.Address + 0xF4);
                    BobombBuddyObject bobombBuddyObject = new BobombBuddyObject(this, Rng, blinkingTimer);
                    GreenObjects.Add(bobombBuddyObject);
                }

                List<ObjectDataModel> bubbleSpawnerObjs = Config.ObjectSlotsManager.GetLoadedObjectsWithName("Bubble Spawner");
                foreach (var bubbleSpawnerObj in bubbleSpawnerObjs)
                {
                    float y = Config.Stream.GetFloat(bubbleSpawnerObj.Address + ObjectConfig.YOffset);
                    int timer = Config.Stream.GetInt(bubbleSpawnerObj.Address + ObjectConfig.TimerOffset);
                    int timerMax = Config.Stream.GetInt(bubbleSpawnerObj.Address + 0xF4);
                    BubbleSpawnerObject bubbleSpawnerObject = new BubbleSpawnerObject(this, Rng, y, timer, timerMax);
                    PurpleObjects.Add(bubbleSpawnerObject);
                }

                List<ObjectDataModel> bubbleObjs = Config.ObjectSlotsManager.GetLoadedObjectsWithName("Underwater Bubble");
                foreach (var bubbleObj in bubbleObjs)
                {
                    float y = Config.Stream.GetFloat(bubbleObj.Address + ObjectConfig.YOffset);
                    int timer = Config.Stream.GetInt(bubbleObj.Address + ObjectConfig.TimerOffset);
                    float varF4 = Config.Stream.GetFloat(bubbleObj.Address + 0xF4);
                    float varF8 = Config.Stream.GetFloat(bubbleObj.Address + 0xF8);
                    float varFC = Config.Stream.GetFloat(bubbleObj.Address + 0xFC);
                    float var100 = Config.Stream.GetFloat(bubbleObj.Address + 0x100);
                    BubbleObject bubbleObject = new BubbleObject(this, Rng, y, timer, varF4, varF8, varFC, var100);
                    BrownObjects.Add(bubbleObject);
                }
            }

            public void Update()
            {
                WaterLevelIndex++;
                WaterLevel = WaterLevelCalculator.GetWaterLevelFromIndex(WaterLevelIndex);
                FutureWaterLevelIndex++;
                FutureWaterLevel = WaterLevelCalculator.GetWaterLevelFromIndex(FutureWaterLevelIndex);

                foreach (var objList in ObjectLists)
                {
                    foreach (var obj in objList)
                    {
                        obj.Update();
                    }
                }

                foreach (var objList in ObjectLists)
                {
                    for (int i = 0; i < objList.Count; i++)
                    {
                        if (objList[i].ShouldBeDeleted)
                        {
                            objList.RemoveAt(i);
                            i--;
                        }
                    }
                }

                GlobalTimer++;
            }

            public void AddObject(WaterObject waterObject)
            {
                if (waterObject is BubbleSpawnerObject)
                {
                    PurpleObjects.Add(waterObject);
                }
                else if (waterObject is BubbleObject)
                {
                    BrownObjects.Add(waterObject);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public bool HasBubbleSpawner()
            {
                return PurpleObjects.Count > 0;
            }

            public bool HasBubbleConfiguration(int numBubbles, bool bubbleSpawnerPresent)
            {
                bool satisfiesNumBubbles = numBubbles == BrownObjects.Count;
                bool satisfiesBubbleSpawnerPresent = bubbleSpawnerPresent == (PurpleObjects.Count > 0);
                return satisfiesNumBubbles && satisfiesBubbleSpawnerPresent;
            }

            public (int numBubbles, bool bubbleSpawnerPresent) GetBubbleConfiguration() 
            {
                return (BrownObjects.Count, PurpleObjects.Count > 0);
            }

            public override string ToString()
            {
                List<WaterObject> objList = ObjectLists.SelectMany(list => list).ToList();
                List<string> stringList = objList.ConvertAll(obj => obj.ToString());
                stringList.Insert(0, GlobalTimer.ToString());
                stringList.Insert(1, Rng.ToString());
                stringList.Insert(2, "WaterLevel=" + WaterLevel);
                return string.Join("\r\n", stringList) + "\r\n";
            }
        }

        public abstract class WaterObject
        {
            public ObjSlotManager ObjSlotManager;
            public TtcRng Rng;
            public bool ShouldBeDeleted;

            public WaterObject(ObjSlotManager objectSlotsManager, TtcRng rng)
            {
                ObjSlotManager = objectSlotsManager;
                Rng = rng;
                ShouldBeDeleted = false;
            }

            public abstract void Update();

            public void MarkForDeletion()
            {
                ShouldBeDeleted = true;
            }
        }

        public class MarioObject : WaterObject
        {
            public List<Input> Inputs;
            public WaterState WaterState;

            public MarioObject(ObjSlotManager objSlotManager, TtcRng rng, List<Input> inputs)
                : base(objSlotManager, rng)
            {
                Inputs = inputs;
                WaterState = new WaterState();
            }

            public override void Update()
            {
                int index = WaterState.Index;
                Input input = index < Inputs.Count ? Inputs[index] : new Input(0, 127);
                WaterState.Update(input, ObjSlotManager.WaterLevel);

                if ((WaterState.Y < (ObjSlotManager.WaterLevel - 160)) || (WaterState.Pitch < -0x800))
                {
                    if (!ObjSlotManager.HasBubbleSpawner())
                    {
                        BubbleSpawnerObject bubbleSpawnerObject =
                            new BubbleSpawnerObject(ObjSlotManager, Rng, WaterState.Y, 0, 0);
                        ObjSlotManager.AddObject(bubbleSpawnerObject);
                    }
                }
            }

            public override string ToString()
            {
                int index = WaterState.Index - 1;
                Input lastInput = index == -1 ? null : index < Inputs.Count ? Inputs[index] : new Input(0, 127);
                string inputString = lastInput?.ToString() ?? "NO_INPUT";
                string inputLine = "Input " + inputString;
                string marioLine = "Mario " + WaterState.ToString();
                return inputLine + "\r\n" + marioLine;
            }
        }

        public class BobombBuddyObject : WaterObject
        {
            public int BobombBuddyBlinkingTimer;

            public BobombBuddyObject(ObjSlotManager objSlotManager, TtcRng rng, int bobombBuddyBlinkingTimer)
                : base(objSlotManager, rng)
            {
                BobombBuddyBlinkingTimer = bobombBuddyBlinkingTimer;
            }

            public override void Update()
            {
                if (BobombBuddyBlinkingTimer > 0)
                {
                    BobombBuddyBlinkingTimer = (BobombBuddyBlinkingTimer + 1) % 16;
                }
                else
                {
                    if (Rng.PollRNG() <= 655)
                    {
                        BobombBuddyBlinkingTimer++;
                    }
                }
            }

            public override string ToString()
            {
                return "BobombBuddy " + BobombBuddyBlinkingTimer;
            }
        }

        public class BubbleSpawnerObject : WaterObject
        {
            public float Y;
            public int Timer;
            public int TimerMax;

            public BubbleSpawnerObject(
                ObjSlotManager objSlotManager, TtcRng rng,
                float y, int timer, int timerMax)
                : base(objSlotManager, rng)
            {
                Y = y;
                Timer = timer;
                TimerMax = timerMax;
            }

            public override void Update()
            {
                if (Timer == 0)
                {
                    TimerMax = 2 + (int)(9 * Rng.PollFloat());
                }

                if (Timer == TimerMax)
                {
                    BubbleObject bubbleObject =
                        new BubbleObject(ObjSlotManager, Rng, Y, 0, 0, 0, 0, 0);
                    ObjSlotManager.AddObject(bubbleObject);
                    MarkForDeletion();
                }

                Timer++;
            }

            public override string ToString()
            {
                return string.Format(
                    "{0} Y={1} Timer={2} TimerMax={3}",
                    "BubbleSpawner", Y, Timer, TimerMax);
            }
        }

        public class BubbleObject : WaterObject
        {
            public float Y;
            public int Timer;
            public float VarF4;
            public float VarF8;
            public float VarFC;
            public float Var100;

            public BubbleObject(
                    ObjSlotManager objSlotManager, TtcRng rng,
                    float y, int timer,
                    float varF4, float varF8, float varFC, float var100)
                : base(objSlotManager, rng)
            {
                Y = y;
                Timer = timer;
                VarF4 = varF4;
                VarF8 = varF8;
                VarFC = varFC;
                Var100 = var100;
            }

            public override void Update()
            {
                if (Timer == 0)
                {
                    bhv_bubble_wave_init();

                    VarF4 = -50 + Rng.PollFloat() * 100;
                    VarF8 = -50 + Rng.PollFloat() * 100;
                    VarFC = Rng.PollFloat() * 50;
                    Y += VarFC;

                    bhvSmallWaterWave398();
                }

                if (Timer < 60)
                {
                    bhvSmallWaterWave398();
                    bhv_small_water_wave_loop();
                }

                Timer++;

                if (Timer == 61)
                {
                    MarkForDeletion();
                }
            }

            public void bhv_bubble_wave_init()
            {
                VarFC = 0x800 + (int)(Rng.PollFloat() * 2048.0f);
                Var100 = 0x800 + (int)(Rng.PollFloat() * 2048.0f);
            }

            public void bhvSmallWaterWave398()
            {
                Y += 7;
                VarF4 = -2 + Rng.PollFloat() * 5;
                VarF8 = -2 + Rng.PollFloat() * 5;
            }

            public void bhv_small_water_wave_loop()
            {
                if (Y > ObjSlotManager.FutureWaterLevel)
                {
                    Y += 5;
                    MarkForDeletion();
                }
            }

            public override string ToString()
            {
                return string.Format(
                    "{0} Y={1} Timer={2}",
                    "Bubble", (double)Y, Timer);
            }
        }
    }
}
