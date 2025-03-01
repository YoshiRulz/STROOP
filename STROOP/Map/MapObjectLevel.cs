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

namespace STROOP.Map
{
    public abstract class MapObjectLevel : MapObjectIconRectangle
    {
        public MapObjectLevel()
            : base()
        {
            Size = 1;
            InternalRotates = true;
        }

        public abstract MapLayout GetMapLayout();

        public override Image GetInternalImage()
        {
            return GetMapLayout().MapImage;
        }

        protected override List<(PointF loc, SizeF size)> GetDimensions()
        {
            MapLayout mapLayout = GetMapLayout();
            RectangleF rectangle = mapLayout.Coordinates;
            float rectangleCenterX = rectangle.X + rectangle.Width / 2;
            float rectangleCenterZ = rectangle.Y + rectangle.Height / 2;
            int size = Math.Max(1, (int)Size);
            List<(float x, float z)> rectangleCenters =
                Config.MapGui.checkBoxMapOptionsEnablePuView.Checked && mapLayout.Id != "000" ?
                MapUtilities.GetPuCoordinates(rectangleCenterX, rectangleCenterZ, size) :
                new List<(float x, float z)>() { (rectangleCenterX, rectangleCenterZ) };
            List<(float x, float z)> controlCenters = rectangleCenters.ConvertAll(
                rectangleCenter => MapUtilities.ConvertCoordsForControlTopDownView(rectangleCenter.x, rectangleCenter.z, UseRelativeCoordinates));
            float sizeX = rectangle.Width * Config.CurrentMapGraphics.MapViewScaleValue;
            float sizeZ = rectangle.Height * Config.CurrentMapGraphics.MapViewScaleValue;
            List<(PointF loc, SizeF size)> dimensions = controlCenters.ConvertAll(
                controlCenter => (new PointF(controlCenter.x, controlCenter.z), new SizeF(sizeX, sizeZ)));
            return dimensions;
        }

        public override void DrawOn3DControl()
        {
            // do nothing
        }
    }
}
