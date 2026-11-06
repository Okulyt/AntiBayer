namespace AntiBayer
{
    partial class SeriesForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.labelFiles = new System.Windows.Forms.Label();
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.buttonAddFiles = new System.Windows.Forms.Button();
            this.buttonRemoveFiles = new System.Windows.Forms.Button();
            this.labelOutput = new System.Windows.Forms.Label();
            this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelPrefix = new System.Windows.Forms.Label();
            this.textBoxPrefix = new System.Windows.Forms.TextBox();
            this.labelSuffix = new System.Windows.Forms.Label();
            this.textBoxSuffix = new System.Windows.Forms.TextBox();
            this.labelIssues = new System.Windows.Forms.Label();
            this.listBoxIssues = new System.Windows.Forms.ListBox();
            this.labelNote = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.textBoxReport = new System.Windows.Forms.TextBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // labelFiles
            //
            this.labelFiles.AutoSize = true;
            this.labelFiles.Location = new System.Drawing.Point(12, 9);
            this.labelFiles.Name = "labelFiles";
            this.labelFiles.Text = "Files";
            //
            // listBoxFiles
            //
            this.listBoxFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxFiles.HorizontalScrollbar = true;
            this.listBoxFiles.Location = new System.Drawing.Point(12, 27);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxFiles.Size = new System.Drawing.Size(520, 95);
            this.listBoxFiles.TabIndex = 0;
            //
            // buttonAddFiles
            //
            this.buttonAddFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddFiles.Location = new System.Drawing.Point(538, 27);
            this.buttonAddFiles.Name = "buttonAddFiles";
            this.buttonAddFiles.Size = new System.Drawing.Size(90, 25);
            this.buttonAddFiles.TabIndex = 1;
            this.buttonAddFiles.Text = "Add files...";
            this.buttonAddFiles.UseVisualStyleBackColor = true;
            this.buttonAddFiles.Click += new System.EventHandler(this.buttonAddFiles_Click);
            //
            // buttonRemoveFiles
            //
            this.buttonRemoveFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveFiles.Location = new System.Drawing.Point(538, 58);
            this.buttonRemoveFiles.Name = "buttonRemoveFiles";
            this.buttonRemoveFiles.Size = new System.Drawing.Size(90, 25);
            this.buttonRemoveFiles.TabIndex = 2;
            this.buttonRemoveFiles.Text = "Remove";
            this.buttonRemoveFiles.UseVisualStyleBackColor = true;
            this.buttonRemoveFiles.Click += new System.EventHandler(this.buttonRemoveFiles_Click);
            //
            // labelOutput
            //
            this.labelOutput.AutoSize = true;
            this.labelOutput.Location = new System.Drawing.Point(12, 132);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Text = "Output folder";
            //
            // textBoxOutputFolder
            //
            this.textBoxOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputFolder.Location = new System.Drawing.Point(12, 150);
            this.textBoxOutputFolder.Name = "textBoxOutputFolder";
            this.textBoxOutputFolder.Size = new System.Drawing.Size(520, 20);
            this.textBoxOutputFolder.TabIndex = 3;
            this.textBoxOutputFolder.TextChanged += new System.EventHandler(this.SettingChanged);
            //
            // buttonBrowse
            //
            this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowse.Location = new System.Drawing.Point(538, 148);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(90, 24);
            this.buttonBrowse.TabIndex = 4;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            //
            // labelPrefix
            //
            this.labelPrefix.AutoSize = true;
            this.labelPrefix.Location = new System.Drawing.Point(12, 181);
            this.labelPrefix.Name = "labelPrefix";
            this.labelPrefix.Text = "Name prefix";
            //
            // textBoxPrefix
            //
            this.textBoxPrefix.Location = new System.Drawing.Point(80, 178);
            this.textBoxPrefix.Name = "textBoxPrefix";
            this.textBoxPrefix.Size = new System.Drawing.Size(80, 20);
            this.textBoxPrefix.TabIndex = 5;
            //
            // labelSuffix
            //
            this.labelSuffix.AutoSize = true;
            this.labelSuffix.Location = new System.Drawing.Point(180, 181);
            this.labelSuffix.Name = "labelSuffix";
            this.labelSuffix.Text = "Name suffix";
            //
            // textBoxSuffix
            //
            this.textBoxSuffix.Location = new System.Drawing.Point(248, 178);
            this.textBoxSuffix.Name = "textBoxSuffix";
            this.textBoxSuffix.Size = new System.Drawing.Size(80, 20);
            this.textBoxSuffix.TabIndex = 6;
            //
            // labelIssues
            //
            this.labelIssues.AutoSize = true;
            this.labelIssues.Location = new System.Drawing.Point(12, 208);
            this.labelIssues.Name = "labelIssues";
            this.labelIssues.Text = "Check before the run";
            //
            // listBoxIssues
            //
            this.listBoxIssues.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxIssues.HorizontalScrollbar = true;
            this.listBoxIssues.Location = new System.Drawing.Point(12, 226);
            this.listBoxIssues.Name = "listBoxIssues";
            this.listBoxIssues.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listBoxIssues.Size = new System.Drawing.Size(616, 82);
            this.listBoxIssues.TabIndex = 7;
            this.listBoxIssues.TabStop = false;
            //
            // labelNote
            //
            this.labelNote.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNote.Location = new System.Drawing.Point(12, 314);
            this.labelNote.Name = "labelNote";
            this.labelNote.Size = new System.Drawing.Size(616, 18);
            this.labelNote.Text = "";
            //
            // progressBar
            //
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 338);
            this.progressBar.Maximum = 1000;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(616, 18);
            //
            // labelProgress
            //
            this.labelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgress.Location = new System.Drawing.Point(12, 360);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(616, 18);
            this.labelProgress.Text = "";
            //
            // textBoxReport
            //
            this.textBoxReport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxReport.Location = new System.Drawing.Point(12, 381);
            this.textBoxReport.Multiline = true;
            this.textBoxReport.Name = "textBoxReport";
            this.textBoxReport.ReadOnly = true;
            this.textBoxReport.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxReport.Size = new System.Drawing.Size(616, 120);
            this.textBoxReport.TabStop = false;
            this.textBoxReport.WordWrap = false;
            //
            // buttonStart
            //
            this.buttonStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStart.Location = new System.Drawing.Point(472, 509);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 28);
            this.buttonStart.TabIndex = 8;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(553, 509);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 28);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "Close";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            //
            // checkTimer
            //
            this.checkTimer.Interval = 400;
            this.checkTimer.Tick += new System.EventHandler(this.checkTimer_Tick);
            //
            // SeriesForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 549);
            this.Controls.Add(this.labelFiles);
            this.Controls.Add(this.listBoxFiles);
            this.Controls.Add(this.buttonAddFiles);
            this.Controls.Add(this.buttonRemoveFiles);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.textBoxOutputFolder);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.labelPrefix);
            this.Controls.Add(this.textBoxPrefix);
            this.Controls.Add(this.labelSuffix);
            this.Controls.Add(this.textBoxSuffix);
            this.Controls.Add(this.labelIssues);
            this.Controls.Add(this.listBoxIssues);
            this.Controls.Add(this.labelNote);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.textBoxReport);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonCancel);
            this.MinimumSize = new System.Drawing.Size(560, 480);
            this.Name = "SeriesForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AntiBayer - convert series";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SeriesForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label labelFiles;
        private System.Windows.Forms.ListBox listBoxFiles;
        private System.Windows.Forms.Button buttonAddFiles;
        private System.Windows.Forms.Button buttonRemoveFiles;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.TextBox textBoxOutputFolder;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelPrefix;
        private System.Windows.Forms.TextBox textBoxPrefix;
        private System.Windows.Forms.Label labelSuffix;
        private System.Windows.Forms.TextBox textBoxSuffix;
        private System.Windows.Forms.Label labelIssues;
        private System.Windows.Forms.ListBox listBoxIssues;
        private System.Windows.Forms.Label labelNote;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.TextBox textBoxReport;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Timer checkTimer;
    }
}
