using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTEinkTools
{
    /// <summary>
    /// 超采样模式
    /// </summary>
    public enum SuperSamplingMode
    {
        None = 1,        // 无超采样
        x2 = 2,          // 2倍超采样
        x4 = 4,          // 4倍超采样
        x8 = 8           // 8倍超采样（高质量模式）
    }

    /// <summary>
    /// 字形渲染器
    /// </summary>
    public class XTEinkFontRenderer : IDisposable
    {

        public enum AntiAltasMode
        {
            System1BitGridFit,
            System1Bit,
            SystemAntiAltasGridFit,
            SystemAntiAltas,
        }

        public AntiAltasMode AAMode { get; set; } = AntiAltasMode.System1BitGridFit;

        public int LightThrehold { get; set; } = 128;
        public int LineSpacingPx { get; set; } = 0;
        public int CharSpacingPx { get; set; } = 0;
        public bool IsVerticalFont { get; set; } = false;
        public Font Font { get; set; } = SystemFonts.DefaultFont;
        public bool IsOldLineAlignment { get; set; }
        public bool RenderBorder { get; set; }

        /// <summary>
        /// 超采样模式，默认为None（无超采样）
        /// </summary>
        public SuperSamplingMode SuperSampling { get; set; } = SuperSamplingMode.None;

        private Bitmap _tempRenderSurface;
        private Graphics _tempGraphics;

        public delegate void RenderMethod(int x, int y, bool pixel);

        public Size GetFontRenderSize()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                bmp.SetResolution(96, 96);
                using(Graphics g = Graphics.FromImage(bmp))
                {
                    StringFormat strfmt = new StringFormat(StringFormat.GenericTypographic);
                    // strfmt.FormatFlags |= StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip;
                    SizeF sf = g.MeasureString("坐", this.Font,999,strfmt); 
                    //if (IsVerticalFont)
                    //{
                    //    sf = new SizeF(sf.Height, sf.Width);
                    //}
                    Size s = new Size((int)Math.Round(sf.Width) + this.CharSpacingPx, (int)Math.Round(sf.Height) + this.LineSpacingPx);
                    if(s.Height < 5)
                    {
                        s.Height = 5;
                        LineSpacingPx = (int)(sf.Height - 5);
                    }
                    if (s.Width < 5)
                    {
                        s.Width = 5;
                        CharSpacingPx = (int)(sf.Width - 5);
                    }
                    return s;
                }
            }

        }

        StringFormat _format = new StringFormat(StringFormat.GenericTypographic)
        {
        };

        private void ensureRenderSurfaceSize(int width,int height)
        {
            // 计算SuperSampling所需的实际尺寸
            int actualWidth = width;
            int actualHeight = height;

            if (SuperSampling != SuperSamplingMode.None)
            {
                int scale = (int)SuperSampling;
                actualWidth = width * scale;
                actualHeight = height * scale;
            }

            if (this._tempRenderSurface != null && this._tempGraphics != null) {
                if (this._tempRenderSurface.Width == actualWidth && this._tempRenderSurface.Height == actualHeight)
                {
                    return;
                }
            }

            try
            {
                this._tempGraphics?.Dispose();
            }
            catch { }
            try
            {
                this._tempRenderSurface?.Dispose();
            }catch { }

            this._tempRenderSurface = new Bitmap(actualWidth, actualHeight);
            this._tempRenderSurface.SetResolution(96, 96);
            this._tempGraphics = Graphics.FromImage(this._tempRenderSurface);
            this._tempGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            // SuperSampling模式下使用专门优化的渲染选项
            if (SuperSampling != SuperSamplingMode.None)
            {
                this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            }
            else
            {
                // 普通模式：同样追求高质量，为小屏幕优化每个像素
                this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            }
        }

        /// <summary>
        /// 应用SuperSampling缩放处理
        /// </summary>
        /// <param name="sourceBitmap">源位图</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>缩放后的位图</returns>
        private Bitmap ApplySuperSampling(Bitmap sourceBitmap, int targetWidth, int targetHeight)
        {
            if (SuperSampling == SuperSamplingMode.None)
            {
                return sourceBitmap; // 无需处理，直接返回原图
            }

            try
            {
                // 创建目标尺寸的位图
                Bitmap targetBitmap = new Bitmap(targetWidth, targetHeight);
                targetBitmap.SetResolution(96, 96);

                using (Graphics g = Graphics.FromImage(targetBitmap))
                {
                    // 设置专门针对文字优化的缩放选项
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // 使用适合文字的缩放算法
                    g.DrawImage(sourceBitmap,
                        new Rectangle(0, 0, targetWidth, targetHeight),
                        new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                        GraphicsUnit.Pixel);
                }

                return targetBitmap;
            }
            catch
            {
                // 如果SuperSampling失败，则返回原图
                return sourceBitmap;
            }
        }

        private void syncSettings()
        {
            if (this._tempGraphics != null) {
                TextRenderingHint targetHint = TextRenderingHint.SingleBitPerPixelGridFit;

                // 始终按用户选择的抗锯齿模式设置，SuperSampling不干预此选择
                switch (AAMode)
                {
                    case AntiAltasMode.System1BitGridFit:
                        targetHint = TextRenderingHint.SingleBitPerPixelGridFit; break;
                    case AntiAltasMode.System1Bit:
                        targetHint = TextRenderingHint.SingleBitPerPixel; break;
                    case AntiAltasMode.SystemAntiAltasGridFit:
                        targetHint = TextRenderingHint.AntiAliasGridFit; break;
                    case AntiAltasMode.SystemAntiAltas:
                        targetHint = TextRenderingHint.AntiAlias; break;
                }

                if(this._tempGraphics.TextRenderingHint != targetHint)
                {
                    this._tempGraphics.TextRenderingHint = targetHint;
                }
                if (IsVerticalFont)
                {
                    _format.FormatFlags |= StringFormatFlags.DirectionVertical;
                }
            }
            
        }

        public void RenderFont(int charCodePoint,XTEinkFontBinary renderer)
        {
            ensureRenderSurfaceSize(renderer.Width, renderer.Height);
            syncSettings();
            char chr = (char)charCodePoint;

            // 计算SuperSampling缩放比例
            int scale = (int)SuperSampling;

            // 清空画布
            this._tempGraphics.Clear(Color.Black);
            this._tempGraphics.ResetTransform();

            // 绘制边框（考虑缩放）
            if (RenderBorder)
            {
                int borderWidth = (renderer.Width * scale) - 1;
                int borderHeight = (renderer.Height * scale) - 1;
                this._tempGraphics.DrawRectangle(Pens.White, 0, 0, borderWidth, borderHeight);
            }

            // 处理垂直字体变换（考虑缩放）
            if (IsVerticalFont)
            {
                this._tempGraphics.TranslateTransform(0, renderer.Height * scale);
                this._tempGraphics.RotateTransform(-90);
            }

            // 处理行对齐（考虑缩放）
            bool shouldSetLineAlignmentToCenter = true;
            if (IsOldLineAlignment)
            {
                shouldSetLineAlignmentToCenter = LineSpacingPx < 0;
            }
            if (shouldSetLineAlignmentToCenter)
            {
                if (IsVerticalFont)
                {
                    this._tempGraphics.TranslateTransform((LineSpacingPx * scale) / 2, 0);
                }
                else
                {
                    this._tempGraphics.TranslateTransform(0, (LineSpacingPx * scale) / 2);
                }
            }

            // 处理字符间距（仅对非ASCII字符，考虑缩放）
            if (CharSpacingPx != 0 && charCodePoint > 255)
            {
                if (IsVerticalFont)
                {
                    this._tempGraphics.TranslateTransform(0, (CharSpacingPx * scale) / 2);
                }
                else
                {
                    this._tempGraphics.TranslateTransform((CharSpacingPx * scale) / 2, 0);
                }
            }

            // 创建缩放后的字体（如果需要）
            Font renderFont = this.Font;
            if (SuperSampling != SuperSamplingMode.None)
            {
                float scaledSize = this.Font.Size * scale;
                renderFont = new Font(this.Font.FontFamily, scaledSize, this.Font.Style);
            }

            // 绘制字符
            this._tempGraphics.DrawString(chr.ToString(), renderFont, Brushes.White, 0, 0, _format);

            // 应用SuperSampling处理
            if (SuperSampling != SuperSamplingMode.None)
            {
                using (Bitmap scaledBitmap = ApplySuperSampling(_tempRenderSurface, renderer.Width, renderer.Height))
                {
                    renderer.LoadFromBitmap(charCodePoint, scaledBitmap, 0, 0, this.LightThrehold);
                }

                // 释放缩放字体资源
                if (renderFont != this.Font)
                {
                    renderFont.Dispose();
                }
            }
            else
            {
                // 无SuperSampling，使用原有逻辑
                renderer.LoadFromBitmap(charCodePoint, _tempRenderSurface, 0, 0, this.LightThrehold);
            }
        }

        void IDisposable.Dispose()
        {
            try
            {
                this._tempGraphics?.Dispose();
            }
            catch { }
            try
            {
                this._tempRenderSurface?.Dispose();
            }
            catch { }

        }
    }
}
