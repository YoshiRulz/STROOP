﻿using STROOP.Forms;
using STROOP.Models;
using STROOP.Structs;
using STROOP.Structs.Configurations;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace STROOP.Ttc
{

    public class TtcSimulation
    {
        private readonly TtcRng _rng;
        private readonly List<TtcObject> _rngObjects;
        private readonly int _startingFrame;
        private int _currentFrame;

        public TtcSimulation(ushort rngValue, int startingFrame, List<int> dustFrames = null)
        {
            //set up objects
            _rng = new TtcRng(rngValue); //initial RNG during star selection screen
            _rngObjects = TtcUtilities.CreateRngObjects(_rng, dustFrames);

            //set up testing variables
            _startingFrame = startingFrame; //the frame directly preceding any object initialization
            _currentFrame = _startingFrame;
        }

        public TtcSimulation(List<int> dustFrames = null)
        {
            //set up objects
            _rng = new TtcRng(Config.Stream.GetUShort(MiscConfig.RngAddress));
            _rngObjects = TtcUtilities.CreateRngObjectsFromGame(_rng, dustFrames);

            //set up testing variables
            _startingFrame = MupenUtilities.GetFrameCount(); //the frame directly preceding any object initialization
            _currentFrame = _startingFrame;
        }

        public TtcSimulation(TtcSaveState saveState)
        {
            (_rng, _rngObjects) = TtcUtilities.CreateRngObjectsFromSaveState(saveState);
            _startingFrame = 0;
            _currentFrame = _startingFrame;
        }

        public TtcSimulation(TtcSaveState saveState, int startingFrame, List<int> dustFrames)
        {
            (_rng, _rngObjects) = TtcUtilities.CreateRngObjectsFromSaveState(saveState);
            _startingFrame = startingFrame;
            _currentFrame = _startingFrame;
            AddDustFrames(dustFrames);
        }

        public TtcSimulation(string saveStateString) : this(new TtcSaveState(saveStateString))
        {
        }

        public TtcSaveState GetSaveState()
        {
            return new TtcSaveState(_rng.GetRng(), _rngObjects);
        }

        public string GetSaveStateString()
        {
            return GetSaveState().ToString();
        }

        public TtcSimulation Clone()
        {
            return new TtcSimulation(GetSaveState());
        }

        public override string ToString()
        {
            return _rng + " " + string.Join(" ", _rngObjects);
        }

        public void AddDustFrames(List<int> dustFrames)
        {
            TtcDust dust = (TtcDust)_rngObjects.FirstOrDefault(obj => obj is TtcDust);
            if (dust == null) throw new ArgumentOutOfRangeException();
            dust.AddDustFrames(dustFrames);
        }

        public List<int> GetDustFrames()
        {
            TtcDust dust = (TtcDust)_rngObjects.FirstOrDefault(obj => obj is TtcDust);
            if (dust == null) throw new ArgumentOutOfRangeException();
            return new List<int>(dust._dustFrames);
        }

        public int GetMaxDustFrame()
        {
            TtcDust dust = (TtcDust)_rngObjects.FirstOrDefault(obj => obj is TtcDust);
            if (dust == null) throw new ArgumentOutOfRangeException();
            return dust.GetMaxDustFrame();
        }

        public void TurnOffBobombs()
        {
            foreach (TtcObject obj in _rngObjects)
            {
                if (obj is TtcBobomb bobomb)
                {
                    bobomb.SetWithinMarioRange(0);
                }
            }
        }

        public string GetObjectsString(int endingFrame)
        {
            //iterate through frames to update objects
            int frame = _startingFrame;
            int counter = 0;
            while (frame < endingFrame)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }
            }

            List<string> lines = new List<string>();
            foreach (TtcObject rngObject in _rngObjects)
            {
                lines.Add(rngObject.ToString());
            }
            lines.Add("RNG Value = " + _rng.GetRng());
            lines.Add("RNG Index = " + _rng.GetIndex());
            lines.Add("");
            lines.Add(String.Format("iterated through {0} frames, from {1} to {2}", counter, _startingFrame, endingFrame));
            lines.Add("frame = " + frame);
            lines.Add("");
            lines.Add(new TtcSaveState(_rng.GetRng(), _rngObjects).ToString());
            return String.Join("\r\n", lines);
        }

        public int? FindIdealCogConfiguration(int numFramesMin, int numFramesMax)
        {
            TtcCog upperCog = _rngObjects[31] as TtcCog;
            TtcCog lowerCog = _rngObjects[32] as TtcCog;
            List<CogConfiguration> cogConfigurations = new List<CogConfiguration>();

            int numCogConfigurations = 9;
            int lowerCogGoodAngle = 9892;
            List<int> lowerCogGoodAngles = Enumerable.Range(0, 6).ToList()
                .ConvertAll(index => lowerCogGoodAngle + 65536 / 6 * index)
                .ConvertAll(angle => (int)MoreMath.NormalizeAngleTruncated(angle));

            //iterate through frames to update objects
            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + numFramesMax)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (cogConfigurations.Count >= numCogConfigurations)
                    cogConfigurations.RemoveAt(0);
                cogConfigurations.Add(new CogConfiguration(upperCog, lowerCog));

                if (counter >= numFramesMin)
                {
                    if (cogConfigurations.Count < numCogConfigurations) continue;

                    int lowerCogAngleDist = lowerCogGoodAngles.Min(
                        angle => (int)MoreMath.GetAngleDistance(
                            angle, MoreMath.NormalizeAngleTruncated(cogConfigurations[5].LowerCogAngle)));

                    bool upperCogPreGoal =
                        cogConfigurations[8].UpperCogAngle == 46432 && // right angle
                        cogConfigurations[7].UpperCogTargetAngularVelocity == 1200 && // was targeting 1200
                        cogConfigurations[8].UpperCogCurrentAngularVelocity == 1200; // moved at 1200 speed

                    bool upperCogGoal =
                        cogConfigurations[8].UpperCogAngle == 46432 && // right angle
                        cogConfigurations[7].UpperCogTargetAngularVelocity == 1200 && // was targeting 1200
                        cogConfigurations[8].UpperCogCurrentAngularVelocity == 1200 && // moved at 1200 speed
                        cogConfigurations[0].UpperCogTargetAngularVelocity == 1200; // had been targeting 1200 for some time

                    int lowerCogMinAngularVelocity = 0;
                    int lowerCogMaxAngularVelocity = 400;

                    bool lowerCogGoal =
                        lowerCogAngleDist <= 64 && // close to some right angle
                        // was moving slowly leading up to right angle
                        cogConfigurations[1].LowerCogCurrentAngularVelocity <= lowerCogMaxAngularVelocity &&
                        cogConfigurations[2].LowerCogCurrentAngularVelocity <= lowerCogMaxAngularVelocity &&
                        cogConfigurations[3].LowerCogCurrentAngularVelocity <= lowerCogMaxAngularVelocity &&
                        cogConfigurations[4].LowerCogCurrentAngularVelocity <= lowerCogMaxAngularVelocity &&
                        cogConfigurations[5].LowerCogCurrentAngularVelocity <= lowerCogMaxAngularVelocity &&
                        cogConfigurations[1].LowerCogCurrentAngularVelocity >= lowerCogMinAngularVelocity &&
                        cogConfigurations[2].LowerCogCurrentAngularVelocity >= lowerCogMinAngularVelocity &&
                        cogConfigurations[3].LowerCogCurrentAngularVelocity >= lowerCogMinAngularVelocity &&
                        cogConfigurations[4].LowerCogCurrentAngularVelocity >= lowerCogMinAngularVelocity &&
                        cogConfigurations[5].LowerCogCurrentAngularVelocity >= lowerCogMinAngularVelocity;

                    if (upperCogGoal && lowerCogGoal)
                    //if (upperCogPreGoal)
                    {
                        return frame;
                    }
                }

                //if (!upperCogEndingYaws.Any(yaw => yaw == upperCog._endingYaw))
                //    return null;
            }

            return null;
        }

        public (bool success, TtcSaveState saveState, int endFrame) FindIdealPendulumManipulation(uint pendulumAddress)
        {
            int? objectIndexNullable = ObjectUtilities.GetObjectIndex(pendulumAddress);
            if (!objectIndexNullable.HasValue) return (false, null, 0);
            int objectIndex = objectIndexNullable.Value;

            TtcPendulum pendulum = _rngObjects[objectIndex] as TtcPendulum;
            int pendulumAmplitudeStart = (int)WatchVariableSpecialUtilities.GetPendulumAmplitude(
                pendulum._accelerationDirection, pendulum._accelerationMagnitude, pendulum._angularVelocity, pendulum._angle);
            int? pendulumSwingIndexStartNullable = TableConfig.PendulumSwings.GetPendulumSwingIndex(pendulumAmplitudeStart);
            if (!pendulumSwingIndexStartNullable.HasValue) return (false, null, 0);
            int pendulumSwingIndexStart = pendulumSwingIndexStartNullable.Value;

            //iterate through frames to update objects
            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + 300)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                int pendulumAmplitude = (int)WatchVariableSpecialUtilities.GetPendulumAmplitude(
                    pendulum._accelerationDirection, pendulum._accelerationMagnitude, pendulum._angularVelocity, pendulum._angle);
                int? pendulumSwingIndexNullable = TableConfig.PendulumSwings.GetPendulumSwingIndex(pendulumAmplitude);
                if (!pendulumSwingIndexNullable.HasValue) return (false, null, 0);
                int pendulumSwingIndex = pendulumSwingIndexNullable.Value;

                if (pendulumSwingIndex > pendulumSwingIndexStart)
                {
                    if (pendulum._waitingTimer == 0)
                    {
                        return (true, GetSaveState(), frame);
                    }
                    else
                    {
                        return (false, null, 0);
                    }
                }
                else if (pendulumSwingIndex < pendulumSwingIndexStart)
                {
                    return (false, null, 0);
                }
            }

            return (false, null, 0);
        }

        public (bool success, TtcSaveState saveState, int endFrame) FindIdealPendulumManipulation2(uint pendulumAddress, bool swingSpeed)
        {
            int? objectIndexNullable = ObjectUtilities.GetObjectIndex(pendulumAddress);
            if (!objectIndexNullable.HasValue) return (false, null, 0);
            int objectIndex = objectIndexNullable.Value;
            TtcPendulum pendulum = _rngObjects[objectIndex] as TtcPendulum;
            float goalSpeed = swingSpeed ? 42 : 13;

            //iterate through frames to update objects
            int frame = _startingFrame;
            int counter = 0;
            while (true)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (pendulum._angularVelocity != 0) continue;

                if (pendulum._accelerationMagnitude == goalSpeed && pendulum._waitingTimer == 0)
                {
                    float initialAccelerationDirection = pendulum._accelerationDirection;
                    while (true)
                    {
                        frame++;
                        counter++;
                        foreach (TtcObject rngObject in _rngObjects)
                        {
                            rngObject.SetFrame(frame);
                            rngObject.Update();
                        }
                        
                        if (pendulum._accelerationDirection != initialAccelerationDirection &&
                            Math.Abs(pendulum._angularVelocity) < 2000)
                        {
                            break;
                        }
                    }
                    return (true, GetSaveState(), frame);
                }
                return (false, null, 0);
            }
        }

        public (bool success, TtcSaveState saveState, int endFrame) FindDualPendulumManipulation()
        {
            TtcPendulum pendulum1 = GetClosePendulum();
            int? pendulum1SwingIndexBaselineNullable = pendulum1.GetSwingIndex();
            if (!pendulum1SwingIndexBaselineNullable.HasValue) return (false, null, 0);
            int pendulum1SwingIndexBaseline = pendulum1SwingIndexBaselineNullable.Value;
            
            TtcPendulum pendulum2 = GetFarPendulum();
            int? pendulum2SwingIndexBaselineNullable = pendulum2.GetSwingIndex();
            if (!pendulum2SwingIndexBaselineNullable.HasValue) return (false, null, 0);
            int pendulum2SwingIndexBaseline = pendulum2SwingIndexBaselineNullable.Value;

            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + 300)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                int? pendulum1SwingIndexNullable = pendulum1.GetSwingIndex();
                if (!pendulum1SwingIndexNullable.HasValue) return (false, null, 0);
                int pendulum1SwingIndex = pendulum1SwingIndexNullable.Value;
                int pendulum1Countdown = pendulum1.GetCountdown();

                int? pendulum2SwingIndexNullable = pendulum2.GetSwingIndex();
                if (!pendulum2SwingIndexNullable.HasValue) return (false, null, 0);
                int pendulum2SwingIndex = pendulum2SwingIndexNullable.Value;
                int pendulum2Countdown = pendulum2.GetCountdown();

                // check if pendulum changed index
                if (pendulum1SwingIndex != pendulum1SwingIndexBaseline || pendulum2SwingIndex != pendulum2SwingIndexBaseline)
                {
                    // if pendulum is moving wrong way or has waiting timer, abort
                    if (pendulum1SwingIndex < pendulum1SwingIndexBaseline ||
                        pendulum2SwingIndex < pendulum2SwingIndexBaseline ||
                        pendulum1._waitingTimer > 0 ||
                        pendulum2._waitingTimer > 0)
                    {
                        return (false, null, 0);
                    }

                    // if we're in a safe zone, return
                    if (pendulum1Countdown >= 15 && pendulum2Countdown >= 15)
                    {
                        return (true, GetSaveState(), frame);
                    }

                    // update baseline to allow for more iterations
                    pendulum1SwingIndexBaseline = pendulum1SwingIndex;
                    pendulum2SwingIndexBaseline = pendulum2SwingIndex;
                }
            }

            return (false, null, 0);
        }

        public (bool success, TtcSaveState saveState, int endFrame) FindPunchRecoilPendulumManipulation()
        {
            int p1A = 315;
            int p1B = 7;
            int p2A = 297;
            int p2B = 11;

            TtcPendulum pendulum1 = GetClosePendulum();
            int pendulum1AmplitudeBaseline = pendulum1.GetAmplitude();
            (int, int)? pendulum1SwingIndexBaselineNullable = pendulum1.GetSwingIndexExtendedPair();
            if (!pendulum1SwingIndexBaselineNullable.HasValue) return (false, null, 0);
            (int pendulum1SwingIndexBaselineA, int pendulum1SwingIndexBaselineB) = pendulum1SwingIndexBaselineNullable.Value;

            TtcPendulum pendulum2 = GetFarPendulum();
            int pendulum2AmplitudeBaseline = pendulum2.GetAmplitude();
            (int, int)? pendulum2SwingIndexBaselineNullable = pendulum2.GetSwingIndexExtendedPair();
            if (!pendulum2SwingIndexBaselineNullable.HasValue) return (false, null, 0);
            (int pendulum2SwingIndexBaselineA, int pendulum2SwingIndexBaselineB) = pendulum2SwingIndexBaselineNullable.Value;

            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + 1000)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                int pendulum1Amplitude = pendulum1.GetAmplitude();
                (int, int)? pendulum1SwingIndexNullable = pendulum1.GetSwingIndexExtendedPair();
                if (!pendulum1SwingIndexNullable.HasValue) return (false, null, 0);
                (int pendulum1SwingIndexA, int pendulum1SwingIndexB) = pendulum1SwingIndexNullable.Value;
                int pendulum1Countdown = pendulum1.GetCountdown();

                int pendulum2Amplitude = pendulum2.GetAmplitude();
                (int, int)? pendulum2SwingIndexNullable = pendulum2.GetSwingIndexExtendedPair();
                if (!pendulum2SwingIndexNullable.HasValue) return (false, null, 0);
                (int pendulum2SwingIndexA, int pendulum2SwingIndexB) = pendulum2SwingIndexNullable.Value;
                int pendulum2Countdown = pendulum2.GetCountdown();

                // check if pendulum changed index
                if (pendulum1Amplitude != pendulum1AmplitudeBaseline ||
                    pendulum2Amplitude != pendulum2AmplitudeBaseline)
                {
                    // if pendulum is moving wrong way or has waiting timer, abort

                    if (pendulum1Amplitude != pendulum1AmplitudeBaseline)
                    {
                        if (pendulum1SwingIndexBaselineA == p1A)
                        {
                            if (pendulum1SwingIndexBaselineB == p1B)
                            {
                                bool satisfiesA = pendulum1SwingIndexA == pendulum1SwingIndexBaselineA;
                                bool satisfiesB = pendulum1SwingIndexB == pendulum1SwingIndexBaselineB - 1;
                                if (!satisfiesA || !satisfiesB) return (false, null, 0);
                            }
                            else
                            {
                                bool satisfiesA = pendulum1SwingIndexA == pendulum1SwingIndexBaselineA;
                                bool satisfiesB = pendulum1SwingIndexB == pendulum1SwingIndexBaselineB + 1;
                                if (!satisfiesA || !satisfiesB) return (false, null, 0);
                            }
                        }
                        else
                        {
                            bool satisfiesA = pendulum1SwingIndexA == pendulum1SwingIndexBaselineA + 1;
                            bool satisfiesB = pendulum1SwingIndexB == 0;
                            if (!satisfiesA || !satisfiesB) return (false, null, 0);
                        }
                    }

                    if (pendulum2Amplitude != pendulum2AmplitudeBaseline)
                    {
                        if (pendulum2SwingIndexBaselineA == p2A)
                        {
                            if (pendulum2SwingIndexBaselineB == p2B)
                            {
                                bool satisfiesA = pendulum2SwingIndexA == pendulum2SwingIndexBaselineA;
                                bool satisfiesB = pendulum2SwingIndexB == pendulum2SwingIndexBaselineB - 1;
                                if (!satisfiesA || !satisfiesB) return (false, null, 0);
                            }
                            else
                            {
                                bool satisfiesA = pendulum2SwingIndexA == pendulum2SwingIndexBaselineA;
                                bool satisfiesB = pendulum2SwingIndexB == pendulum2SwingIndexBaselineB + 1;
                                if (!satisfiesA || !satisfiesB) return (false, null, 0);
                            }
                        }
                        else
                        {
                            bool satisfiesA = pendulum2SwingIndexA == pendulum2SwingIndexBaselineA + 1;
                            bool satisfiesB = pendulum2SwingIndexB == 0;
                            if (!satisfiesA || !satisfiesB) return (false, null, 0);
                        }
                    }

                    if (pendulum1._waitingTimer > 0 || pendulum2._waitingTimer > 0)
                    {
                        return (false, null, 0);
                    }

                    // if we're in a safe zone, return
                    if (frame >= GetMaxDustFrame() && pendulum1Countdown >= 20 && pendulum2Countdown >= 20)
                    {
                        return (true, GetSaveState(), frame);
                    }

                    // update baseline to allow for more iterations
                    pendulum1AmplitudeBaseline = pendulum1Amplitude;
                    pendulum1SwingIndexBaselineA = pendulum1SwingIndexA;
                    pendulum1SwingIndexBaselineB = pendulum1SwingIndexB;

                    pendulum2AmplitudeBaseline = pendulum2Amplitude;
                    pendulum2SwingIndexBaselineA = pendulum2SwingIndexA;
                    pendulum2SwingIndexBaselineB = pendulum2SwingIndexB;
                }
            }

            return (false, null, 0);
        }

        // simulates n frames and branches off if it starts the sequence
        public (bool success, int startFrame, List<int> dustFrames) FindPunchRecoilSetup1()
        {
            TtcPendulum pendulum1 = GetClosePendulum();
            TtcPendulum pendulum2 = GetFarPendulum();
            int maxDustFrame = GetMaxDustFrame();

            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + 2000)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                (int p1A, int p1B) = pendulum1.GetSwingIndexExtendedPair().Value;
                if (p1B < 888 || p1B > 890) return (false, 0, null);
                
                (int p2A, int p2B) = pendulum2.GetSwingIndexExtendedPair().Value;
                if (p2B < 155 || p2B > 157) return (false, 0, null);

                // pendulum is starting pre swing
                if (frame > maxDustFrame && pendulum2.HasState(1, 13, 0, -12375, 0))
                {
                    TtcSimulation clone = Clone();
                    (bool success, List<int> dustFrames) = clone.FindPunchRecoilSetup2(frame);
                    if (success) return (true, frame, dustFrames);
                }
            }

            return (false, 0, null);
        }

        private List<int> punchRecoilRelDustFrames =
            new List<int>()
            {
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                61,
                64,
                96,
                97,
                98,
            };

        public (bool success, List<int> dustFrames) FindPunchRecoilSetup2(int startFrame)
        {
            TtcPendulum pendulum1 = GetClosePendulum();
            TtcPendulum pendulum2 = GetFarPendulum();
            TtcPitBlock pitBlock = GetPitBlock();
            TtcHand hand = GetLowerHand();
            TtcBobomb bobomb1 = GetFirstBobomb();
            TtcBobomb bobomb2 = GetSecondBobomb();

            int? frame1_p2AccChange = null;
            int? frame2_p2SwingStart = null;

            int frame = startFrame;
            int counter = 0;
            while (frame < startFrame + 1000)
            {
                frame++;
                counter++;

                if (frame2_p2SwingStart.HasValue)
                {
                    if (frame == frame2_p2SwingStart.Value + 111)
                    {
                        bobomb1.SetWithinMarioRange(1);
                    }

                    if (frame == frame2_p2SwingStart.Value + 127)
                    {
                        bobomb1.SetWithinMarioRange(0);
                    }

                    if (frame == frame2_p2SwingStart.Value + 134)
                    {
                        bobomb1.SetWithinMarioRange(1);
                    }

                    if (frame == frame2_p2SwingStart.Value + 138)
                    {
                        bobomb2.SetWithinMarioRange(1);
                    }
                }

                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (frame == startFrame + 33)
                {
                    if (pitBlock._height != 259)
                    {
                        return (false, null);
                    }
                }

                // when p2 changes acc, check that it changed to the right one
                if (!frame1_p2AccChange.HasValue && pendulum2._angularVelocity == 0)
                {
                    if (pendulum2._accelerationMagnitude == 42)
                    {
                        return (false, null);
                    }
                    frame1_p2AccChange = frame;
                }
                
                // p2 is starting its swing, so add the dust frames of the movement
                if (!frame2_p2SwingStart.HasValue && frame1_p2AccChange.HasValue && pendulum2._waitingTimer == 0)
                {
                    frame2_p2SwingStart = frame;
                    List<int> dustFrames = punchRecoilRelDustFrames.ConvertAll(rel => frame + rel);
                    AddDustFrames(dustFrames);
                }

                // now we know the exact frame everything should happen
                if (frame2_p2SwingStart.HasValue)
                {
                    if (frame == frame2_p2SwingStart.Value + 95)
                    {
                        if (!pendulum1.HasState(1, 42, 336, -10825, 0))
                        {
                            //Config.Print("FAILED: " + pendulum1);
                            return (false, null);
                        }
                    }

                    if (frame == frame2_p2SwingStart.Value + 100)
                    {
                        _rng.PollRNG(40);
                    }

                    int delta = frame - frame2_p2SwingStart.Value;
                    if (delta >= 111 && delta <= 125 && delta % 2 == 1)
                    {
                        _rng.PollRNG(3);
                    }

                    if (frame == frame2_p2SwingStart.Value + 152)
                    {
                        if (hand._angle != 296)
                        {
                            return (false, null);
                        }
                    }
                }

                //Config.Print(frame + ": " + this);
            }

            return (true, GetDustFrames());
        }

        // simulates n frames and branches off if it starts the sequence
        public (bool success, int startFrame) FindMidairWalkingSetup1()
        {
            TtcPendulum pendulum1 = GetClosePendulum();
            TtcPendulum pendulum2 = GetFarPendulum();
            int maxDustFrame = GetMaxDustFrame();

            int frame = _startingFrame;
            int counter = 0;
            while (frame < _startingFrame + 1000)
            {
                frame++;
                counter++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                // at 7
                (int p1A, int p1B) = pendulum1.GetSwingIndexExtendedPair().Value;
                if (p1B < 6 || p1B > 8) return (false, 0);

                // 10 -> 11
                (int p2A, int p2B) = pendulum2.GetSwingIndexExtendedPair().Value;
                if (p2B < 9 || p2B > 11) return (false, 0);

                // pendulum is starting pre swing
                if (frame > maxDustFrame && pendulum2.HasState(1, 13, 0, -13955, 0))
                {
                    TtcSimulation clone = Clone();
                    bool success = clone.FindMidairWalkingSetup2(frame);
                    if (success) return (true, frame);
                }
            }

            return (false, 0);
        }

        public bool FindMidairWalkingSetup2(int startFrame)
        {
            TtcPendulum pendulum1 = GetClosePendulum();
            TtcPendulum pendulum2 = GetFarPendulum();

            int frame = startFrame;
            int counter = 0;
            while (frame < startFrame + 1000)
            {
                frame++;
                counter++;

                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (counter == 92)
                {
                    if (pendulum2._waitingTimer < 10) return false;
                }

                if (counter >= 170 && counter <= 180)
                {
                    if (pendulum1._angle == 43852 && pendulum1._waitingTimer > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class CogConfiguration
        {
            public readonly int UpperCogAngle;
            public readonly int UpperCogCurrentAngularVelocity;
            public readonly int UpperCogTargetAngularVelocity;
            public readonly int LowerCogAngle;
            public readonly int LowerCogCurrentAngularVelocity;
            public readonly int LowerCogTargetAngularVelocity;

            public CogConfiguration(TtcCog upperCog, TtcCog lowerCog)
            {
                UpperCogAngle = upperCog._angle;
                UpperCogCurrentAngularVelocity = upperCog._currentAngularVelocity;
                UpperCogTargetAngularVelocity = upperCog._targetAngularVelocity;
                LowerCogAngle = lowerCog._angle;
                LowerCogCurrentAngularVelocity = lowerCog._currentAngularVelocity;
                LowerCogTargetAngularVelocity = lowerCog._targetAngularVelocity;
            }
        }

        public static int FindHandMovement(TtcSaveState saveState, int startingFrame)
        {
            TtcSimulation simulation = new TtcSimulation(saveState, startingFrame, new List<int>());
            return simulation.FindHandMovement();
        }

        public int FindHandMovement()
        {
            ushort startAngle = 48700;
            ushort endAngle = 3912;
            ushort resetAngle = 44000;
            int margin = 100;

            TtcHand hand = _rngObjects[37] as TtcHand;

            bool goingForItBool = false;
            int goingForItFrame = 0;
            int bestDist = int.MinValue;
            int totalDist = (int)MoreMath.GetAngleDistance(startAngle, endAngle);

            int frame = _startingFrame;
            for (int counter = 0; true; counter++)
            {
                if (frame % 1000000 == 0)
                {
                    //Config.Print("...frame {0}", frame);
                    return 1000000;
                }

                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                bool atStartAngle = MoreMath.GetAngleDistance(hand._angle, startAngle) <= margin;
                bool atEndAngle = MoreMath.GetAngleDistance(hand._angle, endAngle) <= margin;
                bool atResetAngle = MoreMath.GetAngleDistance(hand._angle, resetAngle) <= margin;

                if (goingForItBool)
                {
                    if (atStartAngle)
                    {
                        goingForItBool = true;
                        goingForItFrame = frame;
                        //Config.Print("Start again on frame {0}", frame);
                    }
                    else if (atEndAngle)
                    {
                        //Config.Print("End on frame {0}", frame);
                        //Config.Print("Success from {0} to {1}", goingForItFrame, frame);
                        return goingForItFrame;
                    }
                    else if (atResetAngle)
                    {
                        goingForItBool = false;
                        //Config.Print("Reset on frame {0}", frame);
                    }
                }
                else
                {
                    if (atStartAngle)
                    {
                        goingForItBool = true;
                        goingForItFrame = frame;
                        //Config.Print("Start on frame {0}", frame);
                    }
                    else if (atEndAngle)
                    {

                    }
                    else if (atResetAngle)
                    {

                    }
                }

                if (goingForItBool)
                {
                    int currentDist = (int)MoreMath.GetAngleDifference(startAngle, hand._angle);
                    if (currentDist > bestDist)
                    {
                        bestDist = currentDist;
                        /*
                        Config.Print(
                            "Frame {0} has dist {1} of {2} ({3})",
                            frame,
                            currentDist,
                            totalDist,
                            MoreMath.GetPercentString(currentDist, totalDist, 2));
                            */
                    }
                }
            }
        }

        public List<int> FindKeyHandFrames()
        {
            List<int> pendulumAnglesForDust = new List<int>()
            {
                -103861, -37756, 26919, 93440,
            };
            List<int> output = new List<int>();

            TtcPendulum pendulum = GetClosePendulum();
            int initialAmplitude = pendulum.GetAmplitude();

            int frame = _startingFrame;
            for (int counter = 0; true; counter++)
            {
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }
                
                if (pendulumAnglesForDust.Contains((int)pendulum._angle))
                {
                    output.Add(frame);
                }

                if (pendulum.GetAmplitude() != initialAmplitude)
                {
                    output.Add(frame);
                    if (output.Count != 5) throw new ArgumentOutOfRangeException();
                    return output;
                }
            }
        }

        public void SimulateUntilFrame(int endingFrame)
        {
            while (true)
            {
                _currentFrame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(_currentFrame);
                    rngObject.Update();
                }

                if (_currentFrame == endingFrame) return;
            }
        }

        public void SimulateNumFrames(int numFrames)
        {
            for (int i = 0; i < numFrames; i++)
            {
                _currentFrame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(_currentFrame);
                    rngObject.Update();
                }
            }
        }

        public TtcPendulum GetClosePendulum()
        {
            return _rngObjects[8] as TtcPendulum;
        }

        public TtcPendulum GetFarPendulum()
        {
            return _rngObjects[9] as TtcPendulum;
        }

        public TtcHand GetUpperHand()
        {
            return _rngObjects[37] as TtcHand;
        }

        public TtcHand GetLowerHand()
        {
            return _rngObjects[38] as TtcHand;
        }

        public TtcSpinner GetLowestSpinner()
        {
            return _rngObjects[47] as TtcSpinner;
        }

        public ushort GetRng()
        {
            return _rng.GetRng();
        }

        public TtcPendulum GetReentryPendulum()
        {
            return _rngObjects[10] as TtcPendulum;
        }

        public TtcBobomb GetFirstBobomb()
        {
            return _rngObjects[67] as TtcBobomb;
        }

        public TtcBobomb GetSecondBobomb()
        {
            return _rngObjects[68] as TtcBobomb;
        }

        public TtcPusher GetMiddlePusher()
        {
            return _rngObjects[19] as TtcPusher;
        }

        public TtcPusher GetUpperPusher()
        {
            return _rngObjects[20] as TtcPusher;
        }

        public TtcPitBlock GetPitBlock()
        {
            return _rngObjects[36] as TtcPitBlock;
        }

        // Given dust, goes forward and spawns height swings to investigate
        public void FindIdealReentryManipulationGivenDustFrames(List<int> dustFrames)
        {
            int phase1Limit = 1000;

            int maxDustFrame = dustFrames.Count == 0 ? 0 : dustFrames.Max();
            int counter = 0;
            int frame = _startingFrame;
            while (frame < _startingFrame + phase1Limit)
            {
                counter++;
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                // Check if pendulum will do height swing after all dust has been made
                TtcPendulum pendulum = GetReentryPendulum();
                if (frame > maxDustFrame &&
                    pendulum._accelerationDirection == -1 &&
                    pendulum._accelerationMagnitude == 13 &&
                    pendulum._angularVelocity == 0 &&
                    pendulum._waitingTimer == 0 &&
                    pendulum._angle == 42748)
                {
                    TtcSimulation simulation = new TtcSimulation(GetSaveState(), frame, new List<int>());
                    simulation.FindIdealReentryManipulationGivenFrame1(dustFrames, frame);
                }
            }
        }

        // Given frame 1, goes forward and spawns wall push swings to investigate
        // Frame 1 is the frame at the start of the pendulum swing that lets Mario get the right height
        public void FindIdealReentryManipulationGivenFrame1(List<int> dustFrames, int frame1)
        {
            //Config.Print("TRY\t{0}\t{1}", frame1, "[" + string.Join(",", dustFrames) + "]");
            int phase2Limit = 1000;

            TtcPendulum pendulum = GetReentryPendulum();
            TtcBobomb firstBobomb = GetFirstBobomb();
            TtcBobomb secondBobomb = GetSecondBobomb();
            TtcBobomb thirdBobomb = null;
            TtcBobomb fourthBobomb = null;

            int counter = 0;
            int frame = _startingFrame;
            while (frame < _startingFrame + phase2Limit)
            {
                counter++;
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    // coin for bobomb 1
                    if (counter == 162 && rngObject == firstBobomb)
                    {
                        _rng.PollRNG(3);
                    }
                    // coin for bobomb 2
                    if (counter == 258 && rngObject == secondBobomb)
                    {
                        _rng.PollRNG(3);
                    }
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                // bob-omb 2 start
                if (counter == 19)
                {
                    secondBobomb.SetWithinMarioRange(1);
                }

                // bob-omb 2 end, bob-omb 4 start
                if (counter == 258)
                {
                    _rngObjects.Remove(secondBobomb);
                    fourthBobomb = new TtcBobomb(_rng, 0, 0); // starts outside range
                    _rngObjects.Insert(68, fourthBobomb);
                }

                // bob-omb 1 start
                if (counter == 154)
                {
                    firstBobomb.SetWithinMarioRange(1);
                }

                // bob-omb 1 end, bob-omb 3 start
                if (counter == 162)
                {
                    _rngObjects.Remove(firstBobomb);
                    thirdBobomb = new TtcBobomb(_rng, 0, 1); // starts inside range
                    _rngObjects.Insert(68, thirdBobomb);
                }

                // bob-omb 3 exiting range
                if (counter == 363)
                {
                    thirdBobomb.SetWithinMarioRange(0);
                }

                // dust frames
                if (counter >= 84 && counter <= 95 && counter != 93)
                {
                    _rng.PollRNG(4);
                }

                // bob-omb 2 fuse smoke
                if ((counter >= 99 && counter <= 211 && counter % 8 == 3) ||
                    (counter >= 219 && counter <= 257 && counter % 2 == 1))
                {
                    _rng.PollRNG(3);
                }

                // bob-omb 1 fuse smoke
                if (counter >= 156 && counter <= 162 && counter % 2 == 0)
                {
                    _rng.PollRNG(3);
                }

                // pendulum must have enough waiting frames
                if (counter == 162)
                {
                    bool pendulumQualifies = pendulum._waitingTimer >= 18;
                    if (!pendulumQualifies) return;
                }

                // Check if pendulum will do wall push swing
                if (counter > 363 + 15 &&
                    pendulum._accelerationDirection == -1 &&
                    pendulum._accelerationMagnitude == 42 &&
                    pendulum._angularVelocity == 0 &&
                    pendulum._waitingTimer == 0 &&
                    pendulum._angle == 42748)
                {
                    TtcSimulation simulation = new TtcSimulation(GetSaveState(), frame, new List<int>());
                    simulation.FindIdealReentryManipulationGivenFrame2(dustFrames, frame1, frame);
                }

                //Config.Print(frame + "\t" + _rng.GetIndex() + "\t" + GetSaveState());
            }
        }

        // Investigates a wall push swing to see if it qualifies
        // Frame 2 is the frame at the start of the pendulum swing that lets Mario get wall displacement
        public void FindIdealReentryManipulationGivenFrame2(List<int> dustFrames, int frame1, int frame2)
        {
            //Config.Print("ATTEMPT\t{0}\t{1}\t{2}", frame1, frame2, "[" + string.Join(",", dustFrames) + "]");
            int counter = 0;
            int frame = _startingFrame;
            while (true)
            {
                counter++;
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                // bob-omb 1 is in range
                if (counter == 63)
                {
                    GetFirstBobomb().SetWithinMarioRange(1);
                }

                // collecting star particles
                if (counter == 66)
                {
                    _rng.PollRNG(80);
                }

                // bob-omb 2 is in range
                if (counter == 70)
                {
                    GetSecondBobomb().SetWithinMarioRange(1);
                }

                // hand is in position
                if (counter == 77)
                {
                    TtcHand hand = GetLowerHand();
                    int min = 36700;
                    int max = 39400;
                    bool handQualifies = hand._angle >= min && hand._angle <= max;
                    if (!handQualifies) return;
                }

                // spinner is in position
                if (counter == 122)
                {
                    TtcSpinner spinner = GetLowestSpinner();
                    int min = 12600;
                    int max = 14700;
                    bool spinnerAngleQualifies =
                        (spinner._angle >= min && spinner._angle <= max) ||
                        (spinner._angle >= min + 32768 && spinner._angle <= max + 32768);
                    bool spinnerDirectionQualifies = spinner._direction == -1;
                    bool spinnerQualifies = spinnerAngleQualifies && spinnerDirectionQualifies;
                    if (!spinnerQualifies) return;

                    List<int> inputDustFrames = dustFrames.ConvertAll(dustFrame => dustFrame - 2);
                    Config.Print("SUCCESS\t{0}\t{1}\t{2}\t", frame1, frame2, "[" + string.Join(",", inputDustFrames) + "]");
                    return;
                }
            }
        }

        public int? FindPendulumSyncingManipulation()
        {
            int limit = 500;

            int counter = 0;
            _currentFrame = _startingFrame;
            while (_currentFrame < _startingFrame + limit)
            {
                counter++;
                _currentFrame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(_currentFrame);
                    rngObject.Update();
                }

                TtcPendulum pendulum1 = GetClosePendulum();
                TtcPendulum pendulum2 = GetFarPendulum();
                if (pendulum1._accelerationDirection == pendulum2._accelerationDirection &&
                    pendulum1._accelerationMagnitude == pendulum2._accelerationMagnitude &&
                    pendulum1._angularVelocity == pendulum2._angularVelocity &&
                    pendulum1._waitingTimer == pendulum2._waitingTimer &&
                    pendulum1._angle == pendulum2._angle)
                {
                    return _currentFrame;
                }
            }
            return null;
        }

        public void FindMovingBarManipulationGivenDustFrames(List<int> dustFrames)
        {
            int limit = 1000;

            TtcPendulum closePendulum = GetClosePendulum();
            TtcPendulum farPendulum = GetFarPendulum();

            int maxDustFrame = dustFrames.Count == 0 ? 0 : dustFrames.Max();
            int counter = 0;
            int frame = _startingFrame;
            while (frame < _startingFrame + limit)
            {
                counter++;
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (frame > maxDustFrame &&
                    farPendulum._accelerationDirection == -1 &&
                    farPendulum._accelerationMagnitude == 13 &&
                    farPendulum._angularVelocity == 0 &&
                    farPendulum._waitingTimer == 0 &&
                    farPendulum._angle == 34440)
                {
                    TtcSimulation simulation = new TtcSimulation(GetSaveState(), frame, new List<int>());
                    simulation.FindMovingBarManipulationGivenFrame1(dustFrames, frame);
                }

                (int, int)? closePair = TableConfig.PendulumSwings.GetPendulumSwingIndexExtendedPair(closePendulum.GetAmplitude());
                (int, int)? farPair = TableConfig.PendulumSwings.GetPendulumSwingIndexExtendedPair(farPendulum.GetAmplitude());
                if (!closePair.HasValue || !farPair.HasValue) return;
                (int c1, int c2) = closePair.Value;
                (int f1, int f2) = farPair.Value;
                if (c1 != 306) return;
                if (f1 != 310) return;
            }
        }

        public void FindMovingBarManipulationGivenFrame1(List<int> dustFrames, int frame1)
        {
            TtcPendulum closePendulum = GetClosePendulum();
            TtcPendulum farPendulum = GetFarPendulum();
            TtcPusher middlePusher = GetMiddlePusher();
            TtcPusher upperPusher = GetUpperPusher();

            int? pendulumAngleCounter = null;

            int counter = 0;
            int frame = _startingFrame;
            while (true)
            {
                counter++;
                frame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(frame);
                    rngObject.Update();
                }

                if (counter == 142)
                {
                    if (!middlePusher.IsExtended()) return;
                }

                if (counter > 142 &&
                    middlePusher.IsRetracting() &&
                    middlePusher._timer < 50 &&
                    closePendulum._angle == 27477 &&
                    upperPusher.IsExtended())
                {
                    pendulumAngleCounter = counter;
                    Config.Print("SUCCESS\t{0}\t{1}\t{2}", frame1, frame, TtcMain.FormatDustFrames(dustFrames));
                }

                if (counter == 300)
                {
                    return;
                }
            }
        }

        public void OutputPendulumData()
        {
            Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
            List<int> lastPendulumState = new List<int>() { 0, 0, 0, 0 };
            dictionary.Add(0, lastPendulumState);

            List<TtcPendulum> pendulums = new List<TtcPendulum>()
            {
                (TtcPendulum)_rngObjects[8],
                (TtcPendulum)_rngObjects[9],
                (TtcPendulum)_rngObjects[10],
                (TtcPendulum)_rngObjects[11],
            };

            for (int i = 0; true; i++)
            {
                _currentFrame++;
                foreach (TtcObject rngObject in _rngObjects)
                {
                    rngObject.SetFrame(_currentFrame);
                    rngObject.Update();
                }

                List<int?> pendulumStateNullable = pendulums.ConvertAll(p => p.GetSwingIndex());
                if (pendulumStateNullable.Any(index => !index.HasValue)) break;
                List<int> pendulumState = pendulumStateNullable.ConvertAll(index => index.Value);
                if (!Enumerable.SequenceEqual(lastPendulumState, pendulumState))
                {
                    dictionary.Add(_currentFrame, pendulumState);
                    lastPendulumState = pendulumState;
                }
            }

            List<string> outputLines = new List<string>();
            foreach (int frame in dictionary.Keys)
            {
                List<int> pendulumState = dictionary[frame];
            }
            InfoForm.ShowValue(string.Join("\r\n", outputLines));
        }

        public XElement ToXml()
        {
            XElement xElement = new XElement("TtcState");
            xElement.Add(_rng.ToXml());
            foreach (TtcObject obj in _rngObjects)
            {
                xElement.Add(obj.ToXml());
            }
            return xElement;
        }
    }
}
