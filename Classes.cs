using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Antibayer
{
    public enum BayerFilter
    {
        red = 0, green1 = 1, green2 = 2, blue = 3,
        None = 4
    }

    public enum BayerType { None, RGGB, GRGB, GBRG, BGGR }

    public class HistogramSettings
    {
        public HistogramSettings()
        { }

        public HistogramSettings(int shiftR, int shiftG1, int shiftG2, int shiftB,double multiplyR, double multiplyG1, double multiplyG2,double multiplyB,BayerType bayerType)
        {
            ShiftR = shiftR;
            ShiftB = shiftB;
            ShiftG1 = shiftG1;
            ShiftG2 = shiftG2;
            MultiplyR=multiplyR;
            MultiplyG1 = multiplyG1;
            MultiplyG2 = multiplyG2;
            MultiplyB = multiplyB;
            BayerType = bayerType;
        }

        public int ShiftR { get; set; } = 0;
        public int ShiftG1 { get; set; } = 0;
        public int ShiftG2 { get; set; } = 0;
        public int ShiftB { get; set; } = 0;

        public double MultiplyR { get; set; } = 1d;
        public double MultiplyG1 { get; set; } = 1d;
        public double MultiplyG2 { get; set; } = 1d;
        public double MultiplyB { get; set; } = 1d;

        public BayerType BayerType { get; set; } = BayerType.None;
    }

    public enum ImageStatus { Original,Processed}
    public class Varianzen
    {
        public Varianzen(BayerType color)
        {
            Color = color;
        }
        public BayerType Color;
        public ImageStatus ImageStatus;
        public double Horizontal;
        public long HorizontalCount;
        public double Vertical;
        public long VerticalCount;
        public double Diagonal1;
        public long Diagonal1Count;
        public double Diagonal2;
        public double Diagonal2Count;
        public double Cross;
        public long CrossCount;
        public double X;
        public long XCount;
        public double Circle;
        public long CircleCount;
    }

    public class BayerPixel
    {
        public BayerPixel(ushort value, BayerFilter bayerFilter)
        {
            Value = value;
            BayerFilter = bayerFilter;
        }

        public BayerFilter BayerFilter;
        public ushort Value;
    }
}
