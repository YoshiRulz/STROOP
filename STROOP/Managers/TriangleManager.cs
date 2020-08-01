using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using STROOP.Models;
using STROOP.Structs;
using STROOP.Utilities;
using STROOP.Controls;
using STROOP.Extensions;
using STROOP.Structs.Configurations;
using STROOP.Forms;

namespace STROOP.Managers
{
    public class TriangleManager : DataManager
    {
        private Dictionary<uint, TriangleDataModel> _triangleCache;

        MaskedTextBox _addressBox;
        uint _triangleAddress = 0;
        CheckBox _useMisalignmentOffsetCheckbox;

        public enum TriangleMode { Floor, Wall, Ceiling, Other };
        public TriangleMode Mode = TriangleMode.Floor;

        private readonly RadioButton _radioButtonTriFloor;
        private readonly RadioButton _radioButtonTriWall;
        private readonly RadioButton _radioButtonTriCeiling;
        private readonly RadioButton _radioButtonTriOther;

        CheckBox _checkBoxNeutralizeTriangle;

        CheckBox _recordTriangleDataCheckbox;
        CheckBox _repeatFirstVertexCheckbox;
        Label _recordTriangleCountLabel;
        List<uint> _recordedTriangleAddresses;

        public uint TrianglePointerAddress
        {
            get;
            private set;
        }

        public uint TriangleAddress
        {
            get
            {
                return _triangleAddress;
            }
            private set
            {
                // update cache
                if (value != 0) GetTriangleStruct(value);

                if (_triangleAddress == value)
                    return;

                _triangleAddress = value;
                _addressBox.Text = HexUtilities.FormatValue(_triangleAddress, 8);
            }
        }

        private static readonly List<VariableGroup> ALL_VAR_GROUPS =
            new List<VariableGroup>()
            {
                VariableGroup.Basic,
                VariableGroup.Intermediate,
                VariableGroup.Advanced,
                VariableGroup.Self,
                VariableGroup.ExtendedLevelBoundaries,
            };

        private static readonly List<VariableGroup> VISIBLE_VAR_GROUPS =
            new List<VariableGroup>()
            {
                VariableGroup.Basic,
                VariableGroup.Intermediate,
                VariableGroup.Advanced,
            };

        public TriangleManager(Control tabControl, string varFilePath, WatchVariableFlowLayoutPanel watchVariablePanel)
            : base(varFilePath, watchVariablePanel, ALL_VAR_GROUPS, VISIBLE_VAR_GROUPS)
        {
            _triangleCache = new Dictionary<uint, TriangleDataModel>();

            _recordedTriangleAddresses = new List<uint>();

            SplitContainer splitContainerTriangles = tabControl.Controls["splitContainerTriangles"] as SplitContainer;

            _addressBox = splitContainerTriangles.Panel1.Controls["maskedTextBoxOtherTriangle"] as MaskedTextBox;
            _useMisalignmentOffsetCheckbox = splitContainerTriangles.Panel1.Controls["checkBoxVertexMisalignment"] as CheckBox;

            _addressBox.KeyDown += AddressBox_KeyDown;

            _radioButtonTriFloor = splitContainerTriangles.Panel1.Controls["radioButtonTriFloor"] as RadioButton;
            _radioButtonTriFloor.Click += (sender, e) => Mode_Click(sender, e, TriangleMode.Floor);
            _radioButtonTriWall = splitContainerTriangles.Panel1.Controls["radioButtonTriWall"] as RadioButton;
            _radioButtonTriWall.Click += (sender, e) => Mode_Click(sender, e, TriangleMode.Wall);
            _radioButtonTriCeiling = splitContainerTriangles.Panel1.Controls["radioButtonTriCeiling"] as RadioButton;
            _radioButtonTriCeiling.Click += (sender, e) => Mode_Click(sender, e, TriangleMode.Ceiling);
            _radioButtonTriOther = splitContainerTriangles.Panel1.Controls["radioButtonTriOther"] as RadioButton;
            _radioButtonTriOther.Click += (sender, e) => Mode_Click(sender, e, TriangleMode.Other);

            Label labelTriangleSelection = splitContainerTriangles.Panel1.Controls["labelTriangleSelection"] as Label;
            ControlUtilities.AddContextMenuStripFunctions(
                labelTriangleSelection,
                new List<string>() { "Update Based on Coordinates" },
                new List<Action>()
                {
                    () => UpdateBasedOnCoordinates(),
                });

            (splitContainerTriangles.Panel1.Controls["buttonGotoV1"] as Button).Click
                += (sender, e) => ButtonUtilities.GotoTriangleVertex(_triangleAddress, 1, _useMisalignmentOffsetCheckbox.Checked);
            (splitContainerTriangles.Panel1.Controls["buttonGotoV2"] as Button).Click
                += (sender, e) => ButtonUtilities.GotoTriangleVertex(_triangleAddress, 2, _useMisalignmentOffsetCheckbox.Checked);
            (splitContainerTriangles.Panel1.Controls["buttonGotoV3"] as Button).Click
                += (sender, e) => ButtonUtilities.GotoTriangleVertex(_triangleAddress, 3, _useMisalignmentOffsetCheckbox.Checked);
            (splitContainerTriangles.Panel1.Controls["buttonGotoVClosest"] as Button).Click += (sender, e) =>
                ButtonUtilities.GotoTriangleVertexClosest(_triangleAddress, _useMisalignmentOffsetCheckbox.Checked);

            (splitContainerTriangles.Panel1.Controls["buttonRetrieveTriangle"] as Button).Click
                += (sender, e) => ButtonUtilities.RetrieveTriangle(_triangleAddress);

            Button buttonNeutralizeTriangle = splitContainerTriangles.Panel1.Controls["buttonNeutralizeTriangle"] as Button;
            buttonNeutralizeTriangle.Click += (sender, e) => ButtonUtilities.NeutralizeTriangle(_triangleAddress);
            ControlUtilities.AddContextMenuStripFunctions(
                buttonNeutralizeTriangle,
                new List<string>() { "Neutralize", "Neutralize with 0", "Neutralize with 0x15" },
                new List<Action>() {
                    () => ButtonUtilities.NeutralizeTriangle(_triangleAddress),
                    () => ButtonUtilities.NeutralizeTriangle(_triangleAddress, false),
                    () => ButtonUtilities.NeutralizeTriangle(_triangleAddress, true),
                });

            Button buttonAnnihilateTriangle = splitContainerTriangles.Panel1.Controls["buttonAnnihilateTriangle"] as Button;
            buttonAnnihilateTriangle.Click += (sender, e) => ButtonUtilities.AnnihilateTriangle(_triangleAddress);
            ControlUtilities.AddContextMenuStripFunctions(
                buttonAnnihilateTriangle,
                new List<string>() { "Annihilate All Tri But Death Barriers" },
                new List<Action>()
                {
                    () => TriangleUtilities.AnnihilateAllTrianglesButDeathBarriers(),
                });

            var trianglePosGroupBox = splitContainerTriangles.Panel1.Controls["groupBoxTrianglePos"] as GroupBox;
            ControlUtilities.InitializeThreeDimensionController(
                CoordinateSystem.Euler,
                true,
                trianglePosGroupBox,
                trianglePosGroupBox.Controls["buttonTrianglePosXn"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosXp"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosZn"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosZp"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosXnZn"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosXnZp"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosXpZn"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosXpZp"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosYp"] as Button,
                trianglePosGroupBox.Controls["buttonTrianglePosYn"] as Button,
                trianglePosGroupBox.Controls["textBoxTrianglePosXZ"] as TextBox,
                trianglePosGroupBox.Controls["textBoxTrianglePosY"] as TextBox,
                trianglePosGroupBox.Controls["checkBoxTrianglePosRelative"] as CheckBox,
                (float hOffset, float vOffset, float nOffset, bool useRelative) =>
                {
                    ButtonUtilities.MoveTriangle(
                        _triangleAddress,
                        hOffset,
                        nOffset,
                        -1 * vOffset,
                        useRelative);
                });

            var triangleNormalGroupBox = splitContainerTriangles.Panel1.Controls["groupBoxTriangleNormal"] as GroupBox;
            ControlUtilities.InitializeScalarController(
                triangleNormalGroupBox.Controls["buttontriangleNormalN"] as Button,
                triangleNormalGroupBox.Controls["buttontriangleNormalP"] as Button,
                triangleNormalGroupBox.Controls["textBoxTriangleNormal"] as TextBox,
                (float normalValue) =>
                {
                    ButtonUtilities.MoveTriangleNormal(_triangleAddress, normalValue);
                });

            _checkBoxNeutralizeTriangle = splitContainerTriangles.Panel1.Controls["checkBoxNeutralizeTriangle"] as CheckBox;

            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowCoords"] as Button).Click
                += (sender, e) => ShowTriangleCoordinates();
            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowEquation"] as Button).Click
                += (sender, e) => ShowTriangleEquation();

            _recordTriangleDataCheckbox = splitContainerTriangles.Panel1.Controls["checkBoxRecordTriangleData"] as CheckBox;
            _recordTriangleCountLabel = splitContainerTriangles.Panel1.Controls["labelRecordTriangleCount"] as Label;

            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowData"] as Button).Click
                += (sender, e) => ShowTriangleData();
            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowVertices"] as Button).Click
                += (sender, e) => ShowTriangleVertices();
            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowAddresses"] as Button).Click
                += (sender, e) => ShowTriangleAddresses();
            (splitContainerTriangles.Panel1.Controls["buttonTriangleClearData"] as Button).Click
                += (sender, e) => ClearTriangleData();

            _repeatFirstVertexCheckbox = splitContainerTriangles.Panel1.Controls["checkBoxRepeatFirstVertex"] as CheckBox;

            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowLevelTris"] as Button).Click
                += (sender, e) => TriangleUtilities.ShowTriangles(TriangleUtilities.GetLevelTriangles());

            Button buttonTriangleShowObjTris = splitContainerTriangles.Panel1.Controls["buttonTriangleShowObjTris"] as Button;
            buttonTriangleShowObjTris.Click += (sender, e) => TriangleUtilities.ShowTriangles(TriangleUtilities.GetObjectTriangles());
            ControlUtilities.AddContextMenuStripFunctions(
                buttonTriangleShowObjTris,
                new List<string>() { "Show All Object Tris", "Show Selected Object Tris" },
                new List<Action>()
                {
                    () => TriangleUtilities.ShowTriangles(TriangleUtilities.GetObjectTriangles()),
                    () => TriangleUtilities.ShowTriangles(TriangleUtilities.GetSelectedObjectTriangles()),
                });

            (splitContainerTriangles.Panel1.Controls["buttonTriangleShowAllTris"] as Button).Click
                += (sender, e) => TriangleUtilities.ShowTriangles(TriangleUtilities.GetAllTriangles());

            var buttonTriangleNeutralizeAllTriangles = splitContainerTriangles.Panel1.Controls["buttonTriangleNeutralizeAllTriangles"] as Button;
            buttonTriangleNeutralizeAllTriangles.Click += (sender, e) => TriangleUtilities.NeutralizeTriangles();

            ControlUtilities.AddContextMenuStripFunctions(
                buttonTriangleNeutralizeAllTriangles,
                new List<string>() {
                    "Neutralize All Triangles",
                    "Neutralize Wall Triangles",
                    "Neutralize Floor Triangles",
                    "Neutralize Ceiling Triangles",
                    "Neutralize Death Barriers",
                    "Neutralize Lava",
                    "Neutralize Sleeping",
                    "Neutralize Loading Zones"
                },
                new List<Action>() {
                    () => TriangleUtilities.NeutralizeTriangles(),
                    () => TriangleUtilities.NeutralizeTriangles(TriangleClassification.Wall),
                    () => TriangleUtilities.NeutralizeTriangles(TriangleClassification.Floor),
                    () => TriangleUtilities.NeutralizeTriangles(TriangleClassification.Ceiling),
                    () => TriangleUtilities.NeutralizeTriangles(0x0A),
                    () => TriangleUtilities.NeutralizeTriangles(0x01),
                    () => TriangleUtilities.NeutralizeSleeping(),
                    () => {
                        TriangleUtilities.NeutralizeTriangles(0x1B);
                        TriangleUtilities.NeutralizeTriangles(0x1C);
                        TriangleUtilities.NeutralizeTriangles(0x1D);
                        TriangleUtilities.NeutralizeTriangles(0x1E);
                    },
                });

            var buttonTriangleDisableAllCamCollision = splitContainerTriangles.Panel1.Controls["buttonTriangleDisableAllCamCollision"] as Button;
            buttonTriangleDisableAllCamCollision.Click += (sender, e) => TriangleUtilities.DisableCamCollision();

            ControlUtilities.AddContextMenuStripFunctions(
                buttonTriangleDisableAllCamCollision,
                new List<string>()
                {
                    "Disable Cam Collision for All Triangles",
                    "Disable Cam Collision for Wall Triangles",
                    "Disable Cam Collision for Floor Triangles",
                    "Disable Cam Collision for Ceiling Triangles",
                },
                new List<Action>()
                {
                    () => TriangleUtilities.DisableCamCollision(),
                    () => TriangleUtilities.DisableCamCollision(TriangleClassification.Wall),
                    () => TriangleUtilities.DisableCamCollision(TriangleClassification.Floor),
                    () => TriangleUtilities.DisableCamCollision(TriangleClassification.Ceiling),
                });

            GroupBox groupBoxTriangleTypeConversion = splitContainerTriangles.Panel1.Controls["groupBoxTriangleTypeConversion"] as GroupBox;
            ComboBox comboBoxTriangleTypeConversionConvert = groupBoxTriangleTypeConversion.Controls["comboBoxTriangleTypeConversionConvert"] as ComboBox;
            TextBox textBoxTriangleTypeConversionFromType = groupBoxTriangleTypeConversion.Controls["textBoxTriangleTypeConversionFromType"] as TextBox;
            TextBox textBoxTriangleTypeConversionToType = groupBoxTriangleTypeConversion.Controls["textBoxTriangleTypeConversionToType"] as TextBox;
            Button buttonTriangleTypeConversionConvert = groupBoxTriangleTypeConversion.Controls["buttonTriangleTypeConversionConvert"] as Button;

            comboBoxTriangleTypeConversionConvert.DataSource = EnumUtilities.GetEnumValues<TriangleClassificationExtended>(typeof(TriangleClassificationExtended));

            buttonTriangleTypeConversionConvert.Click += (sender, e) =>
            {
                TriangleClassificationExtended classification = (TriangleClassificationExtended)comboBoxTriangleTypeConversionConvert.SelectedItem;
                short? fromType = (short?)ParsingUtilities.ParseHexNullable(textBoxTriangleTypeConversionFromType.Text);
                short? toType = (short?)ParsingUtilities.ParseHexNullable(textBoxTriangleTypeConversionToType.Text);
                if (!fromType.HasValue || !toType.HasValue) return;
                TriangleUtilities.ConvertSurfaceTypes(classification, fromType.Value, toType.Value);
            };
        }

        public void GoToClosestVertex()
        {
            ButtonUtilities.GotoTriangleVertexClosest(_triangleAddress, _useMisalignmentOffsetCheckbox.Checked);
        }

        private void UpdateBasedOnCoordinates()
        {
            if (_triangleAddress == 0) return;
            TriangleDataModel tri = new TriangleDataModel(_triangleAddress);

            // update norms
            (float normX, float normY, float normZ, float normOffset) =
                TriangleUtilities.GetNorms(tri.X1, tri.Y1, tri.Z1, tri.X2, tri.Y2, tri.Z2, tri.X3, tri.Y3, tri.Z3);
            Config.Stream.SetValue(normX, _triangleAddress + TriangleOffsetsConfig.NormX);
            Config.Stream.SetValue(normY, _triangleAddress + TriangleOffsetsConfig.NormY);
            Config.Stream.SetValue(normZ, _triangleAddress + TriangleOffsetsConfig.NormZ);
            Config.Stream.SetValue(normOffset, _triangleAddress + TriangleOffsetsConfig.NormOffset);

            // update y bounds
            short yMinMinus5 = (short)(tri.GetMinY() - 5);
            short yMaxPlus5 = (short)(tri.GetMaxY() + 5);
            Config.Stream.SetValue(yMinMinus5, _triangleAddress + TriangleOffsetsConfig.YMinMinus5);
            Config.Stream.SetValue(yMaxPlus5, _triangleAddress + TriangleOffsetsConfig.YMaxPlus5);
        }

        private short[] GetTriangleCoordinates(uint? nullableTriAddress = null)
        {
            uint triAddress = nullableTriAddress ?? TriangleAddress;
            short[] coordinates = new short[9];
            coordinates[0] = TriangleOffsetsConfig.GetX1(triAddress);
            coordinates[1] = TriangleOffsetsConfig.GetY1(triAddress);
            coordinates[2] = TriangleOffsetsConfig.GetZ1(triAddress);
            coordinates[3] = TriangleOffsetsConfig.GetX2(triAddress);
            coordinates[4] = TriangleOffsetsConfig.GetY2(triAddress);
            coordinates[5] = TriangleOffsetsConfig.GetZ2(triAddress);
            coordinates[6] = TriangleOffsetsConfig.GetX3(triAddress);
            coordinates[7] = TriangleOffsetsConfig.GetY3(triAddress);
            coordinates[8] = TriangleOffsetsConfig.GetZ3(triAddress);
            return coordinates;
        }

        private void ShowTriangleCoordinates()
        {
            if (TriangleAddress == 0) return;
            InfoForm infoForm = new InfoForm();
            infoForm.SetTriangleCoordinates(GetTriangleCoordinates());
            infoForm.Show();
        }

        private void ShowTriangleEquation()
        {
            if (TriangleAddress == 0) return;

            float normX, normY, normZ, normOffset;
            normX = Config.Stream.GetSingle(TriangleAddress + TriangleOffsetsConfig.NormX);
            normY = Config.Stream.GetSingle(TriangleAddress + TriangleOffsetsConfig.NormY);
            normZ = Config.Stream.GetSingle(TriangleAddress + TriangleOffsetsConfig.NormZ);
            normOffset = Config.Stream.GetSingle(TriangleAddress + TriangleOffsetsConfig.NormOffset);

            InfoForm infoForm = new InfoForm();
            infoForm.SetTriangleEquation(normX, normY, normZ, normOffset);
            infoForm.Show();
        }

        private void ShowTriangleData()
        {
            InfoForm infoForm = new InfoForm();
            List<short[]> triangleVertices = _recordedTriangleAddresses.ConvertAll(
                triAddress => GetTriangleCoordinates(triAddress));
            infoForm.SetTriangleData(triangleVertices, _repeatFirstVertexCheckbox.Checked);
            infoForm.Show();
        }

        private void ShowTriangleVertices()
        {
            InfoForm infoForm = new InfoForm();
            List<short[]> triangleVertices = _recordedTriangleAddresses.ConvertAll(
                triAddress => GetTriangleCoordinates(triAddress));
            infoForm.SetTriangleVertices(triangleVertices);
            infoForm.Show();
        }

        private void ShowTriangleAddresses()
        {
            InfoForm infoForm = new InfoForm();
            List<string> addressStrings = _recordedTriangleAddresses.ConvertAll(
                triAddress => HexUtilities.FormatValue(triAddress));
            infoForm.SetText(
                "Triangle Info",
                "Triangle Addresses",
                string.Join("\r\n", addressStrings));
            infoForm.Show();
        }

        private void ClearTriangleData()
        {
            _recordedTriangleAddresses.Clear();
        }

        private void Mode_Click(object sender, EventArgs e, TriangleMode mode)
        {
            if (!(sender as RadioButton).Checked)
                return;

            Mode = mode;
        }

        private void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            // On "Enter" key press
            if (e.KeyData != Keys.Enter)
                return;

            uint newAddress;
            if (!ParsingUtilities.TryParseHex(_addressBox.Text, out newAddress))
            {
                MessageBox.Show(String.Format("Address {0} is not valid!", _addressBox.Text),
                    "Address Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetCustomTriangleAddress(newAddress);
            _addressBox.SelectionLength = 0;
        }

        public TriangleDataModel GetTriangleStruct(uint address)
        {
            if (_triangleCache.ContainsKey(address)) return _triangleCache[address];
            TriangleDataModel triStruct = new TriangleDataModel(address);
            _triangleCache.Add(address, triStruct);
            return triStruct;
        }

        public void SetCustomTriangleAddress(uint triAddress)
        {
            _radioButtonTriOther.Checked = true;
            Mode = TriangleMode.Other;
            TrianglePointerAddress = 0;
            TriangleAddress = triAddress;
        }

        public override void Update(bool updateView)
        {
            _triangleCache.Clear();
            switch (Mode)
            {
                case TriangleMode.Ceiling:
                    TrianglePointerAddress = MarioConfig.StructAddress + MarioConfig.CeilingTriangleOffset;
                    TriangleAddress = Config.Stream.GetUInt32(TrianglePointerAddress);
                    break;

                case TriangleMode.Floor:
                    TrianglePointerAddress = MarioConfig.StructAddress + MarioConfig.FloorTriangleOffset;
                    TriangleAddress = Config.Stream.GetUInt32(TrianglePointerAddress);
                    break;

                case TriangleMode.Wall:
                    TrianglePointerAddress = MarioConfig.StructAddress + MarioConfig.WallTriangleOffset;
                    TriangleAddress = Config.Stream.GetUInt32(TrianglePointerAddress);
                    break;

                default:
                    TrianglePointerAddress = 0;
                    break;
            }

            if (_checkBoxNeutralizeTriangle.Checked && TriangleAddress != 0)
            {
                ButtonUtilities.NeutralizeTriangle(TriangleAddress);
            }

            if (_recordTriangleDataCheckbox.Checked && TriangleAddress != 0)
            {
                bool hasAlready = _recordedTriangleAddresses.Any(triAddress => TriangleAddress == triAddress);
                if (!hasAlready) _recordedTriangleAddresses.Add(TriangleAddress);
            }

            if (!updateView) return;

            _recordTriangleCountLabel.Text = _recordedTriangleAddresses.Count.ToString();

            base.Update(updateView);
        }
    }
}
