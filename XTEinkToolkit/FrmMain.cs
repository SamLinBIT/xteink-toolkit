using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XTEinkToolkit.Controls;
using XTEinkTools;

namespace XTEinkToolkit
{
    // TODO: 试试看负数行间距？
    // TODO: 试试看字符间距？
    // TODO: 添加单屏渲染字数量，用于估算翻页加载事件。
    public partial class FrmMain : Form
    {

        private PrivateFontCollection privateFont;

        public FrmMain()
        {
            InitializeComponent();
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch {
                this.Icon = SystemIcons.Application;
            }
        }

        private static Size XTScreenSize = new Size(480, 800);

        private static Size SwapDirection(Size s)
        {
            return new Size(s.Height, s.Width);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            previewSurface.ScaleMode = XTEinkToolkit.Controls.CanvasControl.RenderScaleMode.PreferCenter;
            previewSurface.CanvasSize = new System.Drawing.Size(480, 800);
            chkTraditionalChinese.Checked = FrmMainCodeString.boolShowTCPreview.Contains("true");
            DoPreview();
        }

        private void btnSelectFont_Click(object sender, EventArgs e)
        {
            fontDialog.Font = lblFontSource.Font;
           
            if (fontDialog.ShowDialog(this) == DialogResult.OK)
            {
                lblFontSource.Font = fontDialog.Font;
                lblFontSource.Text = fontDialog.Font.Name + "\r\n"+FrmMainCodeString.abcFontPreviewText;
                numFontSizePt.ValueChanged -= numFontSizePt_ValueChanged;
                numFontSizePt.Value = (decimal)fontDialog.Font.Size;
                numFontSizePt.ValueChanged += numFontSizePt_ValueChanged;

            }
            DoPreview();
        }


        private void btnChooseFontFile_Click(object sender, EventArgs e)
        {
            if(DlgSelectCustomFont.ShowSelectDialog(this,out var pfc,out var fnt))
            {
                lblFontSource.Font = fnt;
                privateFont?.Dispose();
                privateFont = pfc;

                lblFontSource.Text = fontDialog.Font.Name + "\r\n" + FrmMainCodeString.abcFontPreviewText;
                numFontSizePt.ValueChanged -= numFontSizePt_ValueChanged;
                numFontSizePt.Value = (decimal)lblFontSource.Font.Size;
                numFontSizePt.ValueChanged += numFontSizePt_ValueChanged;
                DoPreview();
            }
        }

        private void btnPreview_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DoPreviewDirect();
        }

        private void numFontSizePt_ValueChanged(object sender, EventArgs e)
        {
            float newSize = (float)numFontSizePt.Value;
            Font oldFont = lblFontSource.Font;
            lblFontSource.Font = new Font(oldFont.FontFamily, newSize, oldFont.Style);
            oldFont.Dispose();
            DoPreview();
        }
        private void numFontGamma_ValueChanged(object sender, EventArgs e)
        {
            DoPreview();
        }

        private void numLineSpacing_ValueChanged(object sender, EventArgs e)
        {

            DoPreview();
        }


        private void chkRenderGridFit_CheckedChanged(object sender, EventArgs e)
        {
            numFontGamma.Enabled = chkRenderAntiAltas.Checked;
            DoPreview();
        }


        void DoPreview()
        {
            debounceCd = 18;
            debounceTimer.Enabled = true;
        }

        private int debounceCd = 0;
        private void debounceTimer_Tick(object sender, EventArgs e)
        {
            debounceCd--;
            if (debounceCd < 0)
            {
                debounceTimer.Enabled = false;
                DoPreviewDirect();
            }
        }
        string previewStringSC = Properties.Resources.previewTexts;
        string previewStringTC = Properties.Resources.previewTestTC;
        void DoPreviewDirect()
        {
            debounceTimer.Enabled = false;
            btnPreview.Enabled = false;
            btnPreview.Text = FrmMainCodeString.abcRenderingPreview;
            Application.DoEvents();
            using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
            {
                ConfigureRenderer(renderer);
                Size fontRenderSize = renderer.GetFontRenderSize();

                var screenSize = XTScreenSize;
                var rotatedScreenSize = chkLandspace.Checked ? SwapDirection(screenSize) : screenSize;

                var previewSize = rotatedScreenSize;
                if (chkVerticalFont.Checked)
                {
                    previewSize = SwapDirection(previewSize);
                }

                previewSurface.CanvasSize = previewSize; ;
                XTEinkFontBinary fontBinary = new XTEinkFontBinary(fontRenderSize.Width, fontRenderSize.Height);
                var g = previewSurface.GetGraphics();
                g.ResetTransform();
                if (chkVerticalFont.Checked) {
                    g.TranslateTransform(previewSize.Width,0);
                    g.RotateTransform(90);
                }
                Utility.RenderPreview(chkTraditionalChinese.Checked ? previewStringTC : previewStringSC, fontBinary, renderer, g, rotatedScreenSize);
                previewSurface.Commit();
            }
            GC.Collect();
            btnPreview.Enabled = true;
            btnPreview.Text = FrmMainCodeString.abcBtnPreviewText;
        }

        private void ConfigureRenderer(XTEinkFontRenderer renderer)
        {
            renderer.Font = lblFontSource.Font;
            renderer.LineSpacingPx = (int)numLineSpacing.Value;
            renderer.LightThrehold = numFontGamma.Value;
            renderer.IsVerticalFont = chkVerticalFont.Checked;
            XTEinkFontRenderer.AntiAltasMode[] aaModesEnum = new XTEinkFontRenderer.AntiAltasMode[] {
                    XTEinkFontRenderer.AntiAltasMode.System1Bit, // 0x0
                    XTEinkFontRenderer.AntiAltasMode.System1BitGridFit, // 0x1
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltas, // 0x2
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltasGridFit //0x3
                };
            var whichAAMode = (chkRenderAntiAltas.Checked ? 2 : 0) + (chkRenderGridFit.Checked ? 1 : 0);
            renderer.AAMode = aaModesEnum[whichAAMode];
        }

        private string GetRenderTargetSize()
        {
            using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
            {

                ConfigureRenderer(renderer);
                Size fontRenderSize = renderer.GetFontRenderSize();
                return fontRenderSize.Width+"×"+fontRenderSize.Height;
            }
        }

        private void btnDoGeneration_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = (FrmMainCodeString.abcSaveDialogTypeName.Trim())+"|*." + GetRenderTargetSize() + ".bin";
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string savePath = sfd.FileName;
            btnDoGeneration.Enabled = false;
            EssentialDialogs.ProgressDialog.RunWork(this, (ps) =>
            {
                var renderingMsg = FrmMainCodeString.abcRenderingFont;
                ps.SetMessage(renderingMsg);
                using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
                {
                    Invoke(new Action(() =>
                    {
                        ConfigureRenderer(renderer);
                    }));
                    Size fontRenderSize = renderer.GetFontRenderSize();

                    XTEinkFontBinary fontBinary = new XTEinkFontBinary(fontRenderSize.Width, fontRenderSize.Height);
                    var maxCharRange = 65536;
                    for (int i = 0; i < maxCharRange; i++)
                    {
                        try
                        {
                            ps.SetProgress(i, maxCharRange);
                            ps.SetMessage($"{renderingMsg}({i}/{maxCharRange})");
                            renderer.RenderFont(i, fontBinary);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    using(var stream = File.Create(savePath))
                    {
                        fontBinary.saveToFile(stream);
                    }
                }

            }, (err) =>
            {
                btnDoGeneration.Enabled = true;
                if (err != null)
                {
                    MessageBox.Show(this, FrmMainCodeString.abcRenderingError+"：\r\n" + err.GetType().FullName + ": " + err.Message, FrmMainCodeString.abcRenderingError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show(this, FrmMainCodeString.abcSuccessDialogMsg, FrmMainCodeString.abcSuccessDialogTitle, MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            });
        }


    }
}
