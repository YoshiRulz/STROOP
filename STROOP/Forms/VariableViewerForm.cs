﻿using STROOP.Structs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using STROOP.Extensions;
using STROOP.Utilities;

namespace STROOP.Forms
{
    public partial class VariableViewerForm : Form
    {
        public VariableViewerForm(
            string name, string clazz, string type, string baseTypeOffset, string n64BaseAddress, string emulatorBaseAddress, string n64Address, string emulatorAddress)
        {
            InitializeComponent();

            textBoxVariableName.Text = name;
            textBoxClassValue.Text = clazz;
            textBoxTypeValue.Text = type;
            textBoxBaseTypeOffsetValue.Text = baseTypeOffset;

            textBoxN64BaseAddressValue.Text = n64BaseAddress;
            textBoxEmulatorBaseAddressValue.Text = emulatorBaseAddress;
            textBoxN64AddressValue.Text = n64Address;
            textBoxEmulatorAddressValue.Text = emulatorAddress;

            buttonOk.Click += (sender, e) => Close();
        }
    }
}
