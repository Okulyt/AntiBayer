using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using AntiBeyer;
using AntiBeyer.Interfaces;

namespace Antibayer
{
    public class ImageFunctions
    {
        public static IImaging ImagingInstance = new IImagingdotNet();
        


        /// OLD METHODS
        /*

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height, InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = interpolationMode;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Image ResizeImageEasy(Image image, int new_height, int new_width, InterpolationMode InterpolationMode = InterpolationMode.NearestNeighbor)
        {
            Bitmap new_image = new Bitmap(new_width, new_height);
            Graphics g = Graphics.FromImage((Image)new_image);
            g.InterpolationMode = InterpolationMode.High;
            g.DrawImage(image, 0, 0, new_width, new_height);
            return new_image;
        }

        public static Bitmap ProcessImage(Bitmap image, HistogramSettings histogramSettings, BayerFilter markPixelColor)
        {
            if (image == null) return null;
            var targetBitmap = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                    targetBitmap.SetPixel(x, y, ProcessPixel(image.GetPixel(x, y), histogramSettings, GetbayerFilter(x, y, histogramSettings.BayerType), markPixelColor));
            return targetBitmap;
        }

        public static Color ProcessPixel(Color color, HistogramSettings histogramSettings, BayerFilter bayerFilter, BayerFilter markPixelColor)
        {
            var shift = 0;
            var multiply = 1d;
            int level = color.B;
            switch (bayerFilter)
            {
                case BayerFilter.red: shift = histogramSettings.ShiftR; multiply = histogramSettings.MultiplyR; level = color.R; break;
                case BayerFilter.green1: shift = histogramSettings.ShiftG1; multiply = histogramSettings.MultiplyG1; level = color.G; break;
                case BayerFilter.green2: shift = histogramSettings.ShiftG2; multiply = histogramSettings.MultiplyG2; level = color.G; break;
                case BayerFilter.blue: shift = histogramSettings.ShiftB; multiply = histogramSettings.MultiplyB; level = color.B; break;
            }
            level = Math.Max(0, Math.Min(255, (int)((double)level * multiply) + shift));

            if (markPixelColor == BayerFilter.red && bayerFilter == markPixelColor) return Color.FromArgb(255, RaisedLevel(level), level / 2, level / 2);
            if (markPixelColor == BayerFilter.green1 && bayerFilter == markPixelColor) return Color.FromArgb(255, level / 2, RaisedLevel(level), level / 2);
            if (markPixelColor == BayerFilter.green2 && bayerFilter == markPixelColor) return Color.FromArgb(255, level / 2, RaisedLevel(level), level / 2);
            if (markPixelColor == BayerFilter.blue && bayerFilter == markPixelColor) return Color.FromArgb(255, level / 2, level / 2, RaisedLevel(level));
            return Color.FromArgb(255, level, level, level);

        }

        private static int RaisedLevel(int level)
        {
            return Math.Min(255, level + 10);
        }

        public static BayerFilter GetbayerFilter(int x, int y, BayerType bayerType)
        {
            int odd = y % 2;
            int even = x % 2;
            var pixelIndex = odd * 2 + even;
            switch (bayerType)
            {
                case BayerType.RGGB:
                    switch (pixelIndex)
                    {
                        case 0: return BayerFilter.red;
                        case 1: return BayerFilter.green1;
                        case 2: return BayerFilter.green2;
                        case 3: return BayerFilter.blue;
                    }
                    break;
                case BayerType.GRGB:
                    switch (pixelIndex)
                    {
                        case 0: return BayerFilter.green1;
                        case 1: return BayerFilter.red;
                        case 2: return BayerFilter.green2;
                        case 3: return BayerFilter.blue;
                    }
                    break;
                case BayerType.GBRG:
                    switch (pixelIndex)
                    {
                        case 0: return BayerFilter.green1;
                        case 1: return BayerFilter.blue;
                        case 2: return BayerFilter.red;
                        case 3: return BayerFilter.green2;
                    }
                    break;
                case BayerType.BGGR:
                    switch (pixelIndex)
                    {
                        case 0: return BayerFilter.blue;
                        case 1: return BayerFilter.green1;
                        case 2: return BayerFilter.green2;
                        case 3: return BayerFilter.red;
                    }
                    break;
            }
            return BayerFilter.None;
        }


        

        public static int GetPixelValue(Bitmap image, int x, int y, BayerType bayerType)
        {
            switch (GetbayerFilter(x, y, bayerType))
            {
                case BayerFilter.green1: return image.GetPixel(x, y).G;
                case BayerFilter.green2: return image.GetPixel(x, y).G;
                case BayerFilter.red: return image.GetPixel(x, y).R;
                case BayerFilter.blue: return image.GetPixel(x, y).B;
                default: return (int)image.GetPixel(x, y).GetBrightness() * 255;
            }
        }
        */

        internal static List<BayerPixel> GetPixelsAroundPixel(IImaging imaging, Point clickPoint)
        {
            if (clickPoint.X < 1) clickPoint.X = 1;
            if (clickPoint.Y < 1) clickPoint.Y = 1;
            if (clickPoint.X > imaging.CurrentImage.Width - 1) clickPoint.X = imaging.CurrentImage.Width - 1;
            if (clickPoint.Y > imaging.CurrentImage.Height - 1) clickPoint.Y = imaging.CurrentImage.Height - 1;
            var result = new List<BayerPixel>();
            for (int y = clickPoint.Y - 1; y <= clickPoint.Y + 1; y++)
                for (int x = clickPoint.X - 1; x <= clickPoint.X + 1; x++)
                {
                    result.Add(imaging.GetBayerPixel(x, y));
                }
            return result;
        }

        public static Varianzen[] VarianceCalc(Bitmap image, BayerType bayerType)
        {
            if (image == null) return null;
            Varianzen[] varianzen = new Varianzen[] { new Varianzen(bayerType), new Varianzen(bayerType), new Varianzen(bayerType), new Varianzen(bayerType), new Varianzen(bayerType) };

            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    var bayerFilter = ImagingInstance.GetBayerFilter(x, y);
                    var centre = ImagingInstance.GetPixelValue(image, x, y);
                    var h = Math.Pow(centre - ImagingInstance.GetPixelValue(image, x - 1, y), 2) + Math.Pow(centre - ImagingInstance.GetPixelValue(image, x + 1, y), 2);
                    varianzen[(int)bayerFilter].Horizontal += h;
                    varianzen[(int)bayerFilter].HorizontalCount += 2;
                    var v = Math.Pow(centre - ImagingInstance.GetPixelValue(image, x, y - 1), 2) + Math.Pow(centre - ImagingInstance.GetPixelValue(image, x, y + 1), 2);
                    varianzen[(int)bayerFilter].Vertical += v;
                    varianzen[(int)bayerFilter].VerticalCount += 2;
                    var d1 = Math.Pow(centre - ImagingInstance.GetPixelValue(image, x - 1, y - 1), 2) + Math.Pow(centre - ImagingInstance.GetPixelValue(image, x + 1, y + 1), 2);
                    varianzen[(int)bayerFilter].Diagonal1 += d1;
                    varianzen[(int)bayerFilter].Diagonal1Count += 2;
                    var d2 = Math.Pow(centre - ImagingInstance.GetPixelValue(image, x - 1, y + 1), 2) + Math.Pow(centre - ImagingInstance.GetPixelValue(image, x + 1, y - 1), 2);
                    varianzen[(int)bayerFilter].Diagonal2 += d2;
                    varianzen[(int)bayerFilter].Diagonal2Count += 2;
                    varianzen[(int)bayerFilter].X += d1 + d2;
                    varianzen[(int)bayerFilter].XCount += 4;
                    varianzen[(int)bayerFilter].Cross += h + v;
                    varianzen[(int)bayerFilter].CrossCount += 4;
                    varianzen[(int)bayerFilter].Circle += h + v + d1 + d2;
                    varianzen[(int)bayerFilter].CircleCount += 8;
                }
            for (int i = 0; i <= 4; i++)
            {
                varianzen[i].Diagonal1 = (varianzen[i].Diagonal1) / varianzen[i].Diagonal1Count;
                varianzen[i].Diagonal2 = (varianzen[i].Diagonal2) / varianzen[i].Diagonal2Count;
                varianzen[i].Horizontal = (varianzen[i].Horizontal) / varianzen[i].HorizontalCount;
                varianzen[i].Vertical = (varianzen[i].Vertical) / varianzen[i].VerticalCount;
                varianzen[i].X = (varianzen[i].X) / varianzen[i].XCount;
                varianzen[i].Cross = (varianzen[i].Cross) / varianzen[i].CrossCount;
                varianzen[i].Circle = (varianzen[i].Circle) / varianzen[i].CircleCount;
            }
            return varianzen;
        }

        public static void DisposeBitmap(Bitmap image)
        {
            try
            {
                image?.Dispose();
                image = null;
            }
            catch
            { }
        }

        public static void DisposeImage(Image image)
        {
            try
            {
                image?.Dispose();
                image = null;
            }
            catch
            { }
        }

        internal static ConversionParameterSet ConversionParameterSet = new ConversionParameterSet(ImagingInstance.CurrentImageIsRGB,ImagingInstance.MaxGreyLevel);
        
        internal static void SetStrongPoint(BayerFilter bayerFilter, decimal originalValue, decimal newValue)
        {
            var existingStrongPoint = ConversionParameterSet.Sets[bayerFilter].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == originalValue);
            if (existingStrongPoint != null)
            {
                ConversionParameterSet.Sets[bayerFilter].StrongPoints.Remove(existingStrongPoint);
            }
            ConversionParameterSet.Sets[bayerFilter].StrongPoints.Add(new StrongPoint(bayerFilter, (ushort)originalValue, (ushort)newValue, false));
            ConversionParameterSet.Sets[bayerFilter].CreateConversionDictionary(ImagingInstance.MaxGreyLevel);
        }

        internal static void AddCurrentValuesToConversionParameters(ref ConversionParameterSet conversionParameterSet, ushort originalValueRed, ushort newValueRed, ushort originalValueGreen1, ushort newValueGreen1, ushort originalValueGreen2, ushort newValueGreen2, ushort originalValueBlue, ushort newValueBlue)
        {
            SetPreliminaryStrongPoint(ref conversionParameterSet, BayerFilter.red, originalValueRed, newValueRed);
            SetPreliminaryStrongPoint(ref conversionParameterSet, BayerFilter.green1, originalValueGreen1, newValueGreen1);
            SetPreliminaryStrongPoint(ref conversionParameterSet, BayerFilter.green2, originalValueGreen2, newValueGreen2);
            SetPreliminaryStrongPoint(ref conversionParameterSet, BayerFilter.blue, originalValueBlue, newValueBlue);
        }

        private static void SetPreliminaryStrongPoint(ref ConversionParameterSet conversionParameterSet, BayerFilter bayerFilter, ushort originalValue, ushort newValue)
        {
            var existingStrongPoint = conversionParameterSet.Sets[bayerFilter].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == originalValue);
            if (existingStrongPoint != null)
            {
                conversionParameterSet.Sets[bayerFilter].StrongPoints.Remove(existingStrongPoint);
            }
            conversionParameterSet.Sets[bayerFilter].StrongPoints.Add(new StrongPoint(bayerFilter,(ushort)originalValue, (ushort)newValue, true));
            conversionParameterSet.Sets[bayerFilter].CreateConversionDictionary(ImagingInstance.MaxGreyLevel);
        }

        public static string RectangleToString(Rectangle rectangle)
        {
            return String.Format("({0},{1})|({2},{3})", rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }
    }
}
