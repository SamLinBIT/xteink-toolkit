namespace XTEinkToolkit
{
    partial class FrmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPreview = new System.Windows.Forms.LinkLabel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnAdvancedOptions = new System.Windows.Forms.LinkLabel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.chkShowENCharacter = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.chkTraditionalChinese = new System.Windows.Forms.RadioButton();
            this.lblPreviewMessage = new System.Windows.Forms.Label();
            this.chkVerticalFont = new System.Windows.Forms.CheckBox();
            this.chkShowBorder = new System.Windows.Forms.CheckBox();
            this.chkLandspace = new System.Windows.Forms.CheckBox();
            this.chkRenderGridFit = new System.Windows.Forms.CheckBox();
            this.chkRenderAntiAltas = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.numFontGamma = new System.Windows.Forms.TrackBar();
            this.numCharSpacing = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numLineSpacing = new System.Windows.Forms.NumericUpDown();
            this.numFontSizePt = new System.Windows.Forms.NumericUpDown();
            this.btnChooseFontFile = new System.Windows.Forms.Button();
            this.btnSelectFont = new System.Windows.Forms.Button();
            this.lblFontSource = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnDoGeneration = new System.Windows.Forms.Button();
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.debounceTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.mnuAdvancedOptions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.chkShowBorderInBinaryFont = new System.Windows.Forms.ToolStripMenuItem();
            this.chkOldLineAlignment = new System.Windows.Forms.ToolStripMenuItem();
            this.previewSurface = new XTEinkToolkit.Controls.CanvasControl();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCharSpacing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLineSpacing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePt)).BeginInit();
            this.mnuAdvancedOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.panel2);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPreview);
            this.groupBox1.Controls.Add(this.previewSurface);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // btnPreview
            // 
            resources.ApplyResources(this.btnPreview, "btnPreview");
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.TabStop = true;
            this.btnPreview.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnPreview_LinkClicked);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.btnDoGeneration);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.groupBox2);
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Name = "panel3";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnAdvancedOptions);
            this.groupBox2.Controls.Add(this.btnToggleResolution);
            this.groupBox2.Controls.Add(this.panel4);
            this.groupBox2.Controls.Add(this.lblPreviewMessage);
            this.groupBox2.Controls.Add(this.chkVerticalFont);
            this.groupBox2.Controls.Add(this.chkShowBorder);
            this.groupBox2.Controls.Add(this.chkLandspace);
            this.groupBox2.Controls.Add(this.chkRenderGridFit);
            this.groupBox2.Controls.Add(this.chkRenderAntiAltas);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.numFontGamma);
            this.groupBox2.Controls.Add(this.numCharSpacing);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.numLineSpacing);
            this.groupBox2.Controls.Add(this.numFontSizePt);
            this.groupBox2.Controls.Add(this.btnChooseFontFile);
            this.groupBox2.Controls.Add(this.btnSelectFont);
            this.groupBox2.Controls.Add(this.lblFontSource);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label1);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // btnAdvancedOptions
            // 
            resources.ApplyResources(this.btnAdvancedOptions, "btnAdvancedOptions");
            this.btnAdvancedOptions.Name = "btnAdvancedOptions";
            this.btnAdvancedOptions.TabStop = true;
            this.btnAdvancedOptions.VisitedLinkColor = System.Drawing.Color.Blue;
            this.btnAdvancedOptions.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnAdvancedOptions_LinkClicked);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.chkShowENCharacter);
            this.panel4.Controls.Add(this.radioButton1);
            this.panel4.Controls.Add(this.chkTraditionalChinese);
            resources.ApplyResources(this.panel4, "panel4");
            this.panel4.Name = "panel4";
            // 
            // chkShowENCharacter
            // 
            resources.ApplyResources(this.chkShowENCharacter, "chkShowENCharacter");
            this.chkShowENCharacter.Name = "chkShowENCharacter";
            this.chkShowENCharacter.UseVisualStyleBackColor = true;
            this.chkShowENCharacter.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // radioButton1
            // 
            resources.ApplyResources(this.radioButton1, "radioButton1");
            this.radioButton1.Checked = true;
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.TabStop = true;
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkTraditionalChinese
            // 
            resources.ApplyResources(this.chkTraditionalChinese, "chkTraditionalChinese");
            this.chkTraditionalChinese.Name = "chkTraditionalChinese";
            this.chkTraditionalChinese.UseVisualStyleBackColor = true;
            this.chkTraditionalChinese.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // lblPreviewMessage
            // 
            resources.ApplyResources(this.lblPreviewMessage, "lblPreviewMessage");
            this.lblPreviewMessage.Name = "lblPreviewMessage";
            // 
            // chkVerticalFont
            // 
            resources.ApplyResources(this.chkVerticalFont, "chkVerticalFont");
            this.chkVerticalFont.Name = "chkVerticalFont";
            this.toolTip1.SetToolTip(this.chkVerticalFont, resources.GetString("chkVerticalFont.ToolTip"));
            this.chkVerticalFont.UseVisualStyleBackColor = true;
            this.chkVerticalFont.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkShowBorder
            // 
            resources.ApplyResources(this.chkShowBorder, "chkShowBorder");
            this.chkShowBorder.Name = "chkShowBorder";
            this.toolTip1.SetToolTip(this.chkShowBorder, resources.GetString("chkShowBorder.ToolTip"));
            this.chkShowBorder.UseVisualStyleBackColor = true;
            this.chkShowBorder.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkLandspace
            // 
            resources.ApplyResources(this.chkLandspace, "chkLandspace");
            this.chkLandspace.Name = "chkLandspace";
            this.toolTip1.SetToolTip(this.chkLandspace, resources.GetString("chkLandspace.ToolTip"));
            this.chkLandspace.UseVisualStyleBackColor = true;
            this.chkLandspace.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkRenderGridFit
            // 
            resources.ApplyResources(this.chkRenderGridFit, "chkRenderGridFit");
            this.chkRenderGridFit.Checked = true;
            this.chkRenderGridFit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRenderGridFit.Name = "chkRenderGridFit";
            this.toolTip1.SetToolTip(this.chkRenderGridFit, resources.GetString("chkRenderGridFit.ToolTip"));
            this.chkRenderGridFit.UseVisualStyleBackColor = true;
            this.chkRenderGridFit.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkRenderAntiAltas
            // 
            resources.ApplyResources(this.chkRenderAntiAltas, "chkRenderAntiAltas");
            this.chkRenderAntiAltas.Checked = true;
            this.chkRenderAntiAltas.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRenderAntiAltas.Name = "chkRenderAntiAltas";
            this.toolTip1.SetToolTip(this.chkRenderAntiAltas, resources.GetString("chkRenderAntiAltas.ToolTip"));
            this.chkRenderAntiAltas.UseVisualStyleBackColor = true;
            this.chkRenderAntiAltas.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // numFontGamma
            // 
            resources.ApplyResources(this.numFontGamma, "numFontGamma");
            this.numFontGamma.Maximum = 254;
            this.numFontGamma.Minimum = 1;
            this.numFontGamma.Name = "numFontGamma";
            this.numFontGamma.TickFrequency = 16;
            this.numFontGamma.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.numFontGamma.Value = 127;
            this.numFontGamma.ValueChanged += new System.EventHandler(this.numFontGamma_ValueChanged);
            // 
            // numCharSpacing
            // 
            resources.ApplyResources(this.numCharSpacing, "numCharSpacing");
            this.numCharSpacing.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numCharSpacing.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            -2147483648});
            this.numCharSpacing.Name = "numCharSpacing";
            this.toolTip1.SetToolTip(this.numCharSpacing, resources.GetString("numCharSpacing.ToolTip"));
            this.numCharSpacing.ValueChanged += new System.EventHandler(this.numLineSpacing_ValueChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // numLineSpacing
            // 
            resources.ApplyResources(this.numLineSpacing, "numLineSpacing");
            this.numLineSpacing.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numLineSpacing.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            -2147483648});
            this.numLineSpacing.Name = "numLineSpacing";
            this.toolTip1.SetToolTip(this.numLineSpacing, resources.GetString("numLineSpacing.ToolTip"));
            this.numLineSpacing.ValueChanged += new System.EventHandler(this.numLineSpacing_ValueChanged);
            // 
            // numFontSizePt
            // 
            this.numFontSizePt.DecimalPlaces = 2;
            this.numFontSizePt.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            resources.ApplyResources(this.numFontSizePt, "numFontSizePt");
            this.numFontSizePt.Maximum = new decimal(new int[] {
            72,
            0,
            0,
            0});
            this.numFontSizePt.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numFontSizePt.Name = "numFontSizePt";
            this.numFontSizePt.Value = new decimal(new int[] {
            2175,
            0,
            0,
            131072});
            this.numFontSizePt.ValueChanged += new System.EventHandler(this.numFontSizePt_ValueChanged);
            // 
            // btnChooseFontFile
            // 
            resources.ApplyResources(this.btnChooseFontFile, "btnChooseFontFile");
            this.btnChooseFontFile.Name = "btnChooseFontFile";
            this.btnChooseFontFile.UseVisualStyleBackColor = true;
            this.btnChooseFontFile.Click += new System.EventHandler(this.btnChooseFontFile_Click);
            // 
            // btnSelectFont
            // 
            resources.ApplyResources(this.btnSelectFont, "btnSelectFont");
            this.btnSelectFont.Name = "btnSelectFont";
            this.btnSelectFont.UseVisualStyleBackColor = true;
            this.btnSelectFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // lblFontSource
            // 
            this.lblFontSource.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.lblFontSource, "lblFontSource");
            this.lblFontSource.Name = "lblFontSource";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnDoGeneration
            // 
            resources.ApplyResources(this.btnDoGeneration, "btnDoGeneration");
            this.btnDoGeneration.Name = "btnDoGeneration";
            this.btnDoGeneration.UseVisualStyleBackColor = true;
            this.btnDoGeneration.Click += new System.EventHandler(this.btnDoGeneration_Click);
            // 
            // btnToggleResolution
            // 
            this.btnToggleResolution.Name = "btnToggleResolution";
            this.btnToggleResolution.Size = new System.Drawing.Size(120,30);
            this.btnToggleResolution.Location = new System.Drawing.Point(10,10);
            this.btnToggleResolution.Text = "切换到X3"
            resources.ApplyResources(this.btnToggleResolution, "btnToggleResolution");
            this.btnToggleResolution.UseVisualStyleBackColor = true;
            this.btnToggleResolution.Click += new System.EventHandler(this.btnToggleResolution_Click);
            // 
            // fontDialog
            // 
            this.fontDialog.AllowVerticalFonts = false;
            this.fontDialog.MaxSize = 72;
            this.fontDialog.MinSize = 8;
            // 
            // debounceTimer
            // 
            this.debounceTimer.Interval = 1;
            this.debounceTimer.Tick += new System.EventHandler(this.debounceTimer_Tick);
            // 
            // mnuAdvancedOptions
            // 
            this.mnuAdvancedOptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.chkShowBorderInBinaryFont,
            this.chkOldLineAlignment});
            this.mnuAdvancedOptions.Name = "mnuAdvancedOptions";
            resources.ApplyResources(this.mnuAdvancedOptions, "mnuAdvancedOptions");
            // 
            // chkShowBorderInBinaryFont
            // 
            this.chkShowBorderInBinaryFont.CheckOnClick = true;
            this.chkShowBorderInBinaryFont.Name = "chkShowBorderInBinaryFont";
            resources.ApplyResources(this.chkShowBorderInBinaryFont, "chkShowBorderInBinaryFont");
            this.chkShowBorderInBinaryFont.Click += new System.EventHandler(this.requireUpdatePreview);
            // 
            // chkOldLineAlignment
            // 
            this.chkOldLineAlignment.CheckOnClick = true;
            this.chkOldLineAlignment.Name = "chkOldLineAlignment";
            resources.ApplyResources(this.chkOldLineAlignment, "chkOldLineAlignment");
            this.chkOldLineAlignment.Click += new System.EventHandler(this.requireUpdatePreview);
            // 
            // previewSurface
            // 
            this.previewSurface.BackColor = System.Drawing.Color.Silver;
            this.previewSurface.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.previewSurface.CanvasSize = new System.Drawing.Size(100, 100);
            resources.ApplyResources(this.previewSurface, "previewSurface");
            this.previewSurface.Name = "previewSurface";
            this.previewSurface.ScaleMode = XTEinkToolkit.Controls.CanvasControl.RenderScaleMode.Zoom;
            // 
            // FrmMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "FrmMain";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontGamma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCharSpacing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLineSpacing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePt)).EndInit();
            this.mnuAdvancedOptions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.CanvasControl previewSurface;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnDoGeneration;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.LinkLabel btnPreview;
        private System.Windows.Forms.Label lblFontSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSelectFont;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numFontSizePt;
        private System.Windows.Forms.TrackBar numFontGamma;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkRenderGridFit;
        private System.Windows.Forms.CheckBox chkRenderAntiAltas;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numLineSpacing;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.Timer debounceTimer;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblPreviewMessage;
        private System.Windows.Forms.CheckBox chkLandspace;
        private System.Windows.Forms.RadioButton chkTraditionalChinese;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkVerticalFont;
        private System.Windows.Forms.Button btnChooseFontFile;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numCharSpacing;
        private System.Windows.Forms.RadioButton chkShowENCharacter;
        private System.Windows.Forms.CheckBox chkShowBorder;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.LinkLabel btnAdvancedOptions;
        private System.Windows.Forms.ContextMenuStrip mnuAdvancedOptions;
        private System.Windows.Forms.ToolStripMenuItem chkShowBorderInBinaryFont;
        private System.Windows.Forms.ToolStripMenuItem chkOldLineAlignment;
        private System.Windows.Forms.Button btnToggleResolution;
    }
}

