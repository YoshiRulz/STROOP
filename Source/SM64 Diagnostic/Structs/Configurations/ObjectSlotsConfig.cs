﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public struct ObjectSlotsConfig
    {
        public int MaxSlots;
        public uint HoverObjectAddress;
        public uint LinkStartAddress;
        public uint StructSize;
        public uint HeaderOffset;
        public uint NextLinkOffset;
        public uint PreviousLinkOffset;
        public uint ParentOffset;
        public uint UnusedSlotAddress;
        public uint BehaviorScriptOffset;
        public uint BehaviorGfxOffset;
        public uint BehaviorSubtypeOffset;
        public uint BehaviorAppearance;
        public uint ObjectActiveOffset;
        public uint AnimationOffset;

        public uint ObjectXOffset;
        public uint ObjectYOffset;
        public uint ObjectZOffset;
        public uint HomeXOffset;
        public uint HomeYOffset;
        public uint HomeZOffset;
        public uint ObjectRotationOffset;

        public uint HitboxRadius;
        public uint HitboxHeight;
        public uint HitboxDownOffset;

        public uint YawFacingOffset;
        public uint PitchFacingOffset;
        public uint RollFacingOffset;
        public uint YawMovingOffset;
        public uint PitchMovingOffset;
        public uint RollMovingOffset;

        public uint ScaleWidthOffset;
        public uint ScaleHeightOffset;
        public uint ScaleDepthOffset;

        public uint ReleaseStatusOffset;
        public uint StackIndexOffset;
        public uint StackIndexReleasedValue;
        public uint StackIndexUnReleasedValue;
        public uint InitialReleaseStatusOffset;
        public uint ReleaseStatusThrownValue;
        public uint ReleaseStatusDroppedValue;
        public uint InteractionStatusOffset;

        public uint PendulumAccelerationDirection;
        public uint PendulumAccelerationMagnitude;
        public uint PendulumAngularVelocity;
        public uint PendulumAngle;

        public uint WaypointOffset;
        public uint PitchToWaypointOffset;
        public uint RacingPenguinEffortOffset;
        public uint KoopaTheQuickHSpeedMultiplierOffset;
    }
}
