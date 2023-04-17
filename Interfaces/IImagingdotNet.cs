using Antibayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace AntiBeyer.Interfaces
{
    internal class IImagingdotNet : IImaging
    {
        private bool IsRGB;

        public BayerType BayerType { get; set; } = BayerType.None;
        public Bitmap CurrentImage { get; set; }
        public string StatusMessage { get; set; }
        public string Filename { get;  set; }
        public Bitmap PreviewImage { get;  set; }
        public Rectangle PreviewRectangle { get;  set; }
        public ushort MaxGreyLevel { get;  set; }

        public bool CurrentImageIsRGB
        {
            get
            {
                return IsRGB;
            }
        }

        public Bitmap ApplyConversion(ConversionParameterSet conversionParameterSet, Random randomizer)
        {
            return ApplyConversion(CurrentImage, conversionParameterSet, randomizer);
        }

        public Bitmap ApplyConversion(Bitmap image, ConversionParameterSet conversionParameterSet, Random randomizer)
        {
            DebugTools.Log(DebugTools.GetCallingMethod());
            if (image == null)
            {
                StatusMessage = "Trying to convert an empty image.";
                return null;
            }
            if (conversionParameterSet == null)
            {
                StatusMessage = "Missing conversion parameters.";
                return null;
            }
            if (BayerType != BayerType.None && conversionParameterSet.Sets.Count<4)
            {
                StatusMessage = "Missing conversion parameters for bayer filter(s):";
                if (!conversionParameterSet.Sets.ContainsKey(BayerFilter.red)) StatusMessage += " red";
                if (!conversionParameterSet.Sets.ContainsKey(BayerFilter.green1)) StatusMessage += " green1";
                if (!conversionParameterSet.Sets.ContainsKey(BayerFilter.green2)) StatusMessage += " green2";
                if (!conversionParameterSet.Sets.ContainsKey(BayerFilter.blue)) StatusMessage += " blue";
                return null;
            }
            else
            {
                if (conversionParameterSet.Sets.ContainsKey(BayerFilter.None))
                {
                    StatusMessage = "Missing conversion parameters for bayer filter 'None'";
                    return null;
                }
            }
            if (BayerType!=BayerType.None)
            {
                if (conversionParameterSet.Sets[BayerFilter.red].ConversionDictionary == null)
                {
                    conversionParameterSet.Sets[BayerFilter.red].CreateConversionDictionary();
                }
                if (conversionParameterSet.Sets[BayerFilter.green1].ConversionDictionary == null)
                {
                    conversionParameterSet.Sets[BayerFilter.green1].CreateConversionDictionary();
                }
                if (conversionParameterSet.Sets[BayerFilter.green2].ConversionDictionary == null)
                {
                    conversionParameterSet.Sets[BayerFilter.green2].CreateConversionDictionary();
                }
                if (conversionParameterSet.Sets[BayerFilter.blue].ConversionDictionary == null)
                {
                    conversionParameterSet.Sets[BayerFilter.blue].CreateConversionDictionary();
                }
            }
            else
            {
                if (conversionParameterSet.Sets.ContainsKey(BayerFilter.None))
                {
                    if (conversionParameterSet.Sets[BayerFilter.None].ConversionDictionary == null)
                    {
                        conversionParameterSet.Sets[BayerFilter.None].CreateConversionDictionary();
                    }
                }
            }
            var convertedImage = new Bitmap(image.Width, image.Height, image.PixelFormat);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    var pixelValue = GetPixelValue(image, x, y);
                    var bayerFilter = GetBayerFilter(x, y); // previews must not change the bayer pattern, thus can only have even coordinates!!!
                    //The randomizer only becomes useful when converting multiple images. It will turn the fractional part of the grey level conversion the probability of raising the graylevel by one.
                    //The noise generated that way will sum up into a matching grey level in the final image.
                    //Note: it shouldn't be possible to create a grey level beyond the saturation level of each image format. So the fractional part of e.g. 255 will alway be 0.
                    //That's why the comparison operator between the random value and the fractional is < and not <=!
                    var hit = randomizer == null ? 0 : randomizer.NextDouble() < conversionParameterSet.Sets[bayerFilter].ConversionFrationDictionary[pixelValue] ? 1u : 0u;
                    try
                    {
                        SetPixelValue(convertedImage, x, y, (ushort)(conversionParameterSet.Sets[bayerFilter].ConversionDictionary[pixelValue] + hit));
                    }
                    catch(Exception ex)
                    {
                        var contains = conversionParameterSet.Sets[bayerFilter]?.ConversionDictionary.ContainsKey(pixelValue) == true;
                        DebugTools.Log(StatusMessage = "Failed to convert (" + x + "," + y + ") with value " + pixelValue + ". Dictionary.Contains=" + contains + (contains? " targetValue="+ conversionParameterSet.Sets[bayerFilter]?.ConversionDictionary[pixelValue] : String.Empty));
                        return null;
                    }
                }
            return convertedImage;
        }

        public void SetPixelValue(Bitmap image, int x, int y, ushort value)
        {
            //As long as we don't do anything rash we can leave all the parameters unchecked.
            image.SetPixel(x, y, Color.FromArgb(value, value, value));
        }

        public BayerFilter GetBayerFilter(int x, int y)
        {

            int row = y % 2;
            int column = x % 2;
             var pixelIndex = row * 2 + column;
            switch (BayerType)
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

        public (int, int, PixelFormat, BayerType) GetImageInfo()
        {
            return CurrentImage != null ? (CurrentImage.Width, CurrentImage.Height, ConvertPixelFormat(CurrentImage.PixelFormat), BayerType) : (-1, -1, PixelFormat.Invalid, BayerType.None);
        }

        public (ushort, ushort, ushort) GetPixel(int x, int y)
        {
            if (CurrentImage == null) return (0, 0, 0);
            var pixel = CurrentImage.GetPixel(x, y);
            return (pixel.R, pixel.G, pixel.B);
        }


        public ushort GetPixelValue(Bitmap image, int x, int y)
        {
            switch (GetBayerFilter(x, y))
            {
                case BayerFilter.green1: return image.GetPixel(x, y).G;
                case BayerFilter.green2: return image.GetPixel(x, y).G;
                case BayerFilter.red: return image.GetPixel(x, y).R;
                case BayerFilter.blue: return image.GetPixel(x, y).B;
                default: return (ushort)(image.GetPixel(x, y).GetBrightness() * MaxGreyLevel);
            }
        }

        public ushort GetPixelValue(int x, int y)
        {
         return GetPixelValue(CurrentImage, x, y);
        }

        public BayerPixel GetBayerPixel(int x, int y)
        {
            var bayerFilter = GetBayerFilter(x, y);
            switch (bayerFilter)
            {
                case BayerFilter.green1: return new BayerPixel(CurrentImage.GetPixel(x, y).G, bayerFilter);
                case BayerFilter.green2: return new BayerPixel(CurrentImage.GetPixel(x, y).G, bayerFilter);
                case BayerFilter.red: return new BayerPixel(CurrentImage.GetPixel(x, y).R, bayerFilter);
                case BayerFilter.blue: return new BayerPixel(CurrentImage.GetPixel(x, y).B, bayerFilter);
                default: return new BayerPixel((ushort)(CurrentImage.GetPixel(x, y).GetBrightness() * MaxGreyLevel), bayerFilter);
            }
        }

        public BayerPixel GetBayerPixel(Bitmap image, int x, int y)
        {
            var bayerFilter = GetBayerFilter(x, y); //Always cut previews with even pixel coordinates to stick to the original bayer pattern!
            switch (bayerFilter)
            {
                case BayerFilter.green1: return new BayerPixel(image.GetPixel(x, y).G, bayerFilter);
                case BayerFilter.green2: return new BayerPixel(image.GetPixel(x, y).G, bayerFilter);
                case BayerFilter.red: return new BayerPixel(image.GetPixel(x, y).R, bayerFilter);
                case BayerFilter.blue: return new BayerPixel(image.GetPixel(x, y).B, bayerFilter);
                default: return new BayerPixel((ushort)(image.GetPixel(x, y).GetBrightness() * MaxGreyLevel), bayerFilter);
            }
        }

        /// <summary>
        /// Always cut previews with even pixel coordinates to stick to the original bayer pattern!
        /// </summary>
        /// <param name="rectangle"></param> Make sure that x und y are a multiple of 2!
        /// <param name="markBayer"></param> 
        /// <param name="applyCorrection"></param>
        /// <returns></returns>
        public Bitmap GetPreview(Rectangle rectangle, BayerFilter markBayer, ConversionParameterSet conversionParameterSet)
        {
            if (CurrentImage == null) return null;
            PreviewRectangle = rectangle;

            DisposeBitmap(PreviewImage);
            try
            {
                if (markBayer == BayerFilter.None)
                    return PreviewImage = ApplyConversion(new Bitmap(CurrentImage.Clone(PreviewRectangle, CurrentImage.PixelFormat)), conversionParameterSet, null);
                return PreviewImage = MarkBayerFilters(new Bitmap(CurrentImage.Clone(PreviewRectangle, CurrentImage.PixelFormat)), markBayer);
            }
            catch(Exception ex)
            {
                StatusMessage = ex.Message;
                return null;
            }
        }

        public Bitmap GetPreviewAroundPoint(Point imageLocation, Size previewSize, int zoomFactor)
        {
            if (imageLocation.X + (previewSize.Width / 2) >= CurrentImage.Width)
            {
                imageLocation.X = CurrentImage.Width - previewSize.Width / 2 - 4;
            }
            if (imageLocation.Y + (previewSize.Height / 2) >= CurrentImage.Height)
            {
                imageLocation.Y = CurrentImage.Height - previewSize.Height / 2 - 4;
            }
            if (imageLocation.X - (previewSize.Width / 2) < 8)
            {
                imageLocation.X = previewSize.Width / 2 + 2;
            }
            if (imageLocation.Y - (previewSize.Height / 2) < 8)
            {
                imageLocation.Y = previewSize.Height / 2 + 2;
            }

            imageLocation.X = imageLocation.X / 4 * 4;
            imageLocation.Y = imageLocation.Y / 4 * 4;

            PreviewRectangle = new Rectangle(imageLocation.X - (previewSize.Width / 2), imageLocation.Y - (previewSize.Height / 2), previewSize.Width, previewSize.Height);
            

            StatusMessage = "PreviewRectangle=(" + PreviewRectangle.X + "," + PreviewRectangle.Y + ")-(" + PreviewRectangle.Right + "," + PreviewRectangle.Bottom + ")"
                + " CurrentImage=(" + CurrentImage.Width + "," + CurrentImage.Height + ")";

            DisposeBitmap(PreviewImage);

            return PreviewImage = new Bitmap(ResizeImage(CurrentImage.Clone(PreviewRectangle, CurrentImage.PixelFormat), previewSize.Height, previewSize.Width, InterpolationMode.NearestNeighbor));

        }

        public static Image ResizeImage(Image image, int new_height, int new_width, InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
        {
            Bitmap new_image = new Bitmap(new_width, new_height);
            Graphics g = Graphics.FromImage((Image)new_image);
            g.InterpolationMode = interpolationMode;
            g.DrawImage(image, 0, 0, new_width, new_height);
            return new_image;
        }

        public (bool, int, int, PixelFormat) LoadImage(string file)
        {
            try
            {
                DisposeBitmap(CurrentImage);
                CurrentImage = new Bitmap(file);
                Filename = file;
                if (CurrentImage != null) SetImageProperties(CurrentImage.PixelFormat); else MaxGreyLevel = 255;
                return (true, CurrentImage.Width, CurrentImage.Height,ConvertPixelFormat(CurrentImage.PixelFormat));
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                return (false, -1, -1, PixelFormat.Invalid);
            }
        }

        private void SetImageProperties(System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            switch (System.Drawing.Image.GetPixelFormatSize(pixelFormat))
            {
                case 8: MaxGreyLevel = 255; IsRGB = false; break;
                case 16: MaxGreyLevel = 65535; IsRGB = false; break;
                case 32: MaxGreyLevel = 255; IsRGB = true; break;
                case 48: MaxGreyLevel = 65535; IsRGB = true; break;
                case 64: MaxGreyLevel = 65535; IsRGB = true; break;
                default: MaxGreyLevel = 255; IsRGB = true; break;
            }
        }

        public void SetBayerType(BayerType bayerType)
        {
            BayerType = bayerType;
        }

        private void DisposeBitmap(Bitmap image)
        {
            try
            {
                image?.Dispose();
                image = null;
            }
            catch
            { }
        }

        private void DisposeImage(Image image)
        {
            try
            {
                image?.Dispose();
                image = null;
            }
            catch
            { }
        }

        private PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Undefined: return PixelFormat.Invalid;
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb: return PixelFormat.Color8bpp;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb: return PixelFormat.Color8bpp;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb: return PixelFormat.Color8bpp;
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed: return PixelFormat.Greyscale8bpp;
                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale: return PixelFormat.Greyscale16bpp;
                case System.Drawing.Imaging.PixelFormat.Format48bppRgb: return PixelFormat.Color16bpp;
                case System.Drawing.Imaging.PixelFormat.Format64bppArgb: return PixelFormat.Color16bpp;
                default:return PixelFormat.Invalid;
            }
        }

        private static int RaisedLevel(int level)
        {
            return Math.Min(255, level + 10);
        }

        public Bitmap MarkBayerFilters(Bitmap bitmap, BayerFilter bayerFilter)
        {
            if (bitmap == null) return null;
            for(int y=0;y<bitmap.Height;y++)
                for(int x=0;x<bitmap.Width;x++)
                {
                    var bayerPixel=GetBayerPixel(bitmap,x,y);
                    Color color;
                    if (bayerPixel.BayerFilter == BayerFilter.red && bayerPixel.BayerFilter == bayerFilter) color = Color.FromArgb(255, RaisedLevel(bayerPixel.Value), bayerPixel.Value / 2, bayerPixel.Value / 2);
                    else
                    if (bayerPixel.BayerFilter == BayerFilter.green1 && bayerPixel.BayerFilter == bayerFilter) color = Color.FromArgb(255, bayerPixel.Value / 2, RaisedLevel(bayerPixel.Value), bayerPixel.Value / 2);
                    else
                    if (bayerPixel.BayerFilter == BayerFilter.green2 && bayerPixel.BayerFilter == bayerFilter) color = Color.FromArgb(255, bayerPixel.Value / 2, RaisedLevel(bayerPixel.Value), bayerPixel.Value / 2);
                    else
                    if (bayerPixel.BayerFilter == BayerFilter.blue && bayerPixel.BayerFilter == bayerFilter) color = Color.FromArgb(255, bayerPixel.Value / 2, bayerPixel.Value / 2, RaisedLevel(bayerPixel.Value));
                    else
                        color = Color.FromArgb(255, bayerPixel.Value, bayerPixel.Value, bayerPixel.Value);
                    bitmap.SetPixel(x, y, color);
                }
            return bitmap;
        }

        
    }
}
