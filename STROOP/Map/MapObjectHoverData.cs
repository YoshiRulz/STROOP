﻿using OpenTK;
using OpenTK.Graphics;
using STROOP.Models;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STROOP.Map
{
    public class MapObjectHoverData
    {
        public static long HoverStartTime = 0;
        public static bool ContextMenuStripIsOpen = false;
        public static Point ContextMenuStripPoint = new Point();

        public readonly MapObject MapObject;
        public readonly ObjectDataModel Obj;
        public readonly TriangleDataModel Tri;
        public readonly float? MidUnitX;
        public readonly float? MidUnitZ;

        public MapObjectHoverData(
            MapObject mapObject,
            ObjectDataModel obj = null,
            TriangleDataModel tri = null,
            float? midUnitX = null,
            float? midUnitZ = null)
        {
            MapObject = mapObject;
            Obj = obj;
            Tri = tri;
            MidUnitX = midUnitX;
            MidUnitZ = midUnitZ;
        }

        public static Point GetCurrentPoint()
        {
            return ContextMenuStripIsOpen ? ContextMenuStripPoint : Cursor.Position;
        }

        public List<ToolStripItem> GetContextMenuStripItems()
        {
            return MapObject.GetHoverContextMenuStripItems(this);
        }

        public override string ToString()
        {
            List<object> parts = new List<object>();
            parts.Add(MapObject);
            if (Obj != null) parts.Add(Obj);
            if (Tri != null) parts.Add(HexUtilities.FormatValue(Tri.Address));
            if (MidUnitX.HasValue) parts.Add(MidUnitX.Value);
            if (MidUnitZ.HasValue) parts.Add(MidUnitZ.Value);
            return string.Join(" ", parts);
        }

        public override bool Equals(object obj)
        {
            if (obj is MapObjectHoverData other)
            {
                return MapObject == other.MapObject &&
                    Obj?.Address == other.Obj?.Address &&
                    Tri?.Address == other.Tri?.Address &&
                    MidUnitX == other.MidUnitX &&
                    MidUnitZ == other.MidUnitZ;
            }
            return false;
        }
    }
}
