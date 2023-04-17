using Antibayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AntiBeyer
{
    public enum PixelFormat { Invalid, Greyscale8bpp, Color8bpp, Greyscale16bpp, Color16bpp }

    public interface IImaging
    {
        string StatusMessage { get; set; }
        Bitmap CurrentImage { get; set; }
        BayerType BayerType { get; set; }
        string Filename { get;  set; }
        Bitmap PreviewImage { get;  set; }
        Rectangle PreviewRectangle { get;  set; }
        ushort MaxGreyLevel { get;  set; }
        bool CurrentImageIsRGB { get; }

        (ushort, ushort, ushort) GetPixel(int x, int y);
        
        ushort GetPixelValue(int x, int y);
        
        ushort GetPixelValue(Bitmap image, int x, int y);
        
        void SetPixelValue(Bitmap image, int x, int y, ushort value);

        BayerFilter GetBayerFilter(int x, int y);
        
        (bool, int, int, PixelFormat) LoadImage(string file);

        Bitmap ApplyConversion(ConversionParameterSet conversionParameterSet, Random randomizer);
        
        Bitmap ApplyConversion(Bitmap image, ConversionParameterSet conversionParameterSet, Random randomizer);

        Bitmap GetPreview(Rectangle rectangle, BayerFilter markBayer, ConversionParameterSet conversionParameterSet);
        
        Bitmap GetPreviewAroundPoint(Point clickPoint, Size previewSize, int zoomFactor);

        (int, int, PixelFormat,BayerType) GetImageInfo();
        
        void SetBayerType(BayerType bayerType);
        
        BayerPixel GetBayerPixel(int x, int y);
        
        Bitmap MarkBayerFilters(Bitmap bitmap, BayerFilter bayerFilter);
    }

    
}
