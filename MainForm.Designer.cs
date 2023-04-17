namespace Antibayer
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this._pbWorkingImage = new System.Windows.Forms.PictureBox();
            this._pbHistoRGB = new System.Windows.Forms.PictureBox();
            this._pbHistoR = new System.Windows.Forms.PictureBox();
            this._pbHistoG = new System.Windows.Forms.PictureBox();
            this._pbHistoB = new System.Windows.Forms.PictureBox();
            this.buttonLoadImage = new System.Windows.Forms.Button();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.checkBoxOnlyRGBHistogram = new System.Windows.Forms.CheckBox();
            this.radioButtonBayerRGGB = new System.Windows.Forms.RadioButton();
            this.radioButtonBayerGRGB = new System.Windows.Forms.RadioButton();
            this.radioButtonbayerNone = new System.Windows.Forms.RadioButton();
            this.labelMultiply = new System.Windows.Forms.Label();
            this.labelShift = new System.Windows.Forms.Label();
            this.labelRed = new System.Windows.Forms.Label();
            this.labelGreen1 = new System.Windows.Forms.Label();
            this.labelGreen2 = new System.Windows.Forms.Label();
            this.labelBlue = new System.Windows.Forms.Label();
            this.textBoxOriginalValueRed = new System.Windows.Forms.NumericUpDown();
            this.textBoxNewValueRed = new System.Windows.Forms.NumericUpDown();
            this.textBoxNewValueGreen1 = new System.Windows.Forms.NumericUpDown();
            this.textBoxOriginalValueGreen1 = new System.Windows.Forms.NumericUpDown();
            this.textBoxNewValueGreen2 = new System.Windows.Forms.NumericUpDown();
            this.textBoxOriginalValueGreen2 = new System.Windows.Forms.NumericUpDown();
            this.textBoxNewValueBlue = new System.Windows.Forms.NumericUpDown();
            this.textBoxOriginalValueBlue = new System.Windows.Forms.NumericUpDown();
            this.buttonMarkR = new System.Windows.Forms.Button();
            this.buttonMarkG1 = new System.Windows.Forms.Button();
            this.buttonMarkG2 = new System.Windows.Forms.Button();
            this.buttonMarkB = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonConvertFolder = new System.Windows.Forms.Button();
            this.textBoxFilenamePrefix = new System.Windows.Forms.TextBox();
            this.labelFilenamePrefix = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxFilenameSuffix = new System.Windows.Forms.TextBox();
            this.buttonExit = new System.Windows.Forms.Button();
            this.radioButtonBayerGBRG = new System.Windows.Forms.RadioButton();
            this.radioButtonBayerBGGR = new System.Windows.Forms.RadioButton();
            this.buttonViewSource = new System.Windows.Forms.Button();
            this.textBoxImageInfo = new System.Windows.Forms.TextBox();
            this.buttonSetStrongPointBlue = new System.Windows.Forms.Button();
            this.buttonSetStrongPointGreen2 = new System.Windows.Forms.Button();
            this.buttonSetStrongPointGreen1 = new System.Windows.Forms.Button();
            this.buttonSetStrongPointRed = new System.Windows.Forms.Button();
            this.labelSet = new System.Windows.Forms.Label();
            this.buttonSetStrongPointRGGB = new System.Windows.Forms.Button();
            this.listViewStrongPointList = new System.Windows.Forms.ListView();
            this.Index = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Bayer = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Original = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Target = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Preview = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._pbPreview = new Antibayer.PictureBoxWithInterpolationMode();
            this.buttonDeleteStrongPoints = new System.Windows.Forms.Button();
            this.buttonSaveValues = new System.Windows.Forms.Button();
            this.buttonLoadValues = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._pbWorkingImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoRGB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoR)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoG)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueRed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueRed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueGreen1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueGreen1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueGreen2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueGreen2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueBlue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueBlue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // _pbWorkingImage
            // 
            this._pbWorkingImage.Location = new System.Drawing.Point(566, 12);
            this._pbWorkingImage.Name = "_pbWorkingImage";
            this._pbWorkingImage.Size = new System.Drawing.Size(270, 270);
            this._pbWorkingImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this._pbWorkingImage.TabIndex = 1;
            this._pbWorkingImage.TabStop = false;
            this._pbWorkingImage.Click += new System.EventHandler(this._pbWorkingImage_Click);
            this._pbWorkingImage.MouseDown += new System.Windows.Forms.MouseEventHandler(this._pbWorkingImage_MouseDown);
            // 
            // _pbHistoRGB
            // 
            this._pbHistoRGB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._pbHistoRGB.Location = new System.Drawing.Point(566, 321);
            this._pbHistoRGB.Name = "_pbHistoRGB";
            this._pbHistoRGB.Size = new System.Drawing.Size(532, 63);
            this._pbHistoRGB.TabIndex = 2;
            this._pbHistoRGB.TabStop = false;
            // 
            // _pbHistoR
            // 
            this._pbHistoR.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._pbHistoR.Location = new System.Drawing.Point(566, 390);
            this._pbHistoR.Name = "_pbHistoR";
            this._pbHistoR.Size = new System.Drawing.Size(532, 63);
            this._pbHistoR.TabIndex = 3;
            this._pbHistoR.TabStop = false;
            // 
            // _pbHistoG
            // 
            this._pbHistoG.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._pbHistoG.Location = new System.Drawing.Point(566, 486);
            this._pbHistoG.Name = "_pbHistoG";
            this._pbHistoG.Size = new System.Drawing.Size(532, 79);
            this._pbHistoG.TabIndex = 4;
            this._pbHistoG.TabStop = false;
            // 
            // _pbHistoB
            // 
            this._pbHistoB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._pbHistoB.Location = new System.Drawing.Point(566, 596);
            this._pbHistoB.Name = "_pbHistoB";
            this._pbHistoB.Size = new System.Drawing.Size(532, 70);
            this._pbHistoB.TabIndex = 5;
            this._pbHistoB.TabStop = false;
            // 
            // buttonLoadImage
            // 
            this.buttonLoadImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonLoadImage.Location = new System.Drawing.Point(846, 12);
            this.buttonLoadImage.Name = "buttonLoadImage";
            this.buttonLoadImage.Size = new System.Drawing.Size(48, 44);
            this.buttonLoadImage.TabIndex = 6;
            this.buttonLoadImage.Text = "load image";
            this.buttonLoadImage.UseVisualStyleBackColor = true;
            this.buttonLoadImage.Click += new System.EventHandler(this.buttonLoadImage_Click);
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStatus.Location = new System.Drawing.Point(0, 671);
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.Size = new System.Drawing.Size(1104, 20);
            this.textBoxStatus.TabIndex = 7;
            // 
            // checkBoxOnlyRGBHistogram
            // 
            this.checkBoxOnlyRGBHistogram.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxOnlyRGBHistogram.AutoSize = true;
            this.checkBoxOnlyRGBHistogram.Location = new System.Drawing.Point(846, 122);
            this.checkBoxOnlyRGBHistogram.Name = "checkBoxOnlyRGBHistogram";
            this.checkBoxOnlyRGBHistogram.Size = new System.Drawing.Size(147, 17);
            this.checkBoxOnlyRGBHistogram.TabIndex = 8;
            this.checkBoxOnlyRGBHistogram.Text = "show only RGB histogram";
            this.checkBoxOnlyRGBHistogram.UseVisualStyleBackColor = true;
            this.checkBoxOnlyRGBHistogram.CheckedChanged += new System.EventHandler(this.checkBoxOnlyRGBHistogram_CheckedChanged);
            // 
            // radioButtonBayerRGGB
            // 
            this.radioButtonBayerRGGB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButtonBayerRGGB.Location = new System.Drawing.Point(851, 83);
            this.radioButtonBayerRGGB.Name = "radioButtonBayerRGGB";
            this.radioButtonBayerRGGB.Size = new System.Drawing.Size(43, 33);
            this.radioButtonBayerRGGB.TabIndex = 9;
            this.radioButtonBayerRGGB.Text = "RGGB";
            this.radioButtonBayerRGGB.UseVisualStyleBackColor = true;
            this.radioButtonBayerRGGB.CheckedChanged += new System.EventHandler(this.RadioButtonBayer_CheckedChanged);
            // 
            // radioButtonBayerGRGB
            // 
            this.radioButtonBayerGRGB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButtonBayerGRGB.Location = new System.Drawing.Point(900, 83);
            this.radioButtonBayerGRGB.Name = "radioButtonBayerGRGB";
            this.radioButtonBayerGRGB.Size = new System.Drawing.Size(43, 33);
            this.radioButtonBayerGRGB.TabIndex = 10;
            this.radioButtonBayerGRGB.Text = "GRGB";
            this.radioButtonBayerGRGB.UseVisualStyleBackColor = true;
            this.radioButtonBayerGRGB.CheckedChanged += new System.EventHandler(this.RadioButtonBayer_CheckedChanged);
            // 
            // radioButtonbayerNone
            // 
            this.radioButtonbayerNone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButtonbayerNone.Checked = true;
            this.radioButtonbayerNone.Location = new System.Drawing.Point(1041, 83);
            this.radioButtonbayerNone.Name = "radioButtonbayerNone";
            this.radioButtonbayerNone.Size = new System.Drawing.Size(57, 33);
            this.radioButtonbayerNone.TabIndex = 11;
            this.radioButtonbayerNone.TabStop = true;
            this.radioButtonbayerNone.Text = "None";
            this.radioButtonbayerNone.UseVisualStyleBackColor = true;
            this.radioButtonbayerNone.CheckedChanged += new System.EventHandler(this.RadioButtonBayer_CheckedChanged);
            // 
            // labelMultiply
            // 
            this.labelMultiply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMultiply.Location = new System.Drawing.Point(938, 150);
            this.labelMultiply.Name = "labelMultiply";
            this.labelMultiply.Size = new System.Drawing.Size(48, 32);
            this.labelMultiply.TabIndex = 12;
            this.labelMultiply.Text = "new value";
            this.labelMultiply.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // labelShift
            // 
            this.labelShift.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelShift.Location = new System.Drawing.Point(884, 151);
            this.labelShift.Name = "labelShift";
            this.labelShift.Size = new System.Drawing.Size(48, 32);
            this.labelShift.TabIndex = 13;
            this.labelShift.Text = "original value";
            this.labelShift.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // labelRed
            // 
            this.labelRed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRed.AutoSize = true;
            this.labelRed.Location = new System.Drawing.Point(842, 189);
            this.labelRed.Name = "labelRed";
            this.labelRed.Size = new System.Drawing.Size(27, 13);
            this.labelRed.TabIndex = 14;
            this.labelRed.Text = "Red";
            // 
            // labelGreen1
            // 
            this.labelGreen1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelGreen1.AutoSize = true;
            this.labelGreen1.Location = new System.Drawing.Point(842, 214);
            this.labelGreen1.Name = "labelGreen1";
            this.labelGreen1.Size = new System.Drawing.Size(42, 13);
            this.labelGreen1.TabIndex = 15;
            this.labelGreen1.Text = "Green1";
            // 
            // labelGreen2
            // 
            this.labelGreen2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelGreen2.AutoSize = true;
            this.labelGreen2.Location = new System.Drawing.Point(842, 240);
            this.labelGreen2.Name = "labelGreen2";
            this.labelGreen2.Size = new System.Drawing.Size(42, 13);
            this.labelGreen2.TabIndex = 16;
            this.labelGreen2.Text = "Green2";
            // 
            // labelBlue
            // 
            this.labelBlue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelBlue.AutoSize = true;
            this.labelBlue.Location = new System.Drawing.Point(842, 266);
            this.labelBlue.Name = "labelBlue";
            this.labelBlue.Size = new System.Drawing.Size(28, 13);
            this.labelBlue.TabIndex = 17;
            this.labelBlue.Text = "Blue";
            // 
            // textBoxOriginalValueRed
            // 
            this.textBoxOriginalValueRed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOriginalValueRed.Location = new System.Drawing.Point(887, 186);
            this.textBoxOriginalValueRed.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxOriginalValueRed.Name = "textBoxOriginalValueRed";
            this.textBoxOriginalValueRed.Size = new System.Drawing.Size(43, 20);
            this.textBoxOriginalValueRed.TabIndex = 18;
            this.textBoxOriginalValueRed.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxOriginalValueRed.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxNewValueRed
            // 
            this.textBoxNewValueRed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNewValueRed.Location = new System.Drawing.Point(939, 186);
            this.textBoxNewValueRed.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxNewValueRed.Name = "textBoxNewValueRed";
            this.textBoxNewValueRed.Size = new System.Drawing.Size(47, 20);
            this.textBoxNewValueRed.TabIndex = 19;
            this.textBoxNewValueRed.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxNewValueRed.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.textBoxNewValueRed.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxNewValueGreen1
            // 
            this.textBoxNewValueGreen1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNewValueGreen1.Location = new System.Drawing.Point(939, 211);
            this.textBoxNewValueGreen1.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxNewValueGreen1.Name = "textBoxNewValueGreen1";
            this.textBoxNewValueGreen1.Size = new System.Drawing.Size(47, 20);
            this.textBoxNewValueGreen1.TabIndex = 21;
            this.textBoxNewValueGreen1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxNewValueGreen1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.textBoxNewValueGreen1.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxOriginalValueGreen1
            // 
            this.textBoxOriginalValueGreen1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOriginalValueGreen1.Location = new System.Drawing.Point(887, 211);
            this.textBoxOriginalValueGreen1.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxOriginalValueGreen1.Name = "textBoxOriginalValueGreen1";
            this.textBoxOriginalValueGreen1.Size = new System.Drawing.Size(43, 20);
            this.textBoxOriginalValueGreen1.TabIndex = 20;
            this.textBoxOriginalValueGreen1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxOriginalValueGreen1.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxNewValueGreen2
            // 
            this.textBoxNewValueGreen2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNewValueGreen2.Location = new System.Drawing.Point(939, 237);
            this.textBoxNewValueGreen2.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxNewValueGreen2.Name = "textBoxNewValueGreen2";
            this.textBoxNewValueGreen2.Size = new System.Drawing.Size(47, 20);
            this.textBoxNewValueGreen2.TabIndex = 23;
            this.textBoxNewValueGreen2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxNewValueGreen2.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.textBoxNewValueGreen2.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxOriginalValueGreen2
            // 
            this.textBoxOriginalValueGreen2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOriginalValueGreen2.Location = new System.Drawing.Point(887, 237);
            this.textBoxOriginalValueGreen2.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxOriginalValueGreen2.Name = "textBoxOriginalValueGreen2";
            this.textBoxOriginalValueGreen2.Size = new System.Drawing.Size(43, 20);
            this.textBoxOriginalValueGreen2.TabIndex = 22;
            this.textBoxOriginalValueGreen2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxOriginalValueGreen2.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxNewValueBlue
            // 
            this.textBoxNewValueBlue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNewValueBlue.Location = new System.Drawing.Point(939, 263);
            this.textBoxNewValueBlue.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxNewValueBlue.Name = "textBoxNewValueBlue";
            this.textBoxNewValueBlue.Size = new System.Drawing.Size(47, 20);
            this.textBoxNewValueBlue.TabIndex = 25;
            this.textBoxNewValueBlue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxNewValueBlue.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.textBoxNewValueBlue.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // textBoxOriginalValueBlue
            // 
            this.textBoxOriginalValueBlue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOriginalValueBlue.Location = new System.Drawing.Point(887, 263);
            this.textBoxOriginalValueBlue.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.textBoxOriginalValueBlue.Name = "textBoxOriginalValueBlue";
            this.textBoxOriginalValueBlue.Size = new System.Drawing.Size(43, 20);
            this.textBoxOriginalValueBlue.TabIndex = 24;
            this.textBoxOriginalValueBlue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxOriginalValueBlue.ValueChanged += new System.EventHandler(this.HistogramValueChanged);
            // 
            // buttonMarkR
            // 
            this.buttonMarkR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMarkR.Location = new System.Drawing.Point(992, 184);
            this.buttonMarkR.Name = "buttonMarkR";
            this.buttonMarkR.Size = new System.Drawing.Size(50, 23);
            this.buttonMarkR.TabIndex = 26;
            this.buttonMarkR.Text = "R";
            this.buttonMarkR.UseVisualStyleBackColor = true;
            this.buttonMarkR.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BayerSelectoButton_MouseDown);
            this.buttonMarkR.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BayerSelectButton_MouseUp);
            // 
            // buttonMarkG1
            // 
            this.buttonMarkG1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMarkG1.Location = new System.Drawing.Point(992, 209);
            this.buttonMarkG1.Name = "buttonMarkG1";
            this.buttonMarkG1.Size = new System.Drawing.Size(50, 23);
            this.buttonMarkG1.TabIndex = 27;
            this.buttonMarkG1.Text = "G1";
            this.buttonMarkG1.UseVisualStyleBackColor = true;
            this.buttonMarkG1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BayerSelectoButton_MouseDown);
            this.buttonMarkG1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BayerSelectButton_MouseUp);
            // 
            // buttonMarkG2
            // 
            this.buttonMarkG2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMarkG2.Location = new System.Drawing.Point(992, 234);
            this.buttonMarkG2.Name = "buttonMarkG2";
            this.buttonMarkG2.Size = new System.Drawing.Size(50, 23);
            this.buttonMarkG2.TabIndex = 28;
            this.buttonMarkG2.Text = "G2";
            this.buttonMarkG2.UseVisualStyleBackColor = true;
            this.buttonMarkG2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BayerSelectoButton_MouseDown);
            this.buttonMarkG2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BayerSelectButton_MouseUp);
            // 
            // buttonMarkB
            // 
            this.buttonMarkB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMarkB.Location = new System.Drawing.Point(992, 260);
            this.buttonMarkB.Name = "buttonMarkB";
            this.buttonMarkB.Size = new System.Drawing.Size(50, 23);
            this.buttonMarkB.TabIndex = 29;
            this.buttonMarkB.Text = "B";
            this.buttonMarkB.UseVisualStyleBackColor = true;
            this.buttonMarkB.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BayerSelectoButton_MouseDown);
            this.buttonMarkB.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BayerSelectButton_MouseUp);
            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSave.Location = new System.Drawing.Point(900, 12);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(64, 44);
            this.buttonSave.TabIndex = 30;
            this.buttonSave.Text = "save image";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonConvertFolder
            // 
            this.buttonConvertFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonConvertFolder.Location = new System.Drawing.Point(970, 12);
            this.buttonConvertFolder.Name = "buttonConvertFolder";
            this.buttonConvertFolder.Size = new System.Drawing.Size(72, 44);
            this.buttonConvertFolder.TabIndex = 31;
            this.buttonConvertFolder.Text = "convert folder";
            this.buttonConvertFolder.UseVisualStyleBackColor = true;
            this.buttonConvertFolder.Click += new System.EventHandler(this.buttonConvertFolder_Click);
            // 
            // textBoxFilenamePrefix
            // 
            this.textBoxFilenamePrefix.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilenamePrefix.Location = new System.Drawing.Point(921, 62);
            this.textBoxFilenamePrefix.Name = "textBoxFilenamePrefix";
            this.textBoxFilenamePrefix.Size = new System.Drawing.Size(43, 20);
            this.textBoxFilenamePrefix.TabIndex = 32;
            this.textBoxFilenamePrefix.Text = "BA_";
            // 
            // labelFilenamePrefix
            // 
            this.labelFilenamePrefix.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFilenamePrefix.AutoSize = true;
            this.labelFilenamePrefix.Location = new System.Drawing.Point(842, 65);
            this.labelFilenamePrefix.Name = "labelFilenamePrefix";
            this.labelFilenamePrefix.Size = new System.Drawing.Size(77, 13);
            this.labelFilenamePrefix.TabIndex = 33;
            this.labelFilenamePrefix.Text = "Filename prefix";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(970, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 34;
            this.label2.Text = "Filename suffix";
            // 
            // textBoxFilenameSuffix
            // 
            this.textBoxFilenameSuffix.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilenameSuffix.Location = new System.Drawing.Point(1048, 62);
            this.textBoxFilenameSuffix.Name = "textBoxFilenameSuffix";
            this.textBoxFilenameSuffix.Size = new System.Drawing.Size(50, 20);
            this.textBoxFilenameSuffix.TabIndex = 35;
            // 
            // buttonExit
            // 
            this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExit.Location = new System.Drawing.Point(1048, 12);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(49, 44);
            this.buttonExit.TabIndex = 36;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // radioButtonBayerGBRG
            // 
            this.radioButtonBayerGBRG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButtonBayerGBRG.Location = new System.Drawing.Point(949, 83);
            this.radioButtonBayerGBRG.Name = "radioButtonBayerGBRG";
            this.radioButtonBayerGBRG.Size = new System.Drawing.Size(43, 33);
            this.radioButtonBayerGBRG.TabIndex = 37;
            this.radioButtonBayerGBRG.Text = "GBRG";
            this.radioButtonBayerGBRG.UseVisualStyleBackColor = true;
            this.radioButtonBayerGBRG.CheckedChanged += new System.EventHandler(this.RadioButtonBayer_CheckedChanged);
            // 
            // radioButtonBayerBGGR
            // 
            this.radioButtonBayerBGGR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButtonBayerBGGR.Location = new System.Drawing.Point(992, 83);
            this.radioButtonBayerBGGR.Name = "radioButtonBayerBGGR";
            this.radioButtonBayerBGGR.Size = new System.Drawing.Size(43, 33);
            this.radioButtonBayerBGGR.TabIndex = 38;
            this.radioButtonBayerBGGR.Text = "BGGR";
            this.radioButtonBayerBGGR.UseVisualStyleBackColor = true;
            this.radioButtonBayerBGGR.CheckedChanged += new System.EventHandler(this.RadioButtonBayer_CheckedChanged);
            // 
            // buttonViewSource
            // 
            this.buttonViewSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonViewSource.Location = new System.Drawing.Point(992, 160);
            this.buttonViewSource.Name = "buttonViewSource";
            this.buttonViewSource.Size = new System.Drawing.Size(50, 21);
            this.buttonViewSource.TabIndex = 39;
            this.buttonViewSource.Text = "Source";
            this.buttonViewSource.UseVisualStyleBackColor = true;
            this.buttonViewSource.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BayerSelectoButton_MouseDown);
            this.buttonViewSource.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BayerSelectButton_MouseUp);
            // 
            // textBoxImageInfo
            // 
            this.textBoxImageInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxImageInfo.Location = new System.Drawing.Point(12, 505);
            this.textBoxImageInfo.Multiline = true;
            this.textBoxImageInfo.Name = "textBoxImageInfo";
            this.textBoxImageInfo.ReadOnly = true;
            this.textBoxImageInfo.Size = new System.Drawing.Size(548, 160);
            this.textBoxImageInfo.TabIndex = 40;
            // 
            // buttonSetStrongPointBlue
            // 
            this.buttonSetStrongPointBlue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetStrongPointBlue.Location = new System.Drawing.Point(1047, 260);
            this.buttonSetStrongPointBlue.Name = "buttonSetStrongPointBlue";
            this.buttonSetStrongPointBlue.Size = new System.Drawing.Size(50, 23);
            this.buttonSetStrongPointBlue.TabIndex = 44;
            this.buttonSetStrongPointBlue.Text = "B";
            this.buttonSetStrongPointBlue.UseVisualStyleBackColor = true;
            this.buttonSetStrongPointBlue.Click += new System.EventHandler(this.buttonSetStrongPoint_Click);
            // 
            // buttonSetStrongPointGreen2
            // 
            this.buttonSetStrongPointGreen2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetStrongPointGreen2.Location = new System.Drawing.Point(1047, 234);
            this.buttonSetStrongPointGreen2.Name = "buttonSetStrongPointGreen2";
            this.buttonSetStrongPointGreen2.Size = new System.Drawing.Size(50, 23);
            this.buttonSetStrongPointGreen2.TabIndex = 43;
            this.buttonSetStrongPointGreen2.Text = "G2";
            this.buttonSetStrongPointGreen2.UseVisualStyleBackColor = true;
            this.buttonSetStrongPointGreen2.Click += new System.EventHandler(this.buttonSetStrongPoint_Click);
            // 
            // buttonSetStrongPointGreen1
            // 
            this.buttonSetStrongPointGreen1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetStrongPointGreen1.Location = new System.Drawing.Point(1047, 209);
            this.buttonSetStrongPointGreen1.Name = "buttonSetStrongPointGreen1";
            this.buttonSetStrongPointGreen1.Size = new System.Drawing.Size(50, 23);
            this.buttonSetStrongPointGreen1.TabIndex = 42;
            this.buttonSetStrongPointGreen1.Text = "G1";
            this.buttonSetStrongPointGreen1.UseVisualStyleBackColor = true;
            this.buttonSetStrongPointGreen1.Click += new System.EventHandler(this.buttonSetStrongPoint_Click);
            // 
            // buttonSetStrongPointRed
            // 
            this.buttonSetStrongPointRed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetStrongPointRed.Location = new System.Drawing.Point(1047, 184);
            this.buttonSetStrongPointRed.Name = "buttonSetStrongPointRed";
            this.buttonSetStrongPointRed.Size = new System.Drawing.Size(50, 23);
            this.buttonSetStrongPointRed.TabIndex = 41;
            this.buttonSetStrongPointRed.Text = "R";
            this.buttonSetStrongPointRed.UseVisualStyleBackColor = true;
            this.buttonSetStrongPointRed.Click += new System.EventHandler(this.buttonSetStrongPoint_Click);
            // 
            // labelSet
            // 
            this.labelSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSet.Location = new System.Drawing.Point(1048, 110);
            this.labelSet.Name = "labelSet";
            this.labelSet.Size = new System.Drawing.Size(49, 47);
            this.labelSet.TabIndex = 45;
            this.labelSet.Text = "set Strong- point";
            this.labelSet.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // buttonSetStrongPointRGGB
            // 
            this.buttonSetStrongPointRGGB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetStrongPointRGGB.Location = new System.Drawing.Point(1047, 161);
            this.buttonSetStrongPointRGGB.Name = "buttonSetStrongPointRGGB";
            this.buttonSetStrongPointRGGB.Size = new System.Drawing.Size(50, 21);
            this.buttonSetStrongPointRGGB.TabIndex = 46;
            this.buttonSetStrongPointRGGB.Text = "RGGB";
            this.buttonSetStrongPointRGGB.UseVisualStyleBackColor = true;
            this.buttonSetStrongPointRGGB.Click += new System.EventHandler(this.buttonSetStrongPoint_Click);
            // 
            // listViewStrongPointList
            // 
            this.listViewStrongPointList.AutoArrange = false;
            this.listViewStrongPointList.CheckBoxes = true;
            this.listViewStrongPointList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Index,
            this.Bayer,
            this.Original,
            this.Target,
            this.Preview});
            this.listViewStrongPointList.FullRowSelect = true;
            this.listViewStrongPointList.HideSelection = false;
            this.listViewStrongPointList.Location = new System.Drawing.Point(566, 321);
            this.listViewStrongPointList.Name = "listViewStrongPointList";
            this.listViewStrongPointList.ShowGroups = false;
            this.listViewStrongPointList.Size = new System.Drawing.Size(532, 344);
            this.listViewStrongPointList.TabIndex = 47;
            this.listViewStrongPointList.UseCompatibleStateImageBehavior = false;
            this.listViewStrongPointList.View = System.Windows.Forms.View.Details;
            this.listViewStrongPointList.SelectedIndexChanged += new System.EventHandler(this.listViewStrongPointList_SelectedIndexChanged);
            // 
            // Index
            // 
            this.Index.Text = "No";
            this.Index.Width = 40;
            // 
            // Bayer
            // 
            this.Bayer.Text = "Bayer";
            this.Bayer.Width = 100;
            // 
            // Original
            // 
            this.Original.Text = "Original";
            this.Original.Width = 100;
            // 
            // Target
            // 
            this.Target.Text = "Target";
            this.Target.Width = 100;
            // 
            // Preview
            // 
            this.Preview.Text = "Preview";
            // 
            // _pbPreview
            // 
            this._pbPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this._pbPreview.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            this._pbPreview.Location = new System.Drawing.Point(12, 12);
            this._pbPreview.Name = "_pbPreview";
            this._pbPreview.Size = new System.Drawing.Size(548, 487);
            this._pbPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._pbPreview.TabIndex = 0;
            this._pbPreview.TabStop = false;
            this._pbPreview.Click += new System.EventHandler(this._pbPreview_Click);
            // 
            // buttonDeleteStrongPoints
            // 
            this.buttonDeleteStrongPoints.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDeleteStrongPoints.Location = new System.Drawing.Point(566, 292);
            this.buttonDeleteStrongPoints.Name = "buttonDeleteStrongPoints";
            this.buttonDeleteStrongPoints.Size = new System.Drawing.Size(143, 23);
            this.buttonDeleteStrongPoints.TabIndex = 48;
            this.buttonDeleteStrongPoints.Text = "Delete selected values";
            this.buttonDeleteStrongPoints.UseVisualStyleBackColor = true;
            this.buttonDeleteStrongPoints.Click += new System.EventHandler(this.buttonDeleteStrongPoints_Click);
            // 
            // buttonSaveValues
            // 
            this.buttonSaveValues.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSaveValues.Location = new System.Drawing.Point(805, 292);
            this.buttonSaveValues.Name = "buttonSaveValues";
            this.buttonSaveValues.Size = new System.Drawing.Size(143, 23);
            this.buttonSaveValues.TabIndex = 49;
            this.buttonSaveValues.Text = "save values";
            this.buttonSaveValues.UseVisualStyleBackColor = true;
            this.buttonSaveValues.Click += new System.EventHandler(this.buttonSaveValues_Click);
            // 
            // buttonLoadValues
            // 
            this.buttonLoadValues.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonLoadValues.Location = new System.Drawing.Point(954, 292);
            this.buttonLoadValues.Name = "buttonLoadValues";
            this.buttonLoadValues.Size = new System.Drawing.Size(143, 23);
            this.buttonLoadValues.TabIndex = 50;
            this.buttonLoadValues.Text = "load values";
            this.buttonLoadValues.UseVisualStyleBackColor = true;
            this.buttonLoadValues.Click += new System.EventHandler(this.buttonLoadValues_Click);
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1104, 691);
            this.Controls.Add(this.buttonLoadValues);
            this.Controls.Add(this.buttonSaveValues);
            this.Controls.Add(this.buttonDeleteStrongPoints);
            this.Controls.Add(this.listViewStrongPointList);
            this.Controls.Add(this.buttonSetStrongPointRGGB);
            this.Controls.Add(this.labelSet);
            this.Controls.Add(this.buttonSetStrongPointBlue);
            this.Controls.Add(this.buttonSetStrongPointGreen2);
            this.Controls.Add(this.buttonSetStrongPointGreen1);
            this.Controls.Add(this.buttonSetStrongPointRed);
            this.Controls.Add(this.textBoxImageInfo);
            this.Controls.Add(this.buttonViewSource);
            this.Controls.Add(this.radioButtonBayerBGGR);
            this.Controls.Add(this.radioButtonBayerGBRG);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.textBoxFilenameSuffix);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelFilenamePrefix);
            this.Controls.Add(this.textBoxFilenamePrefix);
            this.Controls.Add(this.buttonConvertFolder);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonMarkB);
            this.Controls.Add(this.buttonMarkG2);
            this.Controls.Add(this.buttonMarkG1);
            this.Controls.Add(this.buttonMarkR);
            this.Controls.Add(this.textBoxNewValueBlue);
            this.Controls.Add(this.textBoxOriginalValueBlue);
            this.Controls.Add(this.textBoxNewValueGreen2);
            this.Controls.Add(this.textBoxOriginalValueGreen2);
            this.Controls.Add(this.textBoxNewValueGreen1);
            this.Controls.Add(this.textBoxOriginalValueGreen1);
            this.Controls.Add(this.textBoxNewValueRed);
            this.Controls.Add(this.textBoxOriginalValueRed);
            this.Controls.Add(this.labelBlue);
            this.Controls.Add(this.labelGreen2);
            this.Controls.Add(this.labelGreen1);
            this.Controls.Add(this.labelRed);
            this.Controls.Add(this.labelShift);
            this.Controls.Add(this.labelMultiply);
            this.Controls.Add(this.radioButtonbayerNone);
            this.Controls.Add(this.radioButtonBayerGRGB);
            this.Controls.Add(this.radioButtonBayerRGGB);
            this.Controls.Add(this.checkBoxOnlyRGBHistogram);
            this.Controls.Add(this.textBoxStatus);
            this.Controls.Add(this.buttonLoadImage);
            this.Controls.Add(this._pbHistoB);
            this.Controls.Add(this._pbHistoG);
            this.Controls.Add(this._pbHistoR);
            this.Controls.Add(this._pbHistoRGB);
            this.Controls.Add(this._pbWorkingImage);
            this.Controls.Add(this._pbPreview);
            this.MinimumSize = new System.Drawing.Size(1120, 730);
            this.Name = "MainForm";
            this.Text = "Bayer Adjust";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.pictureBoxImagePreview_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.pictureBoxImagePreview_DragEnter);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this._pbPreview_DragOver);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this._pbWorkingImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoRGB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoR)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoG)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbHistoB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueRed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueRed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueGreen1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueGreen1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueGreen2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueGreen2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxNewValueBlue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textBoxOriginalValueBlue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._pbPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PictureBoxWithInterpolationMode _pbPreview;
        private System.Windows.Forms.PictureBox _pbWorkingImage;
        private System.Windows.Forms.PictureBox _pbHistoRGB;
        private System.Windows.Forms.PictureBox _pbHistoR;
        private System.Windows.Forms.PictureBox _pbHistoG;
        private System.Windows.Forms.PictureBox _pbHistoB;
        private System.Windows.Forms.Button buttonLoadImage;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.CheckBox checkBoxOnlyRGBHistogram;
        private System.Windows.Forms.RadioButton radioButtonBayerRGGB;
        private System.Windows.Forms.RadioButton radioButtonBayerGRGB;
        private System.Windows.Forms.RadioButton radioButtonbayerNone;
        private System.Windows.Forms.Label labelMultiply;
        private System.Windows.Forms.Label labelShift;
        private System.Windows.Forms.Label labelRed;
        private System.Windows.Forms.Label labelGreen1;
        private System.Windows.Forms.Label labelGreen2;
        private System.Windows.Forms.Label labelBlue;
        private System.Windows.Forms.NumericUpDown textBoxOriginalValueRed;
        private System.Windows.Forms.NumericUpDown textBoxNewValueRed;
        private System.Windows.Forms.NumericUpDown textBoxNewValueGreen1;
        private System.Windows.Forms.NumericUpDown textBoxOriginalValueGreen1;
        private System.Windows.Forms.NumericUpDown textBoxNewValueGreen2;
        private System.Windows.Forms.NumericUpDown textBoxOriginalValueGreen2;
        private System.Windows.Forms.NumericUpDown textBoxNewValueBlue;
        private System.Windows.Forms.NumericUpDown textBoxOriginalValueBlue;
        private System.Windows.Forms.Button buttonMarkR;
        private System.Windows.Forms.Button buttonMarkG1;
        private System.Windows.Forms.Button buttonMarkG2;
        private System.Windows.Forms.Button buttonMarkB;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonConvertFolder;
        private System.Windows.Forms.TextBox textBoxFilenamePrefix;
        private System.Windows.Forms.Label labelFilenamePrefix;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxFilenameSuffix;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.RadioButton radioButtonBayerGBRG;
        private System.Windows.Forms.RadioButton radioButtonBayerBGGR;
        private System.Windows.Forms.Button buttonViewSource;
        private System.Windows.Forms.TextBox textBoxImageInfo;
        private System.Windows.Forms.Button buttonSetStrongPointBlue;
        private System.Windows.Forms.Button buttonSetStrongPointGreen2;
        private System.Windows.Forms.Button buttonSetStrongPointGreen1;
        private System.Windows.Forms.Button buttonSetStrongPointRed;
        private System.Windows.Forms.Label labelSet;
        private System.Windows.Forms.Button buttonSetStrongPointRGGB;
        private System.Windows.Forms.ListView listViewStrongPointList;
        private System.Windows.Forms.ColumnHeader Index;
        private System.Windows.Forms.ColumnHeader Bayer;
        private System.Windows.Forms.ColumnHeader Original;
        private System.Windows.Forms.ColumnHeader Target;
        private System.Windows.Forms.ColumnHeader Preview;
        private System.Windows.Forms.Button buttonDeleteStrongPoints;
        private System.Windows.Forms.Button buttonSaveValues;
        private System.Windows.Forms.Button buttonLoadValues;
    }
}

