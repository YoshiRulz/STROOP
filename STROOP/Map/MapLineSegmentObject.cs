﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using STROOP.Utilities;
using STROOP.Structs.Configurations;
using STROOP.Structs;
using OpenTK;
using System.Windows.Forms;

namespace STROOP.Map
{
    public class MapLineSegmentObject : MapLineObject
    {
        private PositionAngle _posAngle1;
        private PositionAngle _posAngle2;
        private bool _useFixedSize;
        private float _backwardsSize;

        private ToolStripMenuItem _itemUseFixedSize;
        private ToolStripMenuItem _itemSetBackwardsSize;

        private static readonly string SET_BACKWARDS_SIZE_TEXT = "Set Backwards Size";

        public MapLineSegmentObject(PositionAngle posAngle1, PositionAngle posAngle2)
            : base()
        {
            _posAngle1 = posAngle1;
            _posAngle2 = posAngle2;
            _useFixedSize = false;
            _backwardsSize = 0;

            Size = 0;
            OutlineWidth = 3;
            OutlineColor = Color.Red;
        }

        public static MapObject Create(string text1, string text2)
        {
            PositionAngle posAngle1 = PositionAngle.FromString(text1);
            PositionAngle posAngle2 = PositionAngle.FromString(text2);
            if (posAngle1 == null || posAngle2 == null) return null;
            return new MapLineSegmentObject(posAngle1, posAngle2);
        }

        protected override List<(float x, float y, float z)> GetVerticesTopDownView()
        {
            (double x1, double y1, double z1, double angle1) = _posAngle1.GetValues();
            (double x2, double y2, double z2, double angle2) = _posAngle2.GetValues();
            double dist = PositionAngle.GetHDistance(_posAngle1, _posAngle2);
            (double startX, double startZ) = MoreMath.ExtrapolateLine2D(x2, z2, x1, z1, dist + _backwardsSize);
            (double endX, double endZ) = MoreMath.ExtrapolateLine2D(x1, z1, x2, z2, (_useFixedSize ? 0 : dist) + Size);

            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
            vertices.Add(((float)startX, 0, (float)startZ));
            vertices.Add(((float)endX, 0, (float)endZ));
            return vertices;
        }

        public override ContextMenuStrip GetContextMenuStrip()
        {
            if (_contextMenuStrip == null)
            {
                _itemUseFixedSize = new ToolStripMenuItem("Use Fixed Size");
                _itemUseFixedSize.Click += (sender, e) =>
                {
                    MapObjectSettings settings = new MapObjectSettings(
                        changeLineSegmentUseFixedSize: true, newLineSegmentUseFixedSize: !_useFixedSize);
                    GetParentMapTracker().ApplySettings(settings);
                };

                string suffix = string.Format(" ({0})", _backwardsSize);
                _itemSetBackwardsSize = new ToolStripMenuItem(SET_BACKWARDS_SIZE_TEXT + suffix);
                _itemSetBackwardsSize.Click += (sender, e) =>
                {
                    string text = DialogUtilities.GetStringFromDialog(labelText: "Enter backwards size.");
                    float? backwardsSizeNullable = ParsingUtilities.ParseFloatNullable(text);
                    if (!backwardsSizeNullable.HasValue) return;
                    float backwardsSize = backwardsSizeNullable.Value;
                    MapObjectSettings settings = new MapObjectSettings(
                        changeLineSegmentBackwardsSize: true, newLineSegmentBackwardsSize: backwardsSize);
                    GetParentMapTracker().ApplySettings(settings);
                };

                _contextMenuStrip = new ContextMenuStrip();
                _contextMenuStrip.Items.Add(_itemUseFixedSize);
                _contextMenuStrip.Items.Add(_itemSetBackwardsSize);
            }

            return _contextMenuStrip;
        }

        public override void ApplySettings(MapObjectSettings settings)
        {
            base.ApplySettings(settings);

            if (settings.ChangeLineSegmentUseFixedSize)
            {
                _useFixedSize = settings.NewLineSegmentUseFixedSize;
                _itemUseFixedSize.Checked = settings.NewLineSegmentUseFixedSize;
            }

            if (settings.ChangeLineSegmentBackwardsSize)
            {
                _backwardsSize = settings.NewLineSegmentBackwardsSize;
                string suffix = string.Format(" ({0})", settings.NewLineSegmentBackwardsSize);
                _itemSetBackwardsSize.Text = SET_BACKWARDS_SIZE_TEXT + suffix;
            }
        }

        public override string GetName()
        {
            return "Line Segment";
        }

        public override Image GetInternalImage()
        {
            return Config.ObjectAssociations.ArrowImage;
        }
    }
}
