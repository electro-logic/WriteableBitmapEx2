// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

using System.Collections.Generic;

namespace System.Windows.Media.Imaging;

/// <summary>
/// Collection of extension methods for the WriteableBitmap class.
/// </summary>
public static unsafe partial class WriteableBitmapExtensions
{
    #region Methods

    #region Fill Text

    /// <summary>
    /// Draws a filled text.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="formattedText">The text to be rendered</param>
    /// <param name="x">The x-coordinate of the text origin</param>
    /// <param name="y">The y-coordinate of the text origin</param>
    /// <param name="color">the color.</param>
    public static void FillText(this WriteableBitmap bmp, FormattedText formattedText, int x, int y, Color color)
    {
        var _textGeometry = formattedText.BuildGeometry(new Point(x, y));
        FillGeometry(bmp, _textGeometry, color);
    }

    /// <summary>
    /// Draws a filled geometry.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="geometry">The geometry to be rendered</param>
    /// <param name="color">the color.</param>
    public static void FillGeometry(WriteableBitmap bmp, Geometry geometry, Color color)
    {
        if (geometry is GeometryGroup gp)
        {
            foreach (var itm in gp.Children)
            {
                FillGeometry(bmp, itm, color);
            }
        }
        else if (geometry is PathGeometry pg)
        {
            var polygons = new List<int[]>();

            var poly = new List<int>();

            foreach (var fig in pg.Figures)
            {
                ToWriteableBitmapPolygon(fig, poly);
                polygons.Add([.. poly]);
            }

            bmp.FillPolygonsEvenOdd([.. polygons], color);
        }
    }

    #endregion

    #region Draw Text

    /// <summary>
    /// Draws an outlined text.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="formattedText">The text to be rendered</param>
    /// <param name="x">The x-coordinate of the text origin</param>
    /// <param name="y">The y-coordinate of the text origin</param>
    /// <param name="color">the color.</param>
    public static void DrawText(this WriteableBitmap bmp, FormattedText formattedText, int x, int y, Color color)
    {
        var _textGeometry = formattedText.BuildGeometry(new Point(x, y));
        DrawGeometry(bmp, _textGeometry, color);
    }

    /// <summary>
    /// Draws an outlined text.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="formattedText">The text to be rendered</param>
    /// <param name="x">The x-coordinate of the text origin</param>
    /// <param name="y">The y-coordinate of the text origin</param>
    /// <param name="color">the color.</param>
    /// <param name="thickness">the thickness.</param>
    public static void DrawTextAa(this WriteableBitmap bmp, FormattedText formattedText, int x, int y, Color color, int thickness)
    {
        var _textGeometry = formattedText.BuildGeometry(new Point(x, y));
        DrawGeometryAa(bmp, _textGeometry, color, thickness);
    }

    /// <summary>
    /// Draws outline of a geometry.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="geometry">The geometry to be rendered</param>
    /// <param name="color">the color.</param>
    private static void DrawGeometry(WriteableBitmap bmp, Geometry geometry, Color color)
    {
        if (geometry is GeometryGroup gp)
        {
            foreach (var itm in gp.Children)
            {
                DrawGeometry(bmp, itm, color);
            }
        }
        else if (geometry is PathGeometry pg)
        {
            var polygons = new List<int[]>();

            var poly = new List<int>();

            foreach (var fig in pg.Figures)
            {
                ToWriteableBitmapPolygon(fig, poly);
                polygons.Add([.. poly]);
            }

            foreach (var item in polygons)
            {
                bmp.DrawPolyline(item, color);
            }
        }
    }

    private static void DrawGeometryAa(WriteableBitmap bmp, Geometry geometry, Color col, int thickness)
    {
        if (geometry is GeometryGroup gp)
        {
            foreach (var itm in gp.Children)
            {
                DrawGeometryAa(bmp, itm, col, thickness);
            }
        }
        else if (geometry is PathGeometry pg)
        {
            var polygons = new List<int[]>();

            var poly = new List<int>();

            foreach (var fig in pg.Figures)
            {
                ToWriteableBitmapPolygon(fig, poly);
                polygons.Add([.. poly]);
            }

            foreach (var item in polygons)
            {
                bmp.DrawPolylineAa(item, col, thickness);
            }
        }
    }

    #endregion

    #region Common

    //converts the PathFigure (consis of curve, line etc) to int array polygon
    private static void ToWriteableBitmapPolygon(PathFigure fig, List<int> buf)
    {
        if (buf.Count != 0)
        {
            buf.Clear();
        }

        {
            var geo = fig;

            var lastPoint = geo.StartPoint;

            buf.Add((int)lastPoint.X);
            buf.Add((int)lastPoint.Y);

            foreach (var seg in geo.Segments)
            {
                var flag = false;

                if (seg is PolyBezierSegment pbs)
                {
                    flag = true;

                    for (int i = 0; i < pbs.Points.Count; i += 3)
                    {
                        var c1 = pbs.Points[i];
                        var c2 = pbs.Points[i + 1];
                        var en = pbs.Points[i + 2];

                        var pts = ComputeBezierPoints((int)lastPoint.X, (int)lastPoint.Y, (int)c1.X, (int)c1.Y, (int)c2.X, (int)c2.Y, (int)en.X, (int)en.Y);

                        buf.AddRange(pts);

                        lastPoint = en;
                    }
                }

                if (seg is PolyLineSegment pls)
                {
                    flag = true;

                    for (int i = 0; i < pls.Points.Count; i++)
                    {
                        var en = pls.Points[i];

                        buf.Add((int)en.X);
                        buf.Add((int)en.Y);

                        lastPoint = en;
                    }
                }

                if (seg is LineSegment ls)
                {
                    flag = true;

                    var en = ls.Point;

                    buf.Add((int)en.X);
                    buf.Add((int)en.Y);

                    lastPoint = en;
                }

                if (seg is BezierSegment bs)
                {
                    flag = true;

                    var c1 = bs.Point1;
                    var c2 = bs.Point2;
                    var en = bs.Point3;

                    var pts = ComputeBezierPoints((int)lastPoint.X, (int)lastPoint.Y, (int)c1.X, (int)c1.Y, (int)c2.X, (int)c2.Y, (int)en.X, (int)en.Y);

                    buf.AddRange(pts);

                    lastPoint = en;
                }

                if (!flag)
                {
                    throw new NotSupportedException("Error in rendering text, PathSegment type not supported");
                }
            }
        }
    }

    #endregion

    #endregion
}
