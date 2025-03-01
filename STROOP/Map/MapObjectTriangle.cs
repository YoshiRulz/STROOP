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
using STROOP.Models;
using System.Windows.Forms;

namespace STROOP.Map
{
    public abstract class MapObjectTriangle : MapObject
    {
        protected bool _showArrows;
        protected bool _useCrossSection;
        protected bool _colorByHeight;
        protected float _iconSize;
        private float? _withinDist;
        private float? _withinCenter;
        protected bool _excludeDeathBarriers;
        protected float _minColorY;
        protected float _maxColorY;

        private ToolStripMenuItem _itemShowArrows;
        private ToolStripMenuItem _itemUseCrossSection;
        private ToolStripMenuItem _itemColorByHeight;
        private ToolStripMenuItem _itemSetIconSize;
        private ToolStripMenuItem _itemSetWithinDist;
        private ToolStripMenuItem _itemSetWithinCenter;

        private static readonly string SET_ICON_SIZE_TEXT = "Set Icon Size";
        private static readonly string SET_WITHIN_DIST_TEXT = "Set Within Dist";
        private static readonly string SET_WITHIN_CENTER_TEXT = "Set Within Center";

        public MapObjectTriangle()
            : base()
        {
            _showArrows = false;
            _useCrossSection = false;
            _colorByHeight = false;
            _iconSize = 10;
            _withinDist = null;
            _withinCenter = null;
            _excludeDeathBarriers = false;
        }

        protected List<List<(float x, float y, float z)>> GetVertexLists()
        {
            return GetFilteredTriangles().ConvertAll(tri => tri.Get3DVertices());
        }

        protected List<TriangleDataModel> GetFilteredTriangles()
        {
            float centerY = _withinCenter ?? Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset);
            List<TriangleDataModel> tris = GetUnfilteredTriangles()
                .FindAll(tri => tri.IsTriWithinVerticalDistOfCenter(_withinDist, centerY));
            if (_excludeDeathBarriers)
            {
                tris = tris.FindAll(tri => tri.SurfaceType != 0x0A);
            }
            if (Config.CurrentMapGraphics.IsOrthographicViewEnabled &&
                MapConfig.MapSortOrthographicTris != 0)
            {
                if (_useCrossSection)
                {
                    tris.Sort((TriangleDataModel t1, TriangleDataModel t2) =>
                    {
                        string string1 = t1.Classification.ToString();
                        string string2 = t2.Classification.ToString();
                        return string1.CompareTo(string2);
                    });
                }
                else
                {
                    tris.Sort((TriangleDataModel t1, TriangleDataModel t2) =>
                    {
                        double dist1 = MapUtilities.GetSignedDistToCameraPlane(t1);
                        double dist2 = MapUtilities.GetSignedDistToCameraPlane(t2);
                        return dist2.CompareTo(dist1);
                    });
                }
            }
            return tris;
        }

        protected abstract List<TriangleDataModel> GetUnfilteredTriangles();

        protected static (float x, float y, float z) OffsetVertex(
            (float x, float y, float z) vertex, float xOffset, float yOffset, float zOffset)
        {
            return (vertex.x + xOffset, vertex.y + yOffset, vertex.z + zOffset);
        }

        public override void DrawOn2DControlOrthographicView(MapObjectHoverData hoverData)
        {
            if (_useCrossSection)
            {
                DrawOn2DControlOrthographicViewCrossSection(hoverData);
            }
            else
            {
                DrawOn2DControlOrthographicViewTotal(hoverData);
            }
        }

        public virtual float GetWallRelativeHeightForOrthographicViewCrossSection()
        {
            return 0;
        }

        public virtual float GetWallRelativeHeightForOrthographicViewTotal()
        {
            return 0;
        }

        public virtual Color GetColorForOrthographicView(TriangleClassification classification)
        {
            return Color;
        }

        public virtual float GetSizeForOrthographicView(TriangleClassification classification)
        {
            return Size;
        }

        public virtual bool GetShowTriUnits()
        {
            return false;
        }

        public virtual bool GetTruncateBottomOfHitbox()
        {
            return false;
        }

        private float MaybeTruncateHitboxBottom(float y)
        {
            if (GetTruncateBottomOfHitbox())
            {
                return (float)(y >= 0 ? Math.Ceiling(y) : Math.Floor(y));
            }
            else
            {
                return y;
            }
        }

        private List<List<(float x, float y, float z, Color color, TriangleMapData data)>> GetOrthographicCrossSectionVertexLists()
        {
            List<TriangleMapData> triData = GetFilteredTriangles()
                .ConvertAll(tri => MapUtilities.Get2DDataFromTri(tri))
                .FindAll(data => data != null);

            List<List<(float x, float y, float z, Color color, TriangleMapData data)>> vertexLists = triData.ConvertAll(data =>
            {
                Color color = GetColorForOrthographicView(data.Tri.Classification);
                float size = GetSizeForOrthographicView(data.Tri.Classification);
                switch (data.Tri.Classification)
                {
                    case TriangleClassification.Wall:
                        {
                            double pushAngleRadians = MoreMath.AngleUnitsToRadians(data.Tri.GetPushAngle());
                            double mapViewAngleRadians = MoreMath.AngleUnitsToRadians(Config.CurrentMapGraphics.MapViewYawValue);
                            float relativeHeight = GetWallRelativeHeightForOrthographicViewCrossSection();
                            if (data.Tri.XProjection)
                            {
                                float projectionDist = size / (float)Math.Abs(Math.Cos(mapViewAngleRadians - pushAngleRadians + 0.5 * Math.PI));
                                return new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>()
                                {
                                    new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                    {
                                        (data.X1, data.Y1 + relativeHeight, data.Z1, color, data),
                                        (data.X2, data.Y2 + relativeHeight, data.Z2, color, data),
                                        (data.X2 - (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y2 + relativeHeight, data.Z2 + (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                        (data.X1 - (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y1 + relativeHeight, data.Z1 + (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                    },
                                    new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                    {
                                        (data.X1, data.Y1 + relativeHeight, data.Z1, color, data),
                                        (data.X2, data.Y2 + relativeHeight, data.Z2, color, data),
                                        (data.X2 + (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y2 + relativeHeight, data.Z2 - (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                        (data.X1 + (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y1 + relativeHeight, data.Z1 - (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                    },
                                };
                            }
                            else
                            {
                                float projectionDist = size / (float)Math.Abs(Math.Sin(mapViewAngleRadians - pushAngleRadians));
                                return new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>()
                                {
                                    new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                    {
                                        (data.X1, data.Y1 + relativeHeight, data.Z1, color, data),
                                        (data.X2, data.Y2 + relativeHeight, data.Z2, color, data),
                                        (data.X2 - (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y2 + relativeHeight, data.Z2 + (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                        (data.X1 - (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y1 + relativeHeight, data.Z1 + (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                    },
                                    new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                    {
                                        (data.X1, data.Y1 + relativeHeight, data.Z1, color, data),
                                        (data.X2, data.Y2 + relativeHeight, data.Z2, color, data),
                                        (data.X2 + (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y2 + relativeHeight, data.Z2 - (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                        (data.X1 + (float)Math.Cos(mapViewAngleRadians) * projectionDist, data.Y1 + relativeHeight, data.Z1 - (float)Math.Sin(mapViewAngleRadians) * projectionDist, color, data),
                                    },
                                };
                            }
                        }
                    case TriangleClassification.Floor:
                    case TriangleClassification.Ceiling:
                        {
                            if (MapUtilities.IsAbleToShowUnitPrecision() && GetShowTriUnits())
                            {
                                if (Config.CurrentMapGraphics.MapViewYawValue == 0 ||
                                    Config.CurrentMapGraphics.MapViewYawValue == 32768)
                                {
                                    int xMin = (int)Math.Max(data.Tri.GetMinX(), Config.CurrentMapGraphics.MapViewXMin);
                                    int xMax = (int)Math.Min(data.Tri.GetMaxX(), Config.CurrentMapGraphics.MapViewXMax);
                                    float z = Config.CurrentMapGraphics.MapViewCenterZValue;
                                    List<List<(float x, float y, float z, Color color, TriangleMapData data)>> output =
                                        new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>();
                                    List<(int xInner, int xOuter)> xPairs = new List<(int xInner, int xOuter)>();
                                    for (int x = xMin; x <= xMax; x++)
                                    {
                                        if (x <= 0) xPairs.Add((x, x - 1));
                                        if (x >= 0) xPairs.Add((x, x + 1));
                                    }
                                    foreach ((int xInner, int xOuter) in xPairs)
                                    {
                                        float? y = data.Tri.GetTruncatedHeightOnTriangleIfInsideTriangle(xInner, z);
                                        if (y.HasValue)
                                        {
                                            output.Add(new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                            {
                                                (xInner, y.Value, z, color, data),
                                                (xOuter, y.Value, z, color, data),
                                                (xOuter, MaybeTruncateHitboxBottom(y.Value - size), z, color, data),
                                                (xInner, MaybeTruncateHitboxBottom(y.Value - size), z, color, data),
                                            });
                                        }
                                    }
                                    return output;
                                }
                                else if (Config.CurrentMapGraphics.MapViewYawValue == 16384 ||
                                    Config.CurrentMapGraphics.MapViewYawValue == 49152)
                                {
                                    int zMin = (int)Math.Max(data.Tri.GetMinZ(), Config.CurrentMapGraphics.MapViewZMin);
                                    int zMax = (int)Math.Min(data.Tri.GetMaxZ(), Config.CurrentMapGraphics.MapViewZMax);
                                    float x = Config.CurrentMapGraphics.MapViewCenterXValue;
                                    List<List<(float x, float y, float z, Color color, TriangleMapData data)>> output =
                                        new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>();
                                    List<(int zInner, int zOuter)> zPairs = new List<(int zInner, int zOuter)>();
                                    for (int z = zMin; z <= zMax; z++)
                                    {
                                        if (z <= 0) zPairs.Add((z, z - 1));
                                        if (z >= 0) zPairs.Add((z, z + 1));
                                    }
                                    foreach ((int zInner, int zOuter) in zPairs)
                                    {
                                        float? y = data.Tri.GetTruncatedHeightOnTriangleIfInsideTriangle(x, zInner);
                                        if (y.HasValue)
                                        {
                                            output.Add(new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                            {
                                                (x, y.Value, zInner, color, data),
                                                (x, y.Value, zOuter, color, data),
                                                (x, MaybeTruncateHitboxBottom(y.Value - size), zOuter, color, data),
                                                (x, MaybeTruncateHitboxBottom(y.Value - size), zInner, color, data),
                                            });
                                        }
                                    }
                                    return output;
                                }
                                else
                                {
                                    List<List<(float x, float y, float z, Color color, TriangleMapData data)>> output =
                                        new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>();
                                    List<(double x, double z)> points = MapUtilities.GetUnitPointsCrossSection(5);
                                    for (int i = 0; i < points.Count - 1; i++)
                                    {
                                        (float x1, float z1) = ((float x, float z))points[i];
                                        (float x2, float z2) = ((float x, float z))points[i + 1];
                                        int x = (int)(Math.Abs(x1) < Math.Abs(x2) ? x1 : x2);
                                        int z = (int)(Math.Abs(z1) < Math.Abs(z2) ? z1 : z2);
                                        float? y = data.Tri.GetTruncatedHeightOnTriangleIfInsideTriangle(x, z);
                                        if (y.HasValue)
                                        {
                                            output.Add(new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                            {
                                                (x1, y.Value, z1, color, data),
                                                (x2, y.Value, z2, color, data),
                                                (x2, MaybeTruncateHitboxBottom(y.Value - size), z2, color, data),
                                                (x1, MaybeTruncateHitboxBottom(y.Value - size), z1, color, data),
                                            });
                                        }
                                    }
                                    return output;
                                }
                            }
                            return new List<List<(float x, float y, float z, Color color, TriangleMapData data)>>()
                            {
                                new List<(float x, float y, float z, Color color, TriangleMapData data)>()
                                {
                                    (data.X1, data.Y1, data.Z1, color, data),
                                    (data.X2, data.Y2, data.Z2, color, data),
                                    (data.X2, data.Y2 - size, data.Z2, color, data),
                                    (data.X1, data.Y1 - size, data.Z1, color, data),
                                },
                            };
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }).SelectMany(list => list).ToList();

            return vertexLists;
        }

        public void DrawOn2DControlOrthographicViewCrossSection(MapObjectHoverData hoverData)
        {
            List<List<(float x, float y, float z, Color color, TriangleMapData data)>> vertexLists =
                GetOrthographicCrossSectionVertexLists();

            List<List<(float x, float z, Color color, TriangleMapData data)>> vertexListsForControl =
                vertexLists.ConvertAll(vertexList => vertexList.ConvertAll(
                    vertex =>
                    {
                        (float x, float z) = MapUtilities.ConvertCoordsForControlOrthographicView(vertex.x, vertex.y, vertex.z, UseRelativeCoordinates);
                        return (x, z, vertex.color, vertex.data);
                    }));

            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw triangle
            for (int i = 0; i < vertexListsForControl.Count; i++)
            {
                List<(float x, float z, Color color, TriangleMapData data)> vertexList = vertexListsForControl[i];
                GL.Begin(PrimitiveType.Polygon);
                foreach ((float x, float z, Color color, TriangleMapData data) in vertexList)
                {
                    byte opacityByte = OpacityByte;
                    if (this == hoverData?.MapObject &&
                        data.Tri.Address == hoverData?.Tri?.Address &&
                        (!hoverData.Index.HasValue || hoverData.Index.Value == i))
                    {
                        opacityByte = MapUtilities.GetHoverOpacityByte();
                    }
                    GL.Color4(color.R, color.G, color.B, opacityByte);
                    GL.Vertex2(x, z);
                }
                GL.End();
            }

            // Draw arrows
            bool isShowingTriUnits = MapUtilities.IsAbleToShowUnitPrecision() && GetShowTriUnits();
            if (_showArrows && !isShowingTriUnits)
            {
                for (int i = 0; i < vertexLists.Count; i++)
                {
                    var vertexList = vertexLists[i];

                    float x1 = (vertexList[0].x + vertexList[3].x) / 2;
                    float y1 = (vertexList[0].y + vertexList[3].y) / 2;
                    float z1 = (vertexList[0].z + vertexList[3].z) / 2;
                    float x2 = (vertexList[1].x + vertexList[2].x) / 2;
                    float y2 = (vertexList[1].y + vertexList[2].y) / 2;
                    float z2 = (vertexList[1].z + vertexList[2].z) / 2;

                    (float controlX1, float controlZ1) = MapUtilities.ConvertCoordsForControlOrthographicView(vertexList[0].x, vertexList[0].y, vertexList[0].z, UseRelativeCoordinates);
                    (float controlX2, float controlZ2) = MapUtilities.ConvertCoordsForControlOrthographicView(vertexList[1].x, vertexList[1].y, vertexList[1].z, UseRelativeCoordinates);
                    (float controlX3, float controlZ3) = MapUtilities.ConvertCoordsForControlOrthographicView(vertexList[2].x, vertexList[2].y, vertexList[2].z, UseRelativeCoordinates);

                    double angle1 = MoreMath.AngleTo_AngleUnits(controlX1, controlZ1, controlX2, controlZ2);
                    double angle2 = MoreMath.AngleTo_AngleUnits(controlX2, controlZ2, controlX3, controlZ3);
                    double angleDiff = angle2 - angle1;
                    double angleDiffCoefficient = 1 / Math.Abs(Math.Sin(MoreMath.AngleUnitsToRadians(angleDiff)));

                    double totalDistance = MoreMath.GetDistanceBetween(x1, y1, z1, x2, y2, z2);
                    List<double> markDistances = new List<double>();
                    if (totalDistance < 100 * angleDiffCoefficient)
                    {
                        markDistances.Add(totalDistance / 2);
                    }
                    else
                    {
                        double firstDistance = 25 * angleDiffCoefficient;
                        double lastDistance = totalDistance - 25 * angleDiffCoefficient;
                        double distanceDiff = lastDistance - firstDistance;
                        int numMarks = (int)Math.Truncate(distanceDiff / 50 + 0.25) + 1;
                        int numBetweens = numMarks - 1;
                        double betweenDistance = distanceDiff / numBetweens;
                        for (int j = 0; j < numMarks; j++)
                        {
                            markDistances.Add(firstDistance + j * betweenDistance);
                        }
                    }

                    List<(float x, float y, float z)> markPoints = new List<(float x, float y, float z)>();
                    foreach (double dist in markDistances)
                    {
                        double portion = dist / totalDistance;
                        (double x, double y, double z) point = (x1 + portion * (x2 - x1), y1 + portion * (y2 - y1), z1 + portion * (z2 - z1));
                        markPoints.Add(((float x, float y, float z))point);
                    }

                    if (MapConfig.MapUseNotForCeilings == 1 && vertexList[0].data.Tri.IsCeiling())
                    {
                        TriangleDataModel tri = vertexList[0].data.Tri;
                        float size = GetSizeForOrthographicView(tri.Classification);
                        double notRadiusLength = 0.4 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;
                        double notLineThickness = 0.15 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;

                        double angleUp = 0;
                        double angleDown = angleUp + 32768;
                        double angleLeft = angleUp + 16384;
                        double angleRight = angleUp - 16384;
                        double angleUpLeft = angleUp + 8192;
                        double angleUpRight = angleUp - 8192;
                        double angleDownLeft = angleUp + 24576;
                        double angleDownRight = angleUp - 24576;

                        foreach (var point in markPoints)
                        {
                            var controlPoint = MapUtilities.ConvertCoordsForControlOrthographicView(point.x, point.y, point.z, UseRelativeCoordinates);

                            List<(float x, float z)> outerCirclePoints = Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                                .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 65536)
                                .ConvertAll(angle => ((float, float))MoreMath.AddVectorToPoint(notRadiusLength, angle, controlPoint.x, controlPoint.z));
                            List<(float x, float z)> innerCirclePoints = Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                                .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 65536)
                                .ConvertAll(angle => ((float, float))MoreMath.AddVectorToPoint(notRadiusLength - notLineThickness, angle, controlPoint.x, controlPoint.z));

                            Color notColor = vertexList[0].color.Darken(0.5);
                            byte opacityByte = OpacityByte;
                            if (this == hoverData?.MapObject && vertexList[0].data.Tri.Address == hoverData?.Tri?.Address && hoverData?.Index == i)
                            {
                                opacityByte = MapUtilities.GetHoverOpacityByte();
                            }
                            GL.Color4(notColor.R, notColor.G, notColor.B, opacityByte);

                            double firstAngle = 0.125;
                            double secondAngle = firstAngle + 0.5;
                            double angleRadius = 0.05;

                            int firstGapStart = (int)(outerCirclePoints.Count * (firstAngle - angleRadius));
                            int firstGapEnd = (int)(outerCirclePoints.Count * (firstAngle + angleRadius));
                            int secondGapStart = (int)(outerCirclePoints.Count * (secondAngle - angleRadius));
                            int secondGapEnd = (int)(outerCirclePoints.Count * (secondAngle + angleRadius));

                            GL.Begin(PrimitiveType.QuadStrip);
                            for (int j = 0; j <= outerCirclePoints.Count; j++)
                            {
                                int indexOuter = j;
                                int indexInner = j;
                                if (j > firstGapStart && j < firstGapEnd) indexInner = firstGapStart;
                                if (j > secondGapStart && j < secondGapEnd) indexInner = secondGapStart;
                                var outerPoint = outerCirclePoints[indexOuter % outerCirclePoints.Count];
                                var innerPoint = innerCirclePoints[indexInner % outerCirclePoints.Count];
                                GL.Vertex2(outerPoint.x, outerPoint.z);
                                GL.Vertex2(innerPoint.x, innerPoint.z);
                            }
                            GL.End();

                            GL.Begin(PrimitiveType.Quads);
                            GL.Vertex2(innerCirclePoints[firstGapStart].x, innerCirclePoints[firstGapStart].z);
                            GL.Vertex2(innerCirclePoints[firstGapEnd].x, innerCirclePoints[firstGapEnd].z);
                            GL.Vertex2(innerCirclePoints[secondGapStart].x, innerCirclePoints[secondGapStart].z);
                            GL.Vertex2(innerCirclePoints[secondGapEnd].x, innerCirclePoints[secondGapEnd].z);
                            GL.End();
                        }
                    }
                    else if (MapConfig.MapUseXForCeilings == 1 && vertexList[0].data.Tri.IsCeiling())
                    {
                        TriangleDataModel tri = vertexList[0].data.Tri;
                        float size = GetSizeForOrthographicView(tri.Classification);
                        double xBranchLength = 0.4 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;
                        double xLineThickness = 0.2 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;

                        double angleUp = 0;
                        double angleDown = angleUp + 32768;
                        double angleLeft = angleUp + 16384;
                        double angleRight = angleUp - 16384;
                        double angleUpLeft = angleUp + 8192;
                        double angleUpRight = angleUp - 8192;
                        double angleDownLeft = angleUp + 24576;
                        double angleDownRight = angleUp - 24576;

                        foreach (var point in markPoints)
                        {
                            var controlPoint = MapUtilities.ConvertCoordsForControlOrthographicView(point.x, point.y, point.z, UseRelativeCoordinates);

                            (float x, float z) topLeftPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength, angleUpLeft, controlPoint.x, controlPoint.z);
                            (float x, float z) topRightPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength, angleUpRight, controlPoint.x, controlPoint.z);
                            (float x, float z) bottomLeftPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength, angleDownLeft, controlPoint.x, controlPoint.z);
                            (float x, float z) bottomRightPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength, angleDownRight, controlPoint.x, controlPoint.z);

                            (float x, float z) topLeftPointBottomLeft = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleDownLeft, topLeftPoint.x, topLeftPoint.z);
                            (float x, float z) topLeftPointTopRight = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleUpRight, topLeftPoint.x, topLeftPoint.z);
                            (float x, float z) topRightPointTopLeft = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleUpLeft, topRightPoint.x, topRightPoint.z);
                            (float x, float z) topRightPointBottomRight = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleDownRight, topRightPoint.x, topRightPoint.z);
                            (float x, float z) bottomLeftPointTopLeft = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleUpLeft, bottomLeftPoint.x, bottomLeftPoint.z);
                            (float x, float z) bottomLeftPointBottomRight = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleDownRight, bottomLeftPoint.x, bottomLeftPoint.z);
                            (float x, float z) bottomRightPointBottomLeft = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleDownLeft, bottomRightPoint.x, bottomRightPoint.z);
                            (float x, float z) bottomRightPointTopRight = ((float, float))MoreMath.AddVectorToPoint(
                                xLineThickness / 2, angleUpRight, bottomRightPoint.x, bottomRightPoint.z);

                            (float x, float z) topPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength - xLineThickness / 2, angleDownRight, topLeftPointTopRight.x, topLeftPointTopRight.z);
                            (float x, float z) leftPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength - xLineThickness / 2, angleDownRight, topLeftPointBottomLeft.x, topLeftPointBottomLeft.z);
                            (float x, float z) bottomPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength - xLineThickness / 2, angleUpLeft, bottomRightPointBottomLeft.x, bottomRightPointBottomLeft.z);
                            (float x, float z) rightPoint = ((float, float))MoreMath.AddVectorToPoint(
                                xBranchLength - xLineThickness / 2, angleUpLeft, bottomRightPointTopRight.x, bottomRightPointTopRight.z);

                            List<(float x, float z)> xPoints =
                                new List<(float x, float z)>()
                                {
                                    topPoint,
                                    topRightPointTopLeft,
                                    topRightPointBottomRight,
                                    rightPoint,
                                    bottomRightPointTopRight,
                                    bottomRightPointBottomLeft,
                                    bottomPoint,
                                    bottomLeftPointBottomRight,
                                    bottomLeftPointTopLeft,
                                    leftPoint,
                                    topLeftPointBottomLeft,
                                    topLeftPointTopRight,
                                };

                            Color xColor = vertexList[0].color.Darken(0.5);
                            GL.Begin(PrimitiveType.Polygon);
                            foreach (var xPoint in xPoints)
                            {
                                byte opacityByte = OpacityByte;
                                if (this == hoverData?.MapObject && vertexList[0].data.Tri.Address == hoverData?.Tri?.Address && hoverData?.Index == i)
                                {
                                    opacityByte = MapUtilities.GetHoverOpacityByte();
                                }
                                GL.Color4(xColor.R, xColor.G, xColor.B, opacityByte);
                                GL.Vertex2(xPoint.x, xPoint.z);
                            }
                            GL.End();
                        }
                    }
                    else
                    {
                        double arrowAngle;
                        TriangleDataModel tri = vertexList[0].data.Tri;
                        switch (tri.Classification)
                        {
                            case TriangleClassification.Wall:
                                double wallAngleDiff = MoreMath.GetAngleDifference(Config.CurrentMapGraphics.MapViewYawValue, tri.GetPushAngle());
                                arrowAngle = wallAngleDiff > 0 ? 49152 : 16384;
                                break;
                            case TriangleClassification.Floor:
                                arrowAngle = 32768;
                                break;
                            case TriangleClassification.Ceiling:
                                arrowAngle = 0;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        float size = GetSizeForOrthographicView(tri.Classification);
                        double arrowBaseLength = 0.4 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;
                        double arrowSideLength = 0.2 * Math.Min(size, 50) * Config.CurrentMapGraphics.MapViewScaleValue;

                        double angleUp = arrowAngle;
                        double angleDown = arrowAngle + 32768;
                        double angleLeft = arrowAngle + 16384;
                        double angleRight = arrowAngle - 16384;
                        double angleUpLeft = arrowAngle + 8192;
                        double angleUpRight = arrowAngle - 8192;
                        double angleDownLeft = arrowAngle + 24576;
                        double angleDownRight = arrowAngle - 24576;

                        foreach (var point in markPoints)
                        {
                            var controlPoint = MapUtilities.ConvertCoordsForControlOrthographicView(point.x, point.y, point.z, UseRelativeCoordinates);

                            (float x, float z) frontPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength, angleUp, controlPoint.x, controlPoint.z);
                            (float x, float z) leftOuterPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength / 2 + arrowSideLength, angleLeft, controlPoint.x, controlPoint.z);
                            (float x, float z) leftInnerPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength / 2, angleLeft, controlPoint.x, controlPoint.z);
                            (float x, float z) rightOuterPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength / 2 + arrowSideLength, angleRight, controlPoint.x, controlPoint.z);
                            (float x, float z) rightInnerPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength / 2, angleRight, controlPoint.x, controlPoint.z);
                            (float x, float z) backLeftPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength, angleDown, leftInnerPoint.x, leftInnerPoint.z);
                            (float x, float z) backRightPoint = ((float, float))MoreMath.AddVectorToPoint(
                                arrowBaseLength, angleDown, rightInnerPoint.x, rightInnerPoint.z);

                            List<(float x, float z)> arrowPoints =
                                new List<(float x, float z)>()
                                {
                                frontPoint,
                                leftOuterPoint,
                                leftInnerPoint,
                                backLeftPoint,
                                backRightPoint,
                                rightInnerPoint,
                                rightOuterPoint,
                                };

                            Color arrowColor = vertexList[0].color.Darken(0.5);
                            GL.Begin(PrimitiveType.Polygon);
                            foreach (var arrowPoint in arrowPoints)
                            {
                                byte opacityByte = OpacityByte;
                                if (this == hoverData?.MapObject && vertexList[0].data.Tri.Address == hoverData?.Tri?.Address && hoverData?.Index == i)
                                {
                                    opacityByte = MapUtilities.GetHoverOpacityByte();
                                }
                                GL.Color4(arrowColor.R, arrowColor.G, arrowColor.B, opacityByte);
                                GL.Vertex2(arrowPoint.x, arrowPoint.z);
                            }
                            GL.End();
                        }
                    }
                }
            }

            // Draw outline
            if (LineWidth != 0)
            {
                GL.Color4(LineColor.R, LineColor.G, LineColor.B, (byte)255);
                GL.LineWidth(LineWidth);
                foreach (List<(float x, float z, Color color, TriangleMapData data)> vertexList in vertexListsForControl)
                {
                    GL.Begin(PrimitiveType.LineLoop);
                    foreach ((float x, float z, Color color, TriangleMapData data) in vertexList)
                    {
                        GL.Vertex2(x, z);
                    }
                    GL.End();
                }
            }

            GL.Color4(1, 1, 1, 1.0f);
        }

        public void DrawOn2DControlOrthographicViewTotal(MapObjectHoverData hoverData)
        {
            List<List<(float x, float y, float z, Color color, TriangleDataModel tri)>> vertexLists =
                GetFilteredTriangles().ConvertAll(tri =>
                {
                    Color color = GetColorForOrthographicView(tri.Classification);
                    return tri.Get3DVertices().ConvertAll(vertex => (vertex.x, vertex.y + GetWallRelativeHeightForOrthographicViewTotal(), vertex.z, color, tri));
                });

            List<List<(float x, float y, float z, Color color, TriangleDataModel tri)>> vertexListsForControl =
                vertexLists.ConvertAll(vertexList => vertexList.ConvertAll(
                    vertex =>
                    {
                        (float x, float z) = MapUtilities.ConvertCoordsForControlOrthographicView(vertex.x, vertex.y, vertex.z, UseRelativeCoordinates);
                        return (x, vertex.y, z, vertex.color, vertex.tri);
                    }));

            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw triangle
            foreach (List<(float x, float y, float z, Color color, TriangleDataModel tri)> vertexList in vertexListsForControl)
            {
                GL.Begin(PrimitiveType.Polygon);
                foreach ((float x, float y, float z, Color color, TriangleDataModel tri) in vertexList)
                {
                    byte opacityByte = OpacityByte;
                    if (this == hoverData?.MapObject && tri.Address == hoverData?.Tri?.Address && !hoverData.Index2.HasValue)
                    {
                        opacityByte = MapUtilities.GetHoverOpacityByte();
                    }
                    Color colorToUse = color;
                    if (_colorByHeight)
                    {
                        colorToUse = GetColorForHeight(y);
                    }
                    GL.Color4(colorToUse.R, colorToUse.G, colorToUse.B, opacityByte);
                    GL.Vertex2(x, z);
                }
                GL.End();
            }

            // Draw outline
            if (LineWidth != 0)
            {
                GL.Color4(LineColor.R, LineColor.G, LineColor.B, (byte)255);
                GL.LineWidth(LineWidth);
                foreach (List<(float x, float y, float z, Color color, TriangleDataModel tri)> vertexList in vertexListsForControl)
                {
                    GL.Begin(PrimitiveType.LineLoop);
                    foreach ((float x, float y, float z, Color color, TriangleDataModel tri) in vertexList)
                    {
                        GL.Vertex2(x, z);
                    }
                    GL.End();
                }
            }

            if (_customImage != null)
            {
                foreach (var vertexList in vertexListsForControl)
                {
                    for (int i = 0; i < vertexList.Count; i++)
                    {
                        var vertex = vertexList[i];
                        PointF point = new PointF(vertex.x, vertex.z);
                        SizeF size = MapUtilities.ScaleImageSizeForControl(_customImage.Size, _iconSize, Scales);
                        double opacity = 1;
                        if (this == hoverData?.MapObject && vertex.tri.Address == hoverData?.Tri?.Address && i == hoverData.Index2)
                        {
                            opacity = MapUtilities.GetHoverOpacity();
                        }
                        MapUtilities.DrawTexture(_customImageTex.Value, point, size, 0, opacity);
                    }
                }
            }

            GL.Color4(1, 1, 1, 1.0f);
        }

        public override void Update()
        {
            if (_colorByHeight)
            {
                var vertexLists = GetVertexLists();
                if (vertexLists.Count > 0)
                {
                    _maxColorY = vertexLists.Max(list => list.Max(v => v.y + GetWallRelativeHeightForOrthographicViewTotal()));
                    _minColorY = vertexLists.Min(list => list.Min(v => v.y + GetWallRelativeHeightForOrthographicViewTotal()));
                }
            }
        }

        protected Color GetColorForHeight(float y)
        {
            double proportion = (y - _minColorY) / (_maxColorY - _minColorY);
            proportion *= 0.9;
            proportion = MoreMath.Clamp(proportion, 0, 1);
            return ColorUtilities.Rainbow((float)proportion);
        }

        protected List<ToolStripMenuItem> GetTriangleToolStripMenuItems()
        {
            _itemShowArrows = new ToolStripMenuItem("Show Arrows");
            _itemShowArrows.Click += (sender, e) =>
            {
                MapObjectSettings settings = new MapObjectSettings(
                    changeTriangleShowArrows: true, newTriangleShowArrows: !_showArrows);
                GetParentMapTracker().ApplySettings(settings);
            };

            _itemUseCrossSection = new ToolStripMenuItem("Use Cross Section");
            _itemUseCrossSection.Click += (sender, e) =>
            {
                MapObjectSettings settings = new MapObjectSettings(
                    changeUseCrossSection: true, newUseCrossSection: !_useCrossSection);
                GetParentMapTracker().ApplySettings(settings);
            };

            _itemColorByHeight = new ToolStripMenuItem("Color by Height");
            _itemColorByHeight.Click += (sender, e) =>
            {
                MapObjectSettings settings = new MapObjectSettings(
                    changeColorByHeight: true, newColorByHeight: !_colorByHeight);
                GetParentMapTracker().ApplySettings(settings);
            };

            string suffix = string.Format(" ({0})", _iconSize);
            _itemSetIconSize = new ToolStripMenuItem(SET_ICON_SIZE_TEXT + suffix);
            _itemSetIconSize.Click += (sender, e) =>
            {
                string text = DialogUtilities.GetStringFromDialog(labelText: "Enter icon size.");
                float? iconSizeNullable = ParsingUtilities.ParseFloatNullable(text);
                if (!iconSizeNullable.HasValue) return;
                float iconSize = iconSizeNullable.Value;
                MapObjectSettings settings = new MapObjectSettings(
                    changeIconSize: true, newIconSize: iconSize);
                GetParentMapTracker().ApplySettings(settings);
            };

            _itemSetWithinDist = new ToolStripMenuItem(SET_WITHIN_DIST_TEXT);
            _itemSetWithinDist.Click += (sender, e) =>
            {
                string text = DialogUtilities.GetStringFromDialog(labelText: "Enter the vertical distance from the center (default: Mario) within which to show tris.");
                float? withinDistNullable = ParsingUtilities.ParseFloatNullable(text);
                if (!withinDistNullable.HasValue) return;
                MapObjectSettings settings = new MapObjectSettings(
                    changeTriangleWithinDist: true, newTriangleWithinDist: withinDistNullable.Value);
                GetParentMapTracker().ApplySettings(settings);
            };

            ToolStripMenuItem itemClearWithinDist = new ToolStripMenuItem("Clear Within Dist");
            itemClearWithinDist.Click += (sender, e) =>
            {
                MapObjectSettings settings = new MapObjectSettings(
                    changeTriangleWithinDist: true, newTriangleWithinDist: null);
                GetParentMapTracker().ApplySettings(settings);
            };

            _itemSetWithinCenter = new ToolStripMenuItem(SET_WITHIN_CENTER_TEXT);
            _itemSetWithinCenter.Click += (sender, e) =>
            {
                string text = DialogUtilities.GetStringFromDialog(labelText: "Enter the center y of the within-dist range.");
                float? withinCenterNullable =
                    text == "" ?
                    Config.Stream.GetFloat(MarioConfig.StructAddress + MarioConfig.YOffset) :
                    ParsingUtilities.ParseFloatNullable(text);
                if (!withinCenterNullable.HasValue) return;
                MapObjectSettings settings = new MapObjectSettings(
                    changeTriangleWithinCenter: true, newTriangleWithinCenter: withinCenterNullable.Value);
                GetParentMapTracker().ApplySettings(settings);
            };

            ToolStripMenuItem itemClearWithinCenter = new ToolStripMenuItem("Clear Within Center");
            itemClearWithinCenter.Click += (sender, e) =>
            {
                MapObjectSettings settings = new MapObjectSettings(
                    changeTriangleWithinCenter: true, newTriangleWithinCenter: null);
                GetParentMapTracker().ApplySettings(settings);
            };

            return new List<ToolStripMenuItem>()
            {
                _itemShowArrows,
                _itemUseCrossSection,
                _itemColorByHeight,
                _itemSetIconSize,
                _itemSetWithinDist,
                itemClearWithinDist,
                _itemSetWithinCenter,
                itemClearWithinCenter,
            };
        }

        public override void ApplySettings(MapObjectSettings settings)
        {
            base.ApplySettings(settings);

            if (settings.ChangeTriangleShowArrows)
            {
                _showArrows = settings.NewTriangleShowArrows;
                _itemShowArrows.Checked = settings.NewTriangleShowArrows;
            }

            if (settings.ChangeUseCrossSection)
            {
                _useCrossSection = settings.NewUseCrossSection;
                _itemUseCrossSection.Checked = settings.NewUseCrossSection;
            }

            if (settings.ChangeColorByHeight)
            {
                _colorByHeight = settings.NewColorByHeight;
                _itemColorByHeight.Checked = settings.NewColorByHeight;
            }

            if (settings.ChangeIconSize)
            {
                _iconSize = settings.NewIconSize;
                string suffix = string.Format(" ({0})", settings.NewIconSize);
                _itemSetIconSize.Text = SET_ICON_SIZE_TEXT + suffix;
            }

            if (settings.ChangeTriangleWithinDist)
            {
                _withinDist = settings.NewTriangleWithinDist;
                string suffix = _withinDist.HasValue ? string.Format(" ({0})", _withinDist.Value) : "";
                _itemSetWithinDist.Text = SET_WITHIN_DIST_TEXT + suffix;
            }

            if (settings.ChangeTriangleWithinCenter)
            {
                _withinCenter = settings.NewTriangleWithinCenter;
                string suffix = _withinCenter.HasValue ? string.Format(" ({0})", _withinCenter.Value) : "";
                _itemSetWithinCenter.Text = SET_WITHIN_CENTER_TEXT + suffix;
            }
        }

        public override MapDrawType GetDrawType()
        {
            return MapDrawType.Perspective;
        }

        public void ToggleUseCrossSection()
        {
            _useCrossSection = !_useCrossSection;
        }

        public void ToggleShowArrows()
        {
            _showArrows = !_showArrows;
        }

        private (float x, float y, float z) GetMidpointOfTriUnitOrthographicCrossSection(List<(float x, float y, float z, Color color, TriangleMapData data)> data)
        {
            float y = data.Max(p => p.y);

            float xAverage = data.Average(v => v.x);
            float zAverage = data.Average(v => v.z);
            float xMidpoint = (int)xAverage + (xAverage >= 0 ? 0.5f : -0.5f);
            float zMidpoint = (int)zAverage + (zAverage >= 0 ? 0.5f : -0.5f);

            return (xMidpoint, y, zMidpoint);
        }

        public override MapObjectHoverData GetHoverDataOrthographicView(bool isForObjectDrag, bool forceCursorPosition)
        {
            Point? relPosMaybe = MapObjectHoverData.GetPositionMaybe(isForObjectDrag, forceCursorPosition);
            if (!relPosMaybe.HasValue) return null;
            Point relPos = relPosMaybe.Value;

            if (_useCrossSection)
            {
                List<List<(float x, float y, float z, Color color, TriangleMapData data)>> tris =
                    GetOrthographicCrossSectionVertexLists();
                List<List<(float x, float z)>> trisForControl =
                    tris.ConvertAll(vertexList => vertexList.ConvertAll(
                        vertex => MapUtilities.ConvertCoordsForControlOrthographicView(vertex.x, vertex.y, vertex.z, UseRelativeCoordinates)));

                for (int i = trisForControl.Count - 1; i >= 0; i--)
                {
                    var triForControl = trisForControl[i];
                    if (MapUtilities.IsWithinShapeForControl(triForControl, relPos.X, relPos.Y, forceCursorPosition))
                    {
                        TriangleDataModel tri = tris[i][0].data.Tri;
                        if (MapUtilities.IsAbleToShowUnitPrecision() && GetShowTriUnits())
                        {
                            (float x, float y, float z) = GetMidpointOfTriUnitOrthographicCrossSection(tris[i]);
                            return new MapObjectHoverData(this, MapObjectHoverDataEnum.Triangle, x, y, z, tri: tri, index: i, isTriUnit: true);
                        }
                        else
                        {
                            return new MapObjectHoverData(this, MapObjectHoverDataEnum.Triangle, tri.GetMidpointX(), tri.GetMidpointY(), tri.GetMidpointZ(), tri: tri);
                        }
                    }
                }
                return null;
            }
            else
            {
                List<TriangleDataModel> tris = GetFilteredTriangles();
                List<List<(float x, float z)>> trisForControl = tris
                    .ConvertAll(tri => tri.Get3DVertices())
                    .ConvertAll(vertices => vertices.ConvertAll(
                        vertex => MapUtilities.ConvertCoordsForControlOrthographicView(vertex.x, vertex.y, vertex.z, UseRelativeCoordinates)));
                if (_customImage != null)
                {
                    for (int i = trisForControl.Count - 1; i >= 0; i--)
                    {
                        var triForControl = trisForControl[i];
                        for (int j = 0; j < triForControl.Count; j++)
                        {
                            var vertex = triForControl[j];
                            double dist = MoreMath.GetDistanceBetween(vertex.x, vertex.z, relPos.X, relPos.Y);
                            double radius = Scales ? _iconSize * Config.CurrentMapGraphics.MapViewScaleValue : _iconSize;
                            if (dist <= radius || forceCursorPosition)
                            {
                                TriangleDataModel tri = tris[i];
                                (int x, int y, int z) = j == 0 ? tri.GetP1() : j == 1 ? tri.GetP2() : tri.GetP3();
                                return new MapObjectHoverData(this, MapObjectHoverDataEnum.Icon, x, y, z, tri: tri, index: i, index2: j, info: string.Format("V{0}", j + 1));
                            }
                        }
                    }
                }
                for (int i = trisForControl.Count - 1; i >= 0; i--)
                {
                    var triForControl = trisForControl[i];
                    if (MapUtilities.IsWithinShapeForControl(triForControl, relPos.X, relPos.Y, forceCursorPosition))
                    {
                        TriangleDataModel tri = tris[i];
                        return new MapObjectHoverData(this, MapObjectHoverDataEnum.Triangle, tri.GetMidpointX(), tri.GetMidpointY(), tri.GetMidpointZ(), tri: tri);
                    }
                }
                return null;
            }
        }

        public override List<ToolStripItem> GetHoverContextMenuStripItems(MapObjectHoverData hoverData)
        {
            List<ToolStripItem> output = base.GetHoverContextMenuStripItems(hoverData);

            if (hoverData.IsTriUnit)
            {
                ToolStripMenuItem copyBasePointItem = MapUtilities.CreateCopyItem((int)hoverData.X, (int)hoverData.Y, (int)hoverData.Z, "Base Point");
                output.Insert(0, copyBasePointItem);

                ToolStripMenuItem copyMidpointItem = MapUtilities.CreateCopyItem(hoverData.X, hoverData.Y, hoverData.Z, "Midpoint");
                output.Insert(1, copyMidpointItem);

                ToolStripMenuItem copyYItem = new ToolStripMenuItem("Copy Y");
                copyYItem.Click += (sender, e) => Clipboard.SetText(hoverData.Y.ToString());
                output.Insert(2, copyYItem);
            }
            else if (hoverData.Index2.HasValue)
            {
                ToolStripMenuItem copyPositionItem = MapUtilities.CreateCopyItem(hoverData.X, hoverData.Y, hoverData.Z, "Position");
                output.Insert(0, copyPositionItem);
            }
            else
            {
                ToolStripMenuItem selectInTrianglesTabItem = new ToolStripMenuItem("Select in Triangles Tab");
                selectInTrianglesTabItem.Click += (sender, e) =>
                {
                    Config.TriangleManager.SetCustomTriangleAddresses(hoverData.Tri.Address);
                    List<TabPage> tabPages = ControlUtilities.GetTabPages(Config.TabControlMain);
                    bool containsTab = tabPages.Any(tabPage => tabPage == Config.TriangleManager.Tab);
                    if (containsTab) Config.TabControlMain.SelectTab(Config.TriangleManager.Tab);
                };
                output.Insert(0, selectInTrianglesTabItem);

                ToolStripMenuItem copyAddressItem = new ToolStripMenuItem("Copy Address");
                copyAddressItem.Click += (sender, e) => Clipboard.SetText(HexUtilities.FormatValue(hoverData.Tri.Address));
                output.Insert(1, copyAddressItem);

                ToolStripMenuItem copyCoordinatesItem = new ToolStripMenuItem("Copy Coordinates");
                copyCoordinatesItem.Click += (sender, e) => Clipboard.SetText(string.Join(", ", hoverData.Tri.GetCoordinates()));
                output.Insert(2, copyCoordinatesItem);

                ToolStripMenuItem annihilateItem = new ToolStripMenuItem("Annihilate");
                annihilateItem.Click += (sender, e) => ButtonUtilities.AnnihilateTriangle(new List<uint>() { hoverData.Tri.Address });
                output.Insert(3, annihilateItem);

                ToolStripMenuItem unloadAssociatedObjectItem = new ToolStripMenuItem("Unload Associated Object");
                unloadAssociatedObjectItem.Click += (sender, e) =>
                {
                    uint objAddress = hoverData.Tri.AssociatedObject;
                    if (objAddress == 0) return;
                    ObjectDataModel obj = new ObjectDataModel(objAddress);
                    ButtonUtilities.UnloadObject(new List<ObjectDataModel>() { obj });
                };
                output.Insert(4, unloadAssociatedObjectItem);
            }

            return output;
        }
    }
}
