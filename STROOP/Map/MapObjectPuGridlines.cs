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
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace STROOP.Map
{
    public class MapObjectPuGridlines : MapObjectGridlines
    {
        public enum PuGridlineSetting { SETTING1, SETTING2, SETTING3 };
        private PuGridlineSetting _setting;
        private List<ToolStripMenuItem> _settingItemList;

        private bool _useMarioAsOrigin;
        private ToolStripMenuItem _itemUseMarioAsOrigin;

        private int puSize = 65536;
        private int halfPuSize = 32768;
        private int halfCourseSize = 8192;

        public MapObjectPuGridlines()
            : base()
        {
            Size = 1;
            LineWidth = 1;
            LineColor = Color.Black;

            _setting = PuGridlineSetting.SETTING1;
            _useMarioAsOrigin = false;
        }

        protected override List<(float x, float y, float z)> GetVerticesTopDownView()
        {
            switch (_setting)
            {
                case PuGridlineSetting.SETTING1:
                    {
                        float marioY = Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset);

                        long size = (long)Math.Max(Size, 1);
                        long gap = puSize * size;
                        List<long> xValues = ExtendedLevelBoundariesUtilities.GetValuesInRange(
                            (long)Config.CurrentMapGraphics.MapViewXMin, (long)Config.CurrentMapGraphics.MapViewXMax,
                            gap, false, ExtendedLevelBoundariesUtilities.ValueOffsetType.GO_THROUGH_ZERO, false, true, true);
                        List<long> zValues = ExtendedLevelBoundariesUtilities.GetValuesInRange(
                            (long)Config.CurrentMapGraphics.MapViewZMin, (long)Config.CurrentMapGraphics.MapViewZMax,
                            gap, false, ExtendedLevelBoundariesUtilities.ValueOffsetType.GO_THROUGH_ZERO, false, true, true);

                        long xMin = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewXMin, false);
                        long xMax = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewXMax, false);
                        long zMin = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewZMin, false);
                        long zMax = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewZMax, false);

                        List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                        foreach (long x in xValues)
                        {
                            vertices.Add((x, marioY, zMin));
                            vertices.Add((x, marioY, zMax));
                        }
                        foreach (long z in zValues)
                        {
                            vertices.Add((xMin, marioY, z));
                            vertices.Add((xMax, marioY, z));
                        }
                        return vertices;
                    }
                case PuGridlineSetting.SETTING2:
                    {
                        float marioY = Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset);

                        List<long> xValues = ExtendedLevelBoundariesUtilities.GetValuesInRange(
                            (long)Config.CurrentMapGraphics.MapViewXMin, (long)Config.CurrentMapGraphics.MapViewXMax,
                            puSize, false, ExtendedLevelBoundariesUtilities.ValueOffsetType.SPACED_AROUND_ZERO, false, true, true);
                        List<long> zValues = ExtendedLevelBoundariesUtilities.GetValuesInRange(
                            (long)Config.CurrentMapGraphics.MapViewZMin, (long)Config.CurrentMapGraphics.MapViewZMax,
                            puSize, false, ExtendedLevelBoundariesUtilities.ValueOffsetType.SPACED_AROUND_ZERO, false, true, true);

                        long xMin = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewXMin, false);
                        long xMax = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewXMax, false);
                        long zMin = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewZMin, false);
                        long zMax = ExtendedLevelBoundariesUtilities.Convert((long)Config.CurrentMapGraphics.MapViewZMax, false);

                        List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                        foreach (long x in xValues)
                        {
                            vertices.Add((x, marioY, zMin));
                            vertices.Add((x, marioY, zMax));
                        }
                        foreach (long z in zValues)
                        {
                            vertices.Add((xMin, marioY, z));
                            vertices.Add((xMax, marioY, z));
                        }
                        return vertices;
                    }
                case PuGridlineSetting.SETTING3:
                    {
                        float marioY = Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset);

                        int xMin = ((((int)Config.CurrentMapGraphics.MapViewXMin) / puSize) - 1) * puSize;
                        int xMax = ((((int)Config.CurrentMapGraphics.MapViewXMax) / puSize) + 1) * puSize;
                        int zMin = ((((int)Config.CurrentMapGraphics.MapViewZMin) / puSize) - 1) * puSize;
                        int zMax = ((((int)Config.CurrentMapGraphics.MapViewZMax) / puSize) + 1) * puSize;

                        List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                        for (int x = xMin; x <= xMax; x += puSize)
                        {
                            for (int z = zMin; z <= zMax; z += puSize)
                            {
                                float x1 = x - halfCourseSize;
                                float x2 = x + halfCourseSize;
                                float z1 = z - halfCourseSize;
                                float z2 = z + halfCourseSize;

                                vertices.Add((x1, marioY, z1));
                                vertices.Add((x1, marioY, z2));

                                vertices.Add((x2, marioY, z1));
                                vertices.Add((x2, marioY, z2));

                                vertices.Add((x1, marioY, z1));
                                vertices.Add((x2, marioY, z1));

                                vertices.Add((x1, marioY, z2));
                                vertices.Add((x2, marioY, z2));
                            }
                        }
                        return vertices;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override List<(float x, float y, float z)> GetGridlineIntersectionPositionsTopDownView()
        {
            if (_setting != PuGridlineSetting.SETTING1)
            {
                return new List<(float x, float y, float z)>();
            }

            float marioY = Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset);

            long size = (long)Math.Max(Size, 1);
            long spacing = (long)(puSize * size);

            long xOffset = 0;
            long zOffset = 0;
            long xOffsetReverse = 0;
            long zOffsetReverse = 0;
            if (_useMarioAsOrigin)
            {
                (int puXIndex, int puYIndex, int puZIndex) = PuUtilities.GetMarioPuIndexes();
                xOffset = (long)MoreMath.NonNegativeModulus(puXIndex, size);
                zOffset = (long)MoreMath.NonNegativeModulus(puZIndex, size);
                xOffsetReverse = size - xOffset;
                zOffsetReverse = size - zOffset;
            }

            long xMin = ((((long)Config.CurrentMapGraphics.MapViewXMin) / spacing) - 1) * spacing - puSize * xOffsetReverse;
            long xMax = ((((long)Config.CurrentMapGraphics.MapViewXMax) / spacing) + 1) * spacing + puSize * xOffset;
            long zMin = ((((long)Config.CurrentMapGraphics.MapViewZMin) / spacing) - 1) * spacing - puSize * zOffsetReverse;
            long zMax = ((((long)Config.CurrentMapGraphics.MapViewZMax) / spacing) + 1) * spacing + puSize * zOffset;

            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
            for (long x = xMin; x <= xMax; x += spacing)
            {
                for (long z = zMin; z <= zMax; z += spacing)
                {
                    vertices.Add((x, marioY, z));
                }
            }
            return vertices;
        }

        protected override List<(float x, float y, float z)> GetVerticesOrthographicView()
        {
            switch (_setting)
            {
                case PuGridlineSetting.SETTING1:
                    {
                        long size = (long)Math.Max(Size, 1);
                        long spacing = (long)(puSize * size);

                        long xOffset = 0;
                        long yOffset = 0;
                        long zOffset = 0;
                        long xOffsetReverse = 0;
                        long yOffsetReverse = 0;
                        long zOffsetReverse = 0;
                        if (_useMarioAsOrigin)
                        {
                            (int puXIndex, int puYIndex, int puZIndex) = PuUtilities.GetMarioPuIndexes();
                            xOffset = (long)MoreMath.NonNegativeModulus(puXIndex, size);
                            yOffset = (long)MoreMath.NonNegativeModulus(puYIndex, size);
                            zOffset = (long)MoreMath.NonNegativeModulus(puZIndex, size);
                            xOffsetReverse = size - xOffset;
                            yOffsetReverse = size - yOffset;
                            zOffsetReverse = size - zOffset;
                        }

                        float xCenter = Config.CurrentMapGraphics.MapViewCenterXValue;
                        float zCenter = Config.CurrentMapGraphics.MapViewCenterZValue;
                        long xMin = ((((long)Config.CurrentMapGraphics.MapViewXMin) / spacing) - 1) * spacing - puSize * xOffsetReverse;
                        long xMax = ((((long)Config.CurrentMapGraphics.MapViewXMax) / spacing) + 1) * spacing + puSize * xOffset;
                        long yMin = ((((long)Config.CurrentMapGraphics.MapViewYMin) / spacing) - 1) * spacing - puSize * yOffsetReverse;
                        long yMax = ((((long)Config.CurrentMapGraphics.MapViewYMax) / spacing) + 1) * spacing + puSize * yOffset;
                        long zMin = ((((long)Config.CurrentMapGraphics.MapViewZMin) / spacing) - 1) * spacing - puSize * zOffsetReverse;
                        long zMax = ((((long)Config.CurrentMapGraphics.MapViewZMax) / spacing) + 1) * spacing + puSize * zOffset;

                        if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 0 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 32768))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (long x = xMin; x <= xMax; x += spacing)
                            {
                                vertices.Add((x, yMin, zCenter));
                                vertices.Add((x, yMax, zCenter));
                            }
                            for (long y = yMin; y <= yMax; y += spacing)
                            {
                                vertices.Add((xMin, y, zCenter));
                                vertices.Add((xMax, y, zCenter));
                            }
                            return vertices;
                        }
                        else if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 16384 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 49152))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (long z = zMin; z <= zMax; z += spacing)
                            {
                                vertices.Add((xCenter, yMin, z));
                                vertices.Add((xCenter, yMax, z));
                            }
                            for (long y = yMin; y <= yMax; y += spacing)
                            {
                                vertices.Add((zCenter, y, zMin));
                                vertices.Add((xCenter, y, zMax));
                            }
                            return vertices;
                        }
                        else
                        {
                            return new List<(float x, float y, float z)>();
                        }
                    }
                case PuGridlineSetting.SETTING2:
                    {
                        float xCenter = Config.CurrentMapGraphics.MapViewCenterXValue;
                        float zCenter = Config.CurrentMapGraphics.MapViewCenterZValue;
                        int xMin = ((((int)Config.CurrentMapGraphics.MapViewXMin) / puSize) - 1) * puSize - halfPuSize;
                        int xMax = ((((int)Config.CurrentMapGraphics.MapViewXMax) / puSize) + 1) * puSize + halfPuSize;
                        int yMin = ((((int)Config.CurrentMapGraphics.MapViewYMin) / puSize) - 1) * puSize - halfPuSize;
                        int yMax = ((((int)Config.CurrentMapGraphics.MapViewYMax) / puSize) + 1) * puSize + halfPuSize;
                        int zMin = ((((int)Config.CurrentMapGraphics.MapViewZMin) / puSize) - 1) * puSize - halfPuSize;
                        int zMax = ((((int)Config.CurrentMapGraphics.MapViewZMax) / puSize) + 1) * puSize + halfPuSize;

                        if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 0 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 32768))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (int x = xMin; x <= xMax; x += puSize)
                            {
                                vertices.Add((x, yMin, zCenter));
                                vertices.Add((x, yMax, zCenter));
                            }
                            for (int y = yMin; y <= yMax; y += puSize)
                            {
                                vertices.Add((xMin, y, zCenter));
                                vertices.Add((xMax, y, zCenter));
                            }
                            return vertices;
                        }
                        else if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 16384 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 49152))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (int z = zMin; z <= zMax; z += puSize)
                            {
                                vertices.Add((xCenter, yMin, z));
                                vertices.Add((xCenter, yMax, z));
                            }
                            for (int y = yMin; y <= yMax; y += puSize)
                            {
                                vertices.Add((zCenter, y, zMin));
                                vertices.Add((xCenter, y, zMax));
                            }
                            return vertices;
                        }
                        else
                        {
                            return new List<(float x, float y, float z)>();
                        }
                    }
                case PuGridlineSetting.SETTING3:
                    {
                        float xCenter = Config.CurrentMapGraphics.MapViewCenterXValue;
                        float zCenter = Config.CurrentMapGraphics.MapViewCenterZValue;
                        int xMin = ((((int)Config.CurrentMapGraphics.MapViewXMin) / puSize) - 1) * puSize;
                        int xMax = ((((int)Config.CurrentMapGraphics.MapViewXMax) / puSize) + 1) * puSize;
                        int yMin = ((((int)Config.CurrentMapGraphics.MapViewYMin) / puSize) - 1) * puSize;
                        int yMax = ((((int)Config.CurrentMapGraphics.MapViewYMax) / puSize) + 1) * puSize;
                        int zMin = ((((int)Config.CurrentMapGraphics.MapViewZMin) / puSize) - 1) * puSize;
                        int zMax = ((((int)Config.CurrentMapGraphics.MapViewZMax) / puSize) + 1) * puSize;

                        if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 0 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 32768))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (int x = xMin; x <= xMax; x += puSize)
                            {
                                for (int y = yMin; y <= yMax; y += puSize)
                                {
                                    float x1 = x - halfCourseSize;
                                    float x2 = x + halfCourseSize;
                                    float y1 = y - halfCourseSize;
                                    float y2 = y + halfCourseSize;

                                    vertices.Add((x1, y1, zCenter));
                                    vertices.Add((x1, y2, zCenter));

                                    vertices.Add((x2, y1, zCenter));
                                    vertices.Add((x2, y2, zCenter));

                                    vertices.Add((x1, y1, zCenter));
                                    vertices.Add((x2, y1, zCenter));

                                    vertices.Add((x1, y2, zCenter));
                                    vertices.Add((x2, y2, zCenter));
                                }
                            }
                            return vertices;
                        }
                        else if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                            (Config.CurrentMapGraphics.MapViewYawValue == 16384 ||
                            Config.CurrentMapGraphics.MapViewYawValue == 49152))
                        {
                            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                            for (int z = zMin; z <= zMax; z += puSize)
                            {
                                for (int y = yMin; y <= yMax; y += puSize)
                                {
                                    float z1 = z - halfCourseSize;
                                    float z2 = z + halfCourseSize;
                                    float y1 = y - halfCourseSize;
                                    float y2 = y + halfCourseSize;

                                    vertices.Add((xCenter, y1, z1));
                                    vertices.Add((xCenter, y2, z1));

                                    vertices.Add((xCenter, y1, z2));
                                    vertices.Add((xCenter, y2, z2));

                                    vertices.Add((xCenter, y1, z1));
                                    vertices.Add((xCenter, y1, z2));

                                    vertices.Add((xCenter, y2, z1));
                                    vertices.Add((xCenter, y2, z2));
                                }
                            }
                            return vertices;
                        }
                        else
                        {
                            return new List<(float x, float y, float z)>();
                        }
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override List<(float x, float y, float z)> GetGridlineIntersectionPositionsOrthographicView()
        {
            if (_setting != PuGridlineSetting.SETTING1)
            {
                return new List<(float x, float y, float z)>();
            }

            long size = (long)Math.Max(Size, 1);
            long spacing = (long)(puSize * size);

            long xOffset = 0;
            long yOffset = 0;
            long zOffset = 0;
            long xOffsetReverse = 0;
            long yOffsetReverse = 0;
            long zOffsetReverse = 0;
            if (_useMarioAsOrigin)
            {
                (int puXIndex, int puYIndex, int puZIndex) = PuUtilities.GetMarioPuIndexes();
                xOffset = (long)MoreMath.NonNegativeModulus(puXIndex, size);
                yOffset = (long)MoreMath.NonNegativeModulus(puYIndex, size);
                zOffset = (long)MoreMath.NonNegativeModulus(puZIndex, size);
                xOffsetReverse = size - xOffset;
                yOffsetReverse = size - yOffset;
                zOffsetReverse = size - zOffset;
            }

            float xCenter = Config.CurrentMapGraphics.MapViewCenterXValue;
            float zCenter = Config.CurrentMapGraphics.MapViewCenterZValue;
            long xMin = ((((long)Config.CurrentMapGraphics.MapViewXMin) / spacing) - 1) * spacing - puSize * xOffsetReverse;
            long xMax = ((((long)Config.CurrentMapGraphics.MapViewXMax) / spacing) + 1) * spacing + puSize * xOffset;
            long yMin = ((((long)Config.CurrentMapGraphics.MapViewYMin) / spacing) - 1) * spacing - puSize * yOffsetReverse;
            long yMax = ((((long)Config.CurrentMapGraphics.MapViewYMax) / spacing) + 1) * spacing + puSize * yOffset;
            long zMin = ((((long)Config.CurrentMapGraphics.MapViewZMin) / spacing) - 1) * spacing - puSize * zOffsetReverse;
            long zMax = ((((long)Config.CurrentMapGraphics.MapViewZMax) / spacing) + 1) * spacing + puSize * zOffset;

            if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                (Config.CurrentMapGraphics.MapViewYawValue == 0 ||
                Config.CurrentMapGraphics.MapViewYawValue == 32768))
            {
                List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                for (long x = xMin; x <= xMax; x += spacing)
                {
                    for (long y = yMin; y <= yMax; y += spacing)
                    {
                        vertices.Add((x, y, zCenter));
                    }
                }
                return vertices;
            }
            else if (Config.CurrentMapGraphics.MapViewPitchValue == 0 &&
                (Config.CurrentMapGraphics.MapViewYawValue == 16384 ||
                Config.CurrentMapGraphics.MapViewYawValue == 49152))
            {
                List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();
                for (long z = zMin; z <= zMax; z += spacing)
                {
                    for (long y = yMin; y <= yMax; y += spacing)
                    {
                        vertices.Add((xCenter, y, z));
                    }
                }
                return vertices;
            }
            else
            {
                return new List<(float x, float y, float z)>();
            }
        }

        public override string GetName()
        {
            return "PU Gridlines";
        }

        public override Image GetInternalImage()
        {
            return Config.ObjectAssociations.UnitGridlinesImage;
        }

        public override ContextMenuStrip GetContextMenuStrip()
        {
            if (_contextMenuStrip == null)
            {
                List<string> itemNames = new List<string>() { "Setting 1", "Setting 2", "Setting 3" };
                List<PuGridlineSetting> itemValues = EnumUtilities.GetEnumValues<PuGridlineSetting>(typeof(PuGridlineSetting));
                Action<PuGridlineSetting> setterAction = (PuGridlineSetting setting) =>
                {
                    MapObjectSettings settings = new MapObjectSettings(
                        changePuGridlinesSetting: true, newPuGridlinesSetting: setting.ToString());
                    GetParentMapTracker().ApplySettings(settings);
                };
                PuGridlineSetting startingValue = _setting;
                (List<ToolStripMenuItem> itemList, Action<PuGridlineSetting> valueAction) =
                    ControlUtilities.CreateCheckableItems(
                        itemNames, itemValues, setterAction, startingValue);
                _settingItemList = itemList;

                _itemUseMarioAsOrigin = new ToolStripMenuItem("Use Mario as Origin");
                _itemUseMarioAsOrigin.Click += (sender, e) =>
                {
                    MapObjectSettings settings = new MapObjectSettings(
                        changeUseMarioAsOrigin: true, newUseMarioAsOrigin: !_useMarioAsOrigin);
                    GetParentMapTracker().ApplySettings(settings);
                };

                _contextMenuStrip = new ContextMenuStrip();
                itemList.ForEach(item => _contextMenuStrip.Items.Add(item));
                _contextMenuStrip.Items.Add(_itemUseMarioAsOrigin);
                _contextMenuStrip.Items.Add(new ToolStripSeparator());
                GetGridlinesToolStripMenuItems().ForEach(item => _contextMenuStrip.Items.Add(item));
            }

            return _contextMenuStrip;
        }

        public override void ApplySettings(MapObjectSettings settings)
        {
            base.ApplySettings(settings);

            if (settings.ChangePuGridlinesSetting)
            {
                _setting = (PuGridlineSetting)Enum.Parse(typeof(PuGridlineSetting), settings.NewPuGridlinesSetting);
                List<PuGridlineSetting> enumValues = EnumUtilities.GetEnumValues<PuGridlineSetting>(typeof(PuGridlineSetting));
                for (int i = 0; i < 3; i++)
                {
                    _settingItemList[i].Checked = _setting == enumValues[i];
                }
            }

            if (settings.ChangeUseMarioAsOrigin)
            {
                _useMarioAsOrigin = settings.NewUseMarioAsOrigin;
                _itemUseMarioAsOrigin.Checked = _useMarioAsOrigin;
            }
        }
    }
}
