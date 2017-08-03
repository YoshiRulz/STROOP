﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SM64_Diagnostic.Structs;
using SM64_Diagnostic.Structs.Configurations;
using System.Windows.Forms;
using static SM64_Diagnostic.Structs.Configurations.Config;

namespace SM64_Diagnostic.Managers
{
    public class OptionsManager
    {
        public OptionsManager(TabPage tabControl)
        {
            GroupBox groupBoxRomVersion = tabControl.Controls["groupBoxRomVersion"] as GroupBox;
            RadioButton radioButtonRomVersionUS = groupBoxRomVersion.Controls["radioButtonRomVersionUS"] as RadioButton;
            radioButtonRomVersionUS.Click += (sender, e) => { Config.Version = RomVersion.US; };
            RadioButton radioButtonRomVersionJP = groupBoxRomVersion.Controls["radioButtonRomVersionJP"] as RadioButton;
            radioButtonRomVersionJP.Click += (sender, e) => { Config.Version = RomVersion.JP; };
            RadioButton radioButtonRomVersionPAL = groupBoxRomVersion.Controls["radioButtonRomVersionPAL"] as RadioButton;
            radioButtonRomVersionPAL.Click += (sender, e) => { Config.Version = RomVersion.PAL; };

            GroupBox groupBoxGotoRetrieveOffsets = tabControl.Controls["groupBoxGotoRetrieveOffsets"] as GroupBox;
            TextBox textBoxGotoAbove = groupBoxGotoRetrieveOffsets.Controls["textBoxGotoAbove"] as TextBox;
            textBoxGotoAbove.LostFocus += this.textBoxGotoAbove_LostFocus;
            TextBox textBoxGotoInfront = groupBoxGotoRetrieveOffsets.Controls["textBoxGotoInfront"] as TextBox;
            textBoxGotoInfront.LostFocus += this.textBoxGotoInfront_LostFocus;
            TextBox textBoxRetrieveAbove = groupBoxGotoRetrieveOffsets.Controls["textBoxRetrieveAbove"] as TextBox;
            textBoxRetrieveAbove.LostFocus += this.textBoxRetrieveAbove_LostFocus;
            TextBox textBoxRetrieveInfront = groupBoxGotoRetrieveOffsets.Controls["textBoxRetrieveInfront"] as TextBox;
            textBoxRetrieveInfront.LostFocus += this.textBoxRetrieveInfront_LostFocus;
        }

        private void textBoxGotoAbove_LostFocus(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse((sender as TextBox).Text, out value))
            {
                Config.GotoRetrieve.GotoAboveOffset = value;
            }
            else
            {
                Config.GotoRetrieve.GotoAboveOffset = Config.GotoRetrieve.GotoAboveDefault;
                (sender as TextBox).Text = Config.GotoRetrieve.GotoAboveDefault.ToString();
            }
        }

        private void textBoxGotoInfront_LostFocus(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse((sender as TextBox).Text, out value))
            {
                Config.GotoRetrieve.GotoInfrontOffset = value;
            }
            else
            {
                Config.GotoRetrieve.GotoInfrontOffset = Config.GotoRetrieve.GotoInfrontDefault;
                (sender as TextBox).Text = Config.GotoRetrieve.GotoInfrontDefault.ToString();
            }
        }

        private void textBoxRetrieveAbove_LostFocus(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse((sender as TextBox).Text, out value))
            {
                Config.GotoRetrieve.RetrieveAboveOffset = value;
            }
            else
            {
                Config.GotoRetrieve.RetrieveAboveOffset = Config.GotoRetrieve.RetrieveAboveDefault;
                (sender as TextBox).Text = Config.GotoRetrieve.RetrieveAboveDefault.ToString();
            }
        }

        private void textBoxRetrieveInfront_LostFocus(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse((sender as TextBox).Text, out value))
            {
                Config.GotoRetrieve.RetrieveInfrontOffset = value;
            }
            else
            {
                Config.GotoRetrieve.RetrieveInfrontOffset = Config.GotoRetrieve.RetrieveInfrontDefault;
                (sender as TextBox).Text = Config.GotoRetrieve.RetrieveInfrontDefault.ToString();
            }
        }
    }
}
