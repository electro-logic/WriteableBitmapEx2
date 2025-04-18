# WriteableBitmapEx v2

Project based on https://github.com/reneschulte/WriteableBitmapEx

What's changed
- Visual Studio 2022 / .NET 9 support
- RotateFree rewritten from scratch (10x faster)
- New Binning, SetRow and CropRelative method
- ConvertColor refactored to extension method and renamed ToColorInt()
- Removed legacy Silverlight, Windows Phone, and UWP support
- Removed samples projects and legacy unit tests to simplify the code base

# Features

*   Base
    *   Support for System.Windows.Media.Color (alpha premultiplication will be performed)
    *   Also overloads for faster int32 as color (assumed to be already alpha premultiplied)
    *   SetPixel method with various overloads
    *   GetPixel method to get the pixel color at a specified x, y coordinate
    *   Fast Clear methods
    *   Fast Clone method to copy a WriteableBitmap
    *   ForEach method to apply a given function to all pixels of the bitmap
*   Transformation
    *   Crop method to extract a defined region
    *   Resize method with support for bilinear interpolation and nearest neighbor
    *   Rotate in 90° steps clockwise and any arbitrary angle
    *   Flip vertical and horizontal
*   Shapes
    *   Fast line drawing algorithms including various anti-aliased algorithm
    *   Variable stroke thickness, dotted and penned / stamp lines
    *   Ellipse, polyline, quad, rectangle and triangle
    *   Cubic Beziér, Cardinal spline and closed curves
*   Filled shapes
    *   Fast ellipse and rectangle fill method
    *   Triangle, quad, simple and complex polygons
    *   Beziér and Cardinal spline curves
*	Text
	*	Fill and draw outline of text strings. text is highly flexible, it is instance of `FormattedText` thus any text and characted which is supported by wpf, can be rendered (options like `FlowDirection`, `FontWeight` and ... can be changed).
*   Blitting
    *   Different blend modes including alpha, additive, subtractive, multiply, mask and none
    *   Optimized fast path for non blended blitting
    *   Special BlitRender to apply affine transformation with bilinear interpolation
*   Filtering
    *   Convolution, Blur
    *   Brightness, contrast, gamma adjustments
    *   Gray/brightness, invert
*   Conversion
    *   Convert a WriteableBitmap to a byte array
    *   Create a WriteableBitmap from a byte array
    *   Create a WriteableBitmap easily from the application resource or content
    *   Create a WriteableBitmap from an any platform supported image stream
    *   Write a WriteableBitmap as a TGA image to a stream
    *   Separate extension method to save as a PNG image

# Usage examples

```cs
// Initialize the WriteableBitmap with size 512x512 and set it as source of an Image control
WriteableBitmap writeableBmp = BitmapFactory.New(512, 512);
ImageControl.Source = writeableBmp;
using(writeableBmp.GetBitmapContext())
{

   // Load an image from the calling Assembly's resources via the relative path
   writeableBmp = BitmapFactory.New(1, 1).FromResource("Data/flower2.png");

   // Clear the WriteableBitmap with white color
   writeableBmp.Clear(Colors.White);

   // Set the pixel at P(10, 13) to black
   writeableBmp.SetPixel(10, 13, Colors.Black);

   // Get the color of the pixel at P(30, 43)
   Color color = writeableBmp.GetPixel(30, 43);

   // Green line from P1(1, 2) to P2(30, 40)
   writeableBmp.DrawLine(1, 2, 30, 40, Colors.Green);

   // Line from P1(1, 2) to P2(30, 40) using the fastest draw line method 
   int[] pixels = writeableBmp.Pixels;
   int w = writeableBmp.PixelWidth;
   int h = writeableBmp.PixelHeight;
   WriteableBitmapExtensions.DrawLine(pixels, w, h, 1, 2, 30, 40, myIntColor);

   // Blue anti-aliased line from P1(10, 20) to P2(50, 70) with a stroke of 5
   writeableBmp.DrawLineAa(10, 20, 50, 70, Colors.Blue, 5);
   
   // Fills a text on the bitmap, Font, size, weight and almost any option is changable, all text supported with WPF is also supported here including Persian, Arabic, Chinese etc
   var formattedText = new FormattedText("Test String", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface(new FontFamily("Sans MS"), FontStyles.Normal, FontWeights.Medium, FontStretches.Normal), 80.0, System.Windows.Media.Brushes.Black);
   writeableBmp.FillText(formattedText, 100, 100, Colors.Blue, 5);
   
   // Black triangle with the points P1(10, 5), P2(20, 40) and P3(30, 10)
   writeableBmp.DrawTriangle(10, 5, 20, 40, 30, 10, Colors.Black);

   // Red rectangle from the point P1(2, 4) that is 10px wide and 6px high
   writeableBmp.DrawRectangle(2, 4, 12, 10, Colors.Red);

   // Filled blue ellipse with the center point P1(2, 2) that is 8px wide and 5px high
   writeableBmp.FillEllipseCentered(2, 2, 8, 5, Colors.Blue);

   // Closed green polyline with P1(10, 5), P2(20, 40), P3(30, 30) and P4(7, 8)
   int[] p = new int[] { 10, 5, 20, 40, 30, 30, 7, 8, 10, 5 };
   writeableBmp.DrawPolyline(p, Colors.Green);

   // Cubic Beziér curve from P1(5, 5) to P4(20, 7) 
   // with the control points P2(10, 15) and P3(15, 0)
   writeableBmp.DrawBezier(5, 5, 10, 15, 15, 0, 20, 7,  Colors.Purple);

   // Cardinal spline with a tension of 0.5 
   // through the points P1(10, 5), P2(20, 40) and P3(30, 30)
   int[] pts = new int[] { 10, 5, 20, 40, 30, 30};
   writeableBmp.DrawCurve(pts, 0.5,  Colors.Yellow);

   // A filled Cardinal spline with a tension of 0.5 
   // through the points P1(10, 5), P2(20, 40) and P3(30, 30) 
   writeableBmp.FillCurveClosed(pts, 0.5,  Colors.Green);

   // Blit a bitmap using the additive blend mode at P1(10, 10)
   writeableBmp.Blit(new Point(10, 10), bitmap, sourceRect, Colors.White, WriteableBitmapExtensions.BlendMode.Additive);

   // Override all pixels with a function that changes the color based on the coordinate
   writeableBmp.ForEach((x, y, color) => Color.FromArgb(color.A, (byte)(color.R / 2), (byte)(x * y), 100));

} // Invalidate and present in the Dispose call

// Take snapshot
var clone = writeableBmp.Clone();

// Save to a TGA image stream (file for example)
writeableBmp.WriteTga(stream);

// Crops the WriteableBitmap to a region starting at P1(5, 8) and 10px wide and 10px high
var cropped = writeableBmp.Crop(5, 8, 10, 10);

// Rotates a copy of the WriteableBitmap 90 degress clockwise and returns the new copy
var rotated = writeableBmp.Rotate(90);

// Flips a copy of the WriteableBitmap around the horizontal axis and returns the new copy
var flipped = writeableBmp.Flip(FlipMode.Horizontal);

// Resizes the WriteableBitmap to 200px wide and 300px high using bilinear interpolation
var resized = writeableBmp.Resize(200, 300, WriteableBitmapExtensions.Interpolation.Bilinear);
```

# Additional Information

Original blog posts: https://kodierer.blogspot.com/search/label/WriteableBitmapEx