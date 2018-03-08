﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using STROOP.Utilities;
using STROOP.Structs;
using STROOP.Extensions;
using System.Reflection;
using STROOP.Managers;
using STROOP.Structs.Configurations;

namespace STROOP.Controls
{
    /**
     * Class for applying settings to a watch var control and wrapper.
     * For each setting, there are 3 variables:
     * (1) ChangeSetting: a boolean whether this setting should be changed
     * (2) ChangeSettingToDefault: a boolean whether the change should be to the default value
     * (3) NewSetting: the new value if we're not using the default value
     * 
     * When constructing this class, for each setting, either leave all 3 variables out, or:
     * (1) Set changeSetting to true and changeSettingToDefault to true
     * (2) Set changeSetting to true and newSetting to the new value
     */
    public class WatchVariableControlSettings
    {
        public readonly bool ChangeAngleSigned;
        public readonly bool ChangeAngleSignedToDefault;
        public readonly bool NewAngleSigned;

        public WatchVariableControlSettings(
            bool changeAngleSigned = false,
            bool changeAngleSignedToDefault = false,
            bool newAngleSigned = false)
        {
            ChangeAngleSigned = changeAngleSigned;
            ChangeAngleSignedToDefault = changeAngleSignedToDefault;
            NewAngleSigned = newAngleSigned;
        }

    }
}
