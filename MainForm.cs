using AntiBeyer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Antibayer
{

    public partial class MainForm : Form
    {
        private Image Image;
        private Image ZoomedImage;

        public string Filename { get; private set; }

        private long[] RGBHistogram8;
        private long[] RGBHistogram16;
        private long[] RHistogram8;
        private long[] RHistogram16;
        private long[] GHistogram8;
        private long[] G2Histogram8;
        private long[] GHistogram16;
        private long[] G2Histogram16;
        private long[] BHistogram8;
        private long[] BHistogram16;
        private long TotalMaxValue;
        private bool StopEvents;

        public long RMaxValue { get; private set; }
        public long GMaxValue { get; private set; }
        public long G2MaxValue { get; private set; }
        public long BMaxValue { get; private set; }
        public long RGBMaxValue { get; private set; }
        public Bitmap RGBHistogram8Image { get; private set; }
        public Bitmap RHistogram8Image { get; private set; }
        public Bitmap GHistogram8Image { get; private set; }
        public Bitmap G2Histogram8Image { get; private set; }
        public Bitmap BHistogram8Image { get; private set; }
        public BayerType bayerType { get; private set; }
        public Rectangle PreviewRectangle { get; private set; }
        public Bitmap PreviewImage { get; private set; }
        public Bitmap ProcessedPreview { get; private set; }
        public Configuration Configuration { get; set; } = new Configuration();
        public Varianzen[] Variances { get; private set; }

        public MainForm()
        {
            InitializeComponent();
            PlaceHistograms();
            Configuration = Configuration.GetConfig();
            SetConfiguration();
        }

        private void SetConfiguration()
        {
            if (Configuration == null) return;
            if (Configuration.HistogramSettings != null)
            {
                //textBoxOriginalValueRed.Value = Configuration.HistogramSettings.ShiftR;
                //textBoxOriginalValueGreen1.Value = Configuration.HistogramSettings.ShiftG1;
                //textBoxOriginalValueGreen2.Value = Configuration.HistogramSettings.ShiftG2;
                //textBoxOriginalValueBlue.Value = Configuration.HistogramSettings.ShiftB;
                //textBoxNewValueRed.Value = (decimal)Configuration.HistogramSettings.MultiplyR;
                //textBoxNewValueGreen1.Value = (decimal)Configuration.HistogramSettings.MultiplyG1;
                //textBoxNewValueGreen2.Value = (decimal)Configuration.HistogramSettings.MultiplyG2;
                //textBoxNewValueBlue.Value = (decimal)Configuration.HistogramSettings.MultiplyB;
                SetBayerType(Configuration.HistogramSettings.BayerType);
            }
        }

        private void SetBayerType(BayerType bayerType)
        {
            switch (bayerType)
            {
                case BayerType.RGGB: radioButtonBayerRGGB.Checked = true; return;
                case BayerType.GBRG: radioButtonBayerGBRG.Checked = true; return;
                case BayerType.GRGB: radioButtonBayerGRGB.Checked = true; return;
                case BayerType.BGGR: radioButtonBayerBGGR.Checked = true; return;
                case BayerType.None: radioButtonbayerNone.Checked = true; return;
            }
        }


        /// <summary>
        /// Sony IMX 462
        /// RG
        /// GB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void pictureBoxImagePreview_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
            /*if (e.Data.GetDataPresent(Text))
            {
                e.Effect = DragDropEffects.Copy;
            }*/
        }

        private void pictureBoxImagePreview_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var file = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (file?[0] != null && File.Exists(file?[0]))
                {
                    Image?.Dispose();
                    Image = Bitmap.FromFile(file[0]);
                    ZoomedImage?.Dispose();
                    ZoomedImage = new Bitmap(Image, Image.Width * 5, Image.Height * 5);
                    if (Image != null)
                    {
                        _pbPreview.Image = ZoomedImage;
                        _pbWorkingImage.Image = Image;
                    }
                }
            }
        }

        private void _pbPreview_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void buttonLoadImage_Click(object sender, EventArgs e)
        {
            var fileOpenDialog = new OpenFileDialog();
            fileOpenDialog.CheckFileExists = true;
            fileOpenDialog.CheckPathExists = true;
            fileOpenDialog.Multiselect = false;
            fileOpenDialog.Title = "Open image file...";
            fileOpenDialog.DefaultExt = "*.png";
            fileOpenDialog.ValidateNames = true;
            if (fileOpenDialog.ShowDialog() == DialogResult.Cancel) return;
            var file = fileOpenDialog.FileName;            
            if (!File.Exists(file)) return;
            Filename = file;
            ImageFunctions.ImagingInstance.LoadImage(file);
            ImageFunctions.ConversionParameterSet = new ConversionParameterSet(ImageFunctions.ImagingInstance.CurrentImageIsRGB, ImageFunctions.ImagingInstance.MaxGreyLevel);
            textBoxStatus.Text = ImageFunctions.ImagingInstance.StatusMessage;

            _pbWorkingImage.Image = ImageFunctions.ImagingInstance.CurrentImage;
            _pbPreview.Image?.Dispose();
            CreateHistograms();
        }



        private void CreateHistograms()
        {
            if (ImageFunctions.ImagingInstance.CurrentImage == null) return;
            RGBHistogram8 = new long[256];
            RGBHistogram16 = new long[65536];
            RHistogram8 = new long[256];
            RHistogram16 = new long[65536];
            GHistogram8 = new long[256];
            GHistogram16 = new long[65536];
            G2Histogram8 = new long[256];
            G2Histogram16 = new long[65536];
            BHistogram8 = new long[256];
            BHistogram16 = new long[65536];
            RGBMaxValue = 0;
            RMaxValue = 0;
            GMaxValue = 0;
            G2MaxValue = 0;
            BMaxValue = 0;

            for (int x = 0; x < ImageFunctions.ImagingInstance.CurrentImage.Width; x++)
                for (int y = 0; y < ImageFunctions.ImagingInstance.CurrentImage.Height; y++)
                {
                    var pixel = ImageFunctions.ImagingInstance.CurrentImage.GetPixel(x, y);
                    if (bayerType == BayerType.None)
                    {
                        var b = (int)(255 * pixel.GetBrightness());
                        RGBHistogram8[b]++;
                        RHistogram8[pixel.R]++;
                        GHistogram8[pixel.G]++;
                        BHistogram8[pixel.B]++;
                        if (RHistogram8[b] > RGBMaxValue) { RGBMaxValue = RGBHistogram8[b]; }
                        if (RHistogram8[pixel.R] > RMaxValue) { RMaxValue = RHistogram8[pixel.R]; }
                        if (GHistogram8[pixel.G] > GMaxValue) { GMaxValue = GHistogram8[pixel.G]; }
                        if (BHistogram8[pixel.B] > BMaxValue) { BMaxValue = BHistogram8[pixel.B]; }
                    }
                    else
                    {
                        var pixelType = ImageFunctions.ImagingInstance.GetBayerFilter(x, y);
                        switch (pixelType)
                        {
                            case BayerFilter.red:
                                RHistogram8[pixel.R]++;
                                if (RHistogram8[pixel.R] > RMaxValue) { RMaxValue = RHistogram8[pixel.R]; }
                                break;
                            case BayerFilter.blue:
                                BHistogram8[pixel.B]++;
                                if (BHistogram8[pixel.B] > BMaxValue) { BMaxValue = BHistogram8[pixel.B]; }
                                break;
                            case BayerFilter.green1:
                                GHistogram8[pixel.G]++;
                                if (GHistogram8[pixel.G] > GMaxValue) { GMaxValue = GHistogram8[pixel.G]; }
                                break;
                            case BayerFilter.green2:
                                G2Histogram8[pixel.G]++;
                                if (G2Histogram8[pixel.G] > G2MaxValue) { G2MaxValue = G2Histogram8[pixel.G]; }
                                break;
                        }
                    }
                }
            TotalMaxValue = RMaxValue;
            if (GMaxValue > TotalMaxValue) TotalMaxValue = GMaxValue;
            if (BMaxValue > TotalMaxValue) TotalMaxValue = BMaxValue;
            DrawHistograms();
        }

        private void DrawHistograms()
        {
            if (ImageFunctions.ImagingInstance.CurrentImage == null) return;
            ImageFunctions.DisposeBitmap(RGBHistogram8Image);

            RGBHistogram8Image = CreateRGBHistogram8(RHistogram8, GHistogram8, G2Histogram8, BHistogram8, TotalMaxValue, _pbHistoRGB.Width, _pbHistoRGB.Height);
            _pbHistoRGB.Image = RGBHistogram8Image;
            if (!checkBoxOnlyRGBHistogram.Checked)
            {
                RHistogram8Image = CreateHistogram8(RHistogram8, RMaxValue, (int)textBoxOriginalValueRed.Value, (double)textBoxNewValueRed.Value, _pbHistoR.Width, _pbHistoR.Height, 255, 80, 80);
                GHistogram8Image = CreateHistogram8(GHistogram8, GMaxValue, (int)textBoxOriginalValueGreen1.Value, (double)textBoxNewValueGreen1.Value, _pbHistoG.Width, _pbHistoG.Height, 0, 255, 0);
                G2Histogram8Image = CreateHistogram8(G2Histogram8, GMaxValue, (int)textBoxOriginalValueGreen2.Value, (double)textBoxNewValueGreen2.Value, _pbHistoG.Width, _pbHistoG.Height, 0, 255, 0);
                BHistogram8Image = CreateHistogram8(BHistogram8, BMaxValue, (int)textBoxOriginalValueBlue.Value, (double)textBoxNewValueBlue.Value, _pbHistoB.Width, _pbHistoB.Height, 80, 80, 255);
                _pbHistoR.Image = RHistogram8Image;
                _pbHistoG.Image = GHistogram8Image;
                _pbHistoB.Image = BHistogram8Image;
            }
        }

        private Bitmap CreateRGBHistogram8(long[] histogram8R, long[] histogram8G, long[] histogram8G2, long[] histogram8B, long maxValue, int width, int height)
        {
            var conversionParameters = ImageFunctions.ConversionParameterSet;

            var histogram8RS = ScaleHistogram(histogram8R, conversionParameters, BayerFilter.red);
            var histogram8GS = ScaleHistogram(histogram8G, conversionParameters, BayerFilter.green1);
            var histogram8G2S = ScaleHistogram(histogram8G2, conversionParameters, BayerFilter.green2);
            var histogram8BS = ScaleHistogram(histogram8B, conversionParameters, BayerFilter.blue);

            if (width <= 0 || height <= 0) return new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(image))
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                {
                    g.FillRectangle(b, 0, 0, width, height);
                }
                double scale = maxValue != 0 ? maxValue / (height - 10) : 1;
                using (Pen red = new Pen(Color.FromArgb(255, 80, 80)))
                using (Pen green = new Pen(Color.FromArgb(0, 255, 0)))
                using (Pen blue = new Pen(Color.FromArgb(80, 80, 255)))
                using (Pen white = new Pen(Color.FromArgb(255, 255, 255)))
                using (Pen pink = new Pen(Color.FromArgb(255, 0, 255)))
                using (Pen yellow = new Pen(Color.FromArgb(255, 255, 0)))
                using (Pen turquoise = new Pen(Color.FromArgb(0, 255, 255)))
                {
                    var scaleX = width / 512;
                    scaleX = Math.Max(scaleX, 1);
                    for (int i = 0; i < 256; i++)
                    {
                        double r = histogram8R?[i] ?? 0;
                        double gr = histogram8G?[i] ?? 0;
                        double b = histogram8B?[i] ?? 0;

                        double rs = histogram8RS?[i] ?? 0;
                        double grs = histogram8GS?[i] ?? 0;
                        double bs = histogram8BS?[i] ?? 0;
                        for (int j = 0; j < scaleX; j++)
                        {
                            DrawRGBLine(height, g, scale, red, green, blue, white, pink, yellow, turquoise, i * scaleX + j, r, gr, b);
                            DrawRGBLine(height, g, scale, red, green, blue, white, pink, yellow, turquoise, (i + 256) * scaleX + j + 5, rs, grs, bs);
                        }
                    }
                }
            }
            return image;
        }

        private long[] ScaleHistogram(long[] histogram, ConversionParameterSet conversionParameterSet, BayerFilter bayerFilter)
        {
            if (!conversionParameterSet.Sets.ContainsKey(bayerFilter)) return null;
            if (conversionParameterSet.Sets[bayerFilter]?.ConversionDictionary?.Count != histogram?.Length)
            {
                return null;
            }
            var conversionDictionary = conversionParameterSet.Sets[bayerFilter].ConversionDictionary;
            if (conversionDictionary == null) return null;
            var result = new long[conversionDictionary.Count];
            for (ushort i = 0; i < histogram.Length; i++)
            {
                result[conversionDictionary[i]] += histogram[i];
            }
            return result;
        }

        private static void DrawRGBLine(int height, Graphics g, double scale, Pen red, Pen green, Pen blue, Pen white, Pen pink, Pen yellow, Pen turquoise, int i, double r, double gr, double b)
        {
            //R
            if (r <= gr && r <= b && b <= gr)
            {
                g.DrawLine(green, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
                g.DrawLine(turquoise, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
            }
            //BGR
            if (r <= gr && r <= b && gr <= b)
            {
                g.DrawLine(blue, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
                g.DrawLine(turquoise, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
            }
            //GRB
            if (b <= gr && b <= r && r <= gr)
            {
                g.DrawLine(green, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
                g.DrawLine(yellow, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
            }
            //RGB
            if (b <= gr && b <= r && gr <= r)
            {
                g.DrawLine(red, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
                g.DrawLine(yellow, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
            }
            //RBG
            if (gr <= r && gr <= b && b <= r)
            {
                g.DrawLine(red, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
                g.DrawLine(pink, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
            }
            //BRG
            if (gr <= r && gr <= b && r <= b)
            {
                g.DrawLine(blue, i + 10, height - 5, i + 10, (int)(height - 5 - (double)b / scale));
                g.DrawLine(pink, i + 10, height - 5, i + 10, (int)(height - 5 - (double)r / scale));
                g.DrawLine(white, i + 10, height - 5, i + 10, (int)(height - 5 - (double)gr / scale));
            }
        }

        private Bitmap CreateHistogram8(long[] histogram8, long maxValue, int shift, double scale, int width, int height, byte r, byte g, byte b)
        {
            if (Image == null) return null;
            var image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            long[] scaledHistogram8 = ScaleHistogram(histogram8, shift, scale);

            using (Graphics gr = Graphics.FromImage(image))
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                {
                    gr.FillRectangle(br, 0, 0, width, height);
                }
                double hScale = maxValue != 0 ? maxValue / (height - 10) : 1;
                using (Pen p = new Pen(Color.FromArgb(r, g, b)))
                {
                    var scaleX = width / 512;
                    scaleX = Math.Max(scaleX, 1);
                    for (int i = 0; i < 256; i++)
                    {
                        for (int j = 0; j < scaleX; j++)
                        {
                            gr.DrawLine(p, i * scaleX + j + 10, height - 5, i * scaleX + j + 10, (int)(height - 5 - (double)(histogram8?[i] ?? 0) / hScale));
                            gr.DrawLine(p, 256 * (scaleX + i) + j + 15, height - 5, 256 * (scaleX + i) + j + 15, (int)(height - 5 - (double)(scaledHistogram8?[i] ?? 0) / hScale));
                        }
                    }
                }
            }
            return image;
        }

        private static long[] ScaleHistogram(long[] histogram8, int shift, double scale)
        {
            if (histogram8 == null) return null;
            var scaledHistogram8 = new long[256];
            for (int i = 0; i < histogram8.Length; i++)
            {
                var j = Math.Max(0, Math.Min(255, (int)((double)i * scale) + shift));
                scaledHistogram8[j] += histogram8[i];
            }
            return scaledHistogram8;
        }

        private void PlaceHistograms()
        {
            var hTop = _pbWorkingImage.Bottom + 5;
            var hBottom = textBoxStatus.Top - 5;
            var hLeft = _pbPreview.Right + 5;
            var hRight = textBoxStatus.Right;
            var hHeight = (hBottom - hTop - 15) / 4;
            var hWidth = hRight - hLeft;

            if (checkBoxOnlyRGBHistogram.Checked)
            {
                _pbHistoRGB.Location = new Point(hLeft, hTop);
                _pbHistoRGB.Size = new Size(hWidth, hBottom - hTop - 5);
                _pbHistoB.Location = _pbHistoG.Location = _pbHistoR.Location = new Point(0, 0);
                _pbHistoB.Size = _pbHistoG.Size = _pbHistoR.Size = new Size(0, 0);
                return;
            }

            _pbHistoRGB.Location = new Point(hLeft, hTop);
            _pbHistoRGB.Size = new Size(hWidth, hHeight);
            _pbHistoR.Location = new Point(hLeft, hTop + hHeight + 5);
            _pbHistoR.Size = new Size(hWidth, hHeight);
            _pbHistoG.Location = new Point(hLeft, hTop + hHeight * 2 + 10);
            _pbHistoG.Size = new Size(hWidth, hHeight);
            _pbHistoB.Location = new Point(hLeft, hTop + hHeight * 3 + 15);
            _pbHistoB.Size = new Size(hWidth, hHeight);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            PlaceHistograms();
            Invalidate();
            DrawHistograms();
        }

        private void checkBoxOnlyRGBHistogram_CheckedChanged(object sender, EventArgs e)
        {
            PlaceHistograms();
            Invalidate();
            DrawHistograms();
        }

        private void RadioButtonBayer_CheckedChanged(object sender, EventArgs e)
        {

            ImageFunctions.ImagingInstance.SetBayerType(GetBayerType());

            PlaceHistograms();
            Invalidate();
            CreateHistograms();
            DrawHistograms();
        }

        private void ShowImageData()
        {
            if (Variances == null) return;
            textBoxImageInfo.Text = "Variances (R|G1|G2|B):\r\n"
              + "H: " + GetDecimal(Variances[(int)BayerFilter.red].Horizontal) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Horizontal) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Horizontal) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Horizontal) + "\r\n"
              + "V: " + GetDecimal(Variances[(int)BayerFilter.red].Vertical) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Vertical) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Vertical) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Vertical) + "\r\n"
              + "D1: " + GetDecimal(Variances[(int)BayerFilter.red].Diagonal1) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Diagonal1) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Diagonal1) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Diagonal1) + "\r\n"
              + "D2: " + GetDecimal(Variances[(int)BayerFilter.red].Diagonal2) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Diagonal2) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Diagonal2) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Diagonal2) + "\r\n"
              + "C: " + GetDecimal(Variances[(int)BayerFilter.red].Cross) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Cross) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Cross) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Cross) + "\r\n"
              + "X: " + GetDecimal(Variances[(int)BayerFilter.red].X) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].X) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].X) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].X) + "\r\n"
              + "O: " + GetDecimal(Variances[(int)BayerFilter.red].Circle) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green1].Circle) + "   |   " + GetDecimal(Variances[(int)BayerFilter.green2].Circle) + "   |   " + GetDecimal(Variances[(int)BayerFilter.blue].Circle) + "\r\n";
        }

        private string GetDecimal(double value)
        {
            return value.ToString(".##");
        }

        private void HistogramValueChanged(object sender, EventArgs e)
        {
            if (StopEvents) return;
            var conversionParameters = ImageFunctions.ConversionParameterSet;
            ImageFunctions.AddCurrentValuesToConversionParameters(ref conversionParameters, (ushort)textBoxOriginalValueRed.Value, (ushort)textBoxNewValueRed.Value, (ushort)textBoxOriginalValueGreen1.Value, (ushort)textBoxNewValueGreen1.Value, (ushort)textBoxOriginalValueGreen2.Value, (ushort)textBoxNewValueGreen2.Value, (ushort)textBoxOriginalValueBlue.Value, (ushort)textBoxNewValueBlue.Value);
            //DrawHistograms(ref conversionParameters);
            ProcessPreviewImage(conversionParameters);
            FillListViewStrongPoints(conversionParameters);
        }

        private HistogramSettings GetHistogramSettings()
        {
            return new HistogramSettings((int)textBoxOriginalValueRed.Value, (int)textBoxOriginalValueGreen1.Value, (int)textBoxOriginalValueGreen2.Value, (int)textBoxOriginalValueBlue.Value,
                (double)textBoxNewValueRed.Value, (double)textBoxNewValueGreen1.Value, (double)textBoxNewValueGreen2.Value, (double)textBoxNewValueBlue.Value, GetBayerType());
        }

        private BayerType GetBayerType()
        {
            if (radioButtonBayerRGGB.Checked) return BayerType.RGGB;
            if (radioButtonBayerGRGB.Checked) return BayerType.GRGB;
            if (radioButtonbayerNone.Checked) return BayerType.None;
            return BayerType.None;
        }

        private void _pbWorkingImage_Click(object sender, EventArgs e)
        {
            var clickPoint = _pbWorkingImage.PointToClient(Cursor.Position);

            if (clickPoint == null) return;
            ShowPreviewAroundPoint(clickPoint);

        }

        private void _pbPreview_Click(object sender, EventArgs e)
        {
            var clickPoint = _pbPreview.PointToClient(Cursor.Position);
            var x = (PreviewRectangle.Width * (double)clickPoint.X / _pbPreview.Width) + .5d;
            var y = (PreviewRectangle.Height * (double)clickPoint.Y / _pbPreview.Height) + .5d;
            var clickedPreviewLocation = new Point((int)x, (int)y);
            var originalLocation = new Point(clickedPreviewLocation.X + PreviewRectangle.X, clickedPreviewLocation.Y + PreviewRectangle.Y);

            textBoxStatus.Text = "Preview-Click: (" + clickedPreviewLocation.X + "|" + clickedPreviewLocation.Y + ") => Original (" + originalLocation.X + "|" + originalLocation.Y + ")";


            //#if DEBUG
            //            var copy = new Bitmap(_pbWorkingImage.Image);
            //            _pbWorkingImage.Image.Dispose();
            //            copy.SetPixel(originalLocation.X,originalLocation.Y, Color.Red);
            //            _pbWorkingImage.Image = ImageFunctions.ImagingInstance.CurrentImage = copy;
            //#endif
            var clickedBayerFilter = ImageFunctions.ImagingInstance.GetBayerFilter(originalLocation.X, originalLocation.Y);
            labelRed.ForeColor = labelGreen1.ForeColor = labelGreen2.ForeColor = labelBlue.ForeColor = Color.Black;
            textBoxOriginalValueRed.BackColor = textBoxOriginalValueGreen1.BackColor = textBoxOriginalValueGreen2.BackColor = textBoxOriginalValueBlue.BackColor = Control.DefaultBackColor;
            foreach (BayerFilter bayerFilter in Enum.GetValues(typeof(BayerFilter)))
            {
                if (bayerFilter == clickedBayerFilter)
                {
                    switch (bayerFilter)
                    {
                        case BayerFilter.red: textBoxOriginalValueRed.BackColor = labelRed.ForeColor = Color.Red; break;
                        case BayerFilter.green1: textBoxOriginalValueGreen1.BackColor = Color.LightGreen; labelGreen1.ForeColor = Color.Green; break;
                        case BayerFilter.green2: textBoxOriginalValueGreen2.BackColor = Color.LightGreen; labelGreen2.ForeColor = Color.Green; break;
                        case BayerFilter.blue: textBoxOriginalValueBlue.BackColor = Color.LightBlue; labelBlue.ForeColor = Color.Blue; break;
                        case BayerFilter.None: break;
                    }
                }
            }


            var surroundingPixels = ImageFunctions.GetPixelsAroundPixel(ImageFunctions.ImagingInstance, originalLocation);
            ImageFunctions.ConversionParameterSet.RemovePreviewStrongPoints();
            FillListViewStrongPoints(ImageFunctions.ConversionParameterSet);
            FillValueConversionFields(surroundingPixels);
        }

        private void FillValueConversionFields(List<StrongPoint> strongPoints)
        {
            if (strongPoints == null) return;
            StopEvents = true;

            {
                foreach (var strongPoint in strongPoints)
                {
                    switch (strongPoint.BayerFilter)
                    {
                        case BayerFilter.None: break;
                        case BayerFilter.red: textBoxOriginalValueRed.Text = strongPoint.OriginalGreyValue.ToString(); textBoxNewValueRed.Text = strongPoint.NewGreyValue.ToString(); break;
                        case BayerFilter.green1: textBoxOriginalValueGreen1.Text = strongPoint.OriginalGreyValue.ToString(); textBoxNewValueGreen1.Text = strongPoint.NewGreyValue.ToString(); break;
                        case BayerFilter.green2: textBoxOriginalValueGreen2.Text = strongPoint.OriginalGreyValue.ToString(); textBoxNewValueGreen2.Text = strongPoint.NewGreyValue.ToString(); break;
                        case BayerFilter.blue: textBoxOriginalValueBlue.Text = strongPoint.OriginalGreyValue.ToString(); textBoxNewValueBlue.Text = strongPoint.NewGreyValue.ToString(); break;
                    }
                }
            }
            StopEvents = false;
        }

        private void FillValueConversionFields(List<BayerPixel> surroundingPixels)
        {
            int redSum = 0, redCount = 0, green1Sum = 0, green1Count = 0, green2Sum = 0, green2Count = 0, blueSum = 0, blueCount = 0;
            foreach (var pixel in surroundingPixels)
            {
                switch (pixel.BayerFilter)
                {
                    case BayerFilter.None: break;
                    case BayerFilter.red: redSum += pixel.Value; redCount++; break;
                    case BayerFilter.green1: green1Sum += pixel.Value; green1Count++; break;
                    case BayerFilter.green2: green2Sum += pixel.Value; green2Count++; break;
                    case BayerFilter.blue: blueSum += pixel.Value; blueCount++; break;
                }
            }
            StopEvents = true;
            var conversionParameterSet = ImageFunctions.ConversionParameterSet;
            ushort red = (ushort)(redSum / redCount);
            textBoxOriginalValueRed.Value = red;
            var strongPointRed = conversionParameterSet.Sets[BayerFilter.red].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == red);
            textBoxNewValueRed.Value = red;//strongPointRed != null ? strongPointRed.NewGreyValue : red;

            ushort green1 = (ushort)(green1Sum / green1Count);
            textBoxOriginalValueGreen1.Value = green1;
            var strongPointgreen1 = conversionParameterSet.Sets[BayerFilter.green1].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == green1);
            textBoxNewValueGreen1.Value = green1;//strongPointgreen1 != null ? strongPointgreen1.NewGreyValue : green1;

            ushort green2 = (ushort)(green2Sum / green2Count);
            textBoxOriginalValueGreen2.Value = green2;
            var strongPointgreen2 = conversionParameterSet.Sets[BayerFilter.green2].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == green2);
            textBoxNewValueGreen2.Value = green2;// strongPointgreen2 != null ? strongPointgreen2.NewGreyValue : green2;

            ushort blue = (ushort)(blueSum / blueCount);
            textBoxOriginalValueBlue.Value = blue;
            var strongPointBlue = conversionParameterSet.Sets[BayerFilter.blue].StrongPoints.FirstOrDefault(s => s.OriginalGreyValue == blue);
            textBoxNewValueBlue.Value = blue;// strongPointBlue != null ? strongPointBlue.NewGreyValue : blue;
            StopEvents = false;
        }

        private void ShowPreviewAroundPoint(Point clickPoint)
        {
            var zoomFactor = 8;
            var clickedImageLocation = new Point((int)(ImageFunctions.ImagingInstance.CurrentImage.Width * (double)clickPoint.X / _pbWorkingImage.Width), (int)(ImageFunctions.ImagingInstance.CurrentImage.Height * (double)clickPoint.Y / _pbWorkingImage.Height));

            var previewSize = new Size(_pbPreview.Width / zoomFactor, _pbPreview.Height / zoomFactor);
            if (clickedImageLocation.X + (previewSize.Width / 2) >= ImageFunctions.ImagingInstance.CurrentImage.Width)
            {
                clickedImageLocation.X = ImageFunctions.ImagingInstance.CurrentImage.Width - previewSize.Width / 2 - 4;
            }
            if (clickedImageLocation.Y + (previewSize.Height / 2) >= ImageFunctions.ImagingInstance.CurrentImage.Height)
            {
                clickedImageLocation.Y = ImageFunctions.ImagingInstance.CurrentImage.Height - previewSize.Height / 2 - 4;
            }
            if (clickedImageLocation.X - (previewSize.Width / 2) < 8)
            {
                clickedImageLocation.X = previewSize.Width / 2 + 2;
            }
            if (clickedImageLocation.Y - (previewSize.Height / 2) < 8)
            {
                clickedImageLocation.Y = previewSize.Height / 2 + 2;
            }

            clickedImageLocation.X = clickedImageLocation.X / 4 * 4;
            clickedImageLocation.Y = clickedImageLocation.Y / 4 * 4;

            PreviewRectangle = new Rectangle(clickedImageLocation.X - (previewSize.Width / 2), clickedImageLocation.Y - (previewSize.Height / 2), previewSize.Width, previewSize.Height);

            textBoxStatus.Text = "PreviewRectangle=(" + PreviewRectangle.X + "," + PreviewRectangle.Y + ")-(" + PreviewRectangle.Right + "," + PreviewRectangle.Bottom + ")"
                + " CurrentImage=(" + ImageFunctions.ImagingInstance.CurrentImage.Width + "," + ImageFunctions.ImagingInstance.CurrentImage.Height + ")";

            ImageFunctions.ConversionParameterSet.RemovePreviewStrongPoints();

            ProcessPreviewImage(ImageFunctions.ConversionParameterSet);

            _pbPreview.Image = ProcessedPreview;

            Variances = ImageFunctions.VarianceCalc(ProcessedPreview, GetBayerType());

            ShowImageData();
            ImageFunctions.ConversionParameterSet.RemovePreviewStrongPoints();
        }

        private void ProcessPreviewImage(ConversionParameterSet conversionParameterSet)
        {
            if (ImageFunctions.ImagingInstance.CurrentImage == null) return;
            ImageFunctions.DisposeBitmap(PreviewImage);
            ImageFunctions.DisposeImage(_pbPreview.Image);
            try
            {
                textBoxStatus.Text = "ProcessPreviewImage: PreviewRectangle=" + ImageFunctions.RectangleToString(PreviewRectangle);
                PreviewImage = ImageFunctions.ImagingInstance.GetPreview(PreviewRectangle, BayerFilter.None, ImageFunctions.ConversionParameterSet);

                //ProcessedPreview = ImageFunctions.ImagingInstance.ApplyConversion(PreviewImage, conversionParameterSet, null);//   ImageFunctions.ImagingInstance.GetPreview(PreviewRectangle, 8, BayerFilter.None, false);
                //if (ProcessedPreview == null) return;
                ProcessedPreview = ImageFunctions.ImagingInstance.GetPreview(PreviewRectangle, BayerFilter.None, conversionParameterSet);

                if (ProcessedPreview != null) _pbPreview.Image = ProcessedPreview;
            }
            catch (Exception ex)
            {
                textBoxStatus.Text += "-> " + ex.Message;
            }
        }

        private void _pbWorkingImage_MouseDown(object sender, MouseEventArgs e)
        {
            //var clickPoint = e.Location; 
            //ShowPreviewAroundPoint(clickPoint);
        }

        private void BayerSelectoButton_MouseDown(object sender, MouseEventArgs e)
        {
            var bayerFilter = BayerFilter.None;
            if (sender == buttonMarkR) bayerFilter = BayerFilter.red;
            if (sender == buttonMarkG1) bayerFilter = BayerFilter.green1;
            if (sender == buttonMarkG2) bayerFilter = BayerFilter.green2;
            if (sender == buttonMarkB) bayerFilter = BayerFilter.blue;
            if (sender == buttonViewSource)
            {
                _pbPreview.Image = PreviewImage;
                return;
            }
            textBoxStatus.Text = "MarkBayerFilter " + bayerFilter + ": PreviewRectangle=" + ImageFunctions.RectangleToString(PreviewRectangle);
            ImageFunctions.DisposeImage(_pbPreview.Image);
            _pbPreview.Image = ImageFunctions.ImagingInstance.GetPreview(PreviewRectangle, bayerFilter, ImageFunctions.ConversionParameterSet);
            //_pbPreview.Image = ImageFunctions.ImagingInstance.MarkBayerFilters(ImageFunctions.ImagingInstance.ApplyConversion(FuckingProcessedPreview, ImageFunctions.GetConversionParameters(), null), bayerFilter);
        }

        private void BayerSelectButton_MouseUp(object sender, MouseEventArgs e)
        {
            ImageFunctions.DisposeImage(_pbPreview.Image);
            _pbPreview.Image = ImageFunctions.ImagingInstance.GetPreview(PreviewRectangle, BayerFilter.None, ImageFunctions.ConversionParameterSet);

            //ProcessedPreview = ImageFunctions.ImagingInstance.MarkBayerFilters(ImageFunctions.ImagingInstance.ApplyConversion(FuckingProcessedPreview, ImageFunctions.GetConversionParameters(), null), BayerFilter.None);
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
            Close();
        }

        private void SaveConfiguration()
        {
            Configuration.LastFolder = Path.GetDirectoryName(Filename);
            Configuration.HistogramSettings = GetHistogramSettings();
            var result = Configuration.StoreConfig();
            if (result != null)
            {
                MessageBox.Show(result);
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (ImageFunctions.ImagingInstance.CurrentImage == null) return;
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = Path.GetExtension(Filename);
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(Filename);
            saveFileDialog.CheckFileExists = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImageFunctions.ImagingInstance.ApplyConversion(PreviewImage, ImageFunctions.ConversionParameterSet, null)?.Save(saveFileDialog.FileName);
            }
        }

        private void buttonConvertFolder_Click(object sender, EventArgs e)
        {
            GetHistogramSettings();
            buttonConvertFolder.Enabled = false;
            var folder = Path.GetDirectoryName(Filename);
            var folderBrowser = new FolderBrowserDialog();
            folderBrowser.SelectedPath = Path.GetFullPath(folder);
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                var folderName = folderBrowser.SelectedPath;
                var files = Directory.GetFiles(folderName);
                var randomizer = new Random(files.Count());
                foreach (var file in files)
                {
                    ConvertFile(file, folderName, textBoxFilenamePrefix.Text, textBoxFilenameSuffix.Text, randomizer);
                }
            }
            buttonConvertFolder.Enabled = true;
        }

        private string CreateFileName(string currentOriginalFile, string targetFolder, string prefix, string suffix)
        {
            var filename = Path.GetFileNameWithoutExtension(currentOriginalFile);
            var extension = Path.GetExtension(currentOriginalFile);
            var newFilename = Path.Combine(targetFolder, prefix + filename + suffix + extension);
            var count = 0;
            while (File.Exists(newFilename))
            {
                newFilename = Path.Combine(targetFolder, filename + count + extension);
                count++;
                if (count == 0)
                {
                    MessageBox.Show("Overflow in file numbering counter. Couldn't add a number to prevent overwriting the file.");
                    return null;
                }
            }
            return newFilename;
        }

        private void ConvertFile(string file, string targetFolder, string prefix, string suffix, Random randomizer)
        {
            try
            {

                var currentImage = new Bitmap(file);
                if (currentImage == null)
                {
                    textBoxStatus.Text = "Unable to open file " + file;
                    return;
                }
                var newFilename = CreateFileName(file, targetFolder, prefix, suffix);
                var processedImage = ImageFunctions.ImagingInstance.ApplyConversion(currentImage, ImageFunctions.ConversionParameterSet, randomizer);
                if (processedImage == null) return;
                processedImage.Save(newFilename);
                ImageFunctions.DisposeBitmap(processedImage);
                ImageFunctions.DisposeBitmap(currentImage);
            }
            catch (Exception ex)
            {
                textBoxStatus.Text = ex.Message;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveConfiguration();
            base.OnClosed(e);
        }

        private void buttonSetStrongPoint_Click(object sender, EventArgs e)
        {
            if (sender == buttonSetStrongPointRGGB)
            {
                SetStrongPoint(BayerFilter.red, textBoxOriginalValueRed.Value, textBoxNewValueRed.Value);
                SetStrongPoint(BayerFilter.green1, textBoxOriginalValueGreen1.Value, textBoxNewValueGreen1.Value);
                SetStrongPoint(BayerFilter.green2, textBoxOriginalValueGreen2.Value, textBoxNewValueGreen2.Value);
                SetStrongPoint(BayerFilter.blue, textBoxOriginalValueBlue.Value, textBoxNewValueBlue.Value);
            }
            if (sender == buttonSetStrongPointRed) SetStrongPoint(BayerFilter.red, textBoxOriginalValueRed.Value, textBoxNewValueRed.Value);
            if (sender == buttonSetStrongPointGreen1) SetStrongPoint(BayerFilter.green1, textBoxOriginalValueGreen1.Value, textBoxNewValueGreen1.Value);
            if (sender == buttonSetStrongPointGreen2) SetStrongPoint(BayerFilter.green2, textBoxOriginalValueGreen2.Value, textBoxNewValueGreen2.Value);
            if (sender == buttonSetStrongPointBlue) SetStrongPoint(BayerFilter.blue, textBoxOriginalValueBlue.Value, textBoxNewValueBlue.Value);
        }


        private void SetStrongPoint(StrongPoint strongPoint)
        {
            SetStrongPoint(strongPoint.BayerFilter, strongPoint.OriginalGreyValue, strongPoint.NewGreyValue);
        }

        private void SetStrongPoint(BayerFilter bayerFilter, decimal originalValue, decimal newValue)
        {
            ImageFunctions.SetStrongPoint(bayerFilter, originalValue, newValue);
            FillListViewStrongPoints(ImageFunctions.ConversionParameterSet);
        }

        private void FillListViewStrongPoints(ConversionParameterSet conversionParameterSet)
        {
            listViewStrongPointList.Items.Clear();
            var itemCount = 0;
            foreach (var bf in Enum.GetValues(typeof(BayerFilter)))
            {
                BayerFilter bayerFilter = (BayerFilter)bf;
                if (conversionParameterSet.Sets.ContainsKey(bayerFilter))
                {
                    foreach (var strongPoint in conversionParameterSet.Sets[bayerFilter].StrongPoints)
                    {
                        itemCount++;
                        var listItem = new ListViewItem(itemCount.ToString());
                        listItem.SubItems.AddRange(StrongPointData(bayerFilter, strongPoint));
                        listItem.Tag = strongPoint;
                        listViewStrongPointList.Items.Add(listItem);
                    }
                }
            }
        }

        private static string[] StrongPointData(BayerFilter bayerFilter, StrongPoint strongPoint)
        {
            return new string[] { bayerFilter.ToString(), strongPoint.OriginalGreyValue.ToString(), strongPoint.NewGreyValue.ToString(), strongPoint.PreviewStrongPoint ? "●" : "" };
        }

        private void buttonDeleteStrongPoints_Click(object sender, EventArgs e)
        {
            if (listViewStrongPointList.Items.Count <= 0) return;
            if (listViewStrongPointList.CheckedItems.Count == 0) return;
            foreach (ListViewItem item in listViewStrongPointList.CheckedItems)
            {
                ImageFunctions.ConversionParameterSet.RemoveStrongPoint((StrongPoint)item.Tag);
            }
            FillListViewStrongPoints(ImageFunctions.ConversionParameterSet);
        }

        private void listViewStrongPointList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewStrongPointList.SelectedItems.Count > 0)
            {
                var item = listViewStrongPointList.SelectedItems[0];
                if (item.Tag != null && item.Tag is StrongPoint)
                {
                    var strongPoint = (StrongPoint)item.Tag;
                    var strongPointList = new List<StrongPoint> { strongPoint };

                    foreach (BayerFilter bayerFilter in Enum.GetValues(typeof(BayerFilter)))
                    {
                        if (!ImageFunctions.ConversionParameterSet.Sets.ContainsKey(bayerFilter)) continue;
                        var theSetsStrongPoints = ImageFunctions.ConversionParameterSet.Sets[bayerFilter].StrongPoints;
                        var searchedStrongPoint = theSetsStrongPoints.FirstOrDefault(sp => sp.OriginalGreyValue == strongPoint.OriginalGreyValue);
                        if (searchedStrongPoint != null)
                        {
                            strongPointList.Add(searchedStrongPoint);
                        }
                        else
                        {
                            strongPointList.Add(new StrongPoint(bayerFilter, strongPoint.OriginalGreyValue, ImageFunctions.ConversionParameterSet.Sets[bayerFilter].ConversionDictionary[strongPoint.OriginalGreyValue], false));
                        }
                    }
                    FillValueConversionFields(strongPointList);
                }
            }
        }

        private void buttonSaveValues_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            if (ImageFunctions.ImagingInstance.CurrentImage != null)
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(Filename);
            }
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(Filename);
            saveFileDialog.Filter = "conversion parameter set(*.cps)|*.cps";
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.Runtime.Serialization.DataContractSerializer xmlSerializer = new System.Runtime.Serialization.DataContractSerializer(typeof(ConversionParameterSet));
                var stream = new System.IO.FileStream(saveFileDialog.FileName,System.IO.FileMode.Create);

                xmlSerializer.WriteObject(stream, ImageFunctions.ConversionParameterSet);
                stream.Flush();
                stream.Close();
            }
        }

        private void buttonLoadValues_Click(object sender, EventArgs e)
        {
            var fileOpenDialog = new OpenFileDialog();
            fileOpenDialog.CheckFileExists = true;
            fileOpenDialog.CheckPathExists = true;
            fileOpenDialog.Multiselect = false;
            fileOpenDialog.Title = "Load conversion parameter set...";
            fileOpenDialog.DefaultExt = "*.cps";
            fileOpenDialog.ValidateNames = true;
            if (fileOpenDialog.ShowDialog() == DialogResult.Cancel) return;
            var file = fileOpenDialog.FileName;

            if (File.Exists(file))
            {
                try
                {
                    System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(ConversionParameterSet));
                    FileStream fs = new FileStream(file, FileMode.Open);
                    var temp = serializer.ReadObject(fs) as ConversionParameterSet;
                    if (temp!=null)
                    {
                        ImageFunctions.ConversionParameterSet = temp;
                        FillListViewStrongPoints(temp);
                    }
                    fs.Close();
                }
                catch
                {

                }
            }
        }
    }
}
