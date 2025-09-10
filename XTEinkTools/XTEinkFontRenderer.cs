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

        // SuperSampling字体缓存
        private Font _cachedSuperSamplingFont;
        private float _cachedSuperSamplingSize;
        private SuperSamplingMode _cachedSuperSamplingMode;

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

            // ===== 1. 超采样前：渲染大字 ===================================
            this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; // 最高质量插值
            this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            // 注意：TextRenderingHint 通过 syncSettings() 根据用户AAMode动态设置
        }

        /// <summary>
        /// 应用SuperSampling缩放处理
        /// SuperSampling分为两个阶段：
        /// 1. 字体渲染阶段：在高分辨率画布上渲染字体（需要抗锯齿）
        /// 2. 缩放阶段：将高分辨率位图缩放到目标尺寸（避免过度平滑）
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
                    // ===== 2. 超采样后：缩图回目标尺寸 ==============================
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // 最邻域=最锐利
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // 避免半像素偏移

                    // 使用NearestNeighbor保持最大锐利度，最小化亮度损失
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

                // 始终按用户选择的抗锯齿模式设置，让用户完全控制渲染行为
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

            // 创建缩放后的字体（如果需要），使用缓存避免重复创建
            Font renderFont = this.Font;
            if (SuperSampling != SuperSamplingMode.None)
            {
                float scaledSize = this.Font.Size * scale;

                // 检查缓存是否有效
                if (_cachedSuperSamplingFont == null ||
                    _cachedSuperSamplingSize != scaledSize ||
                    _cachedSuperSamplingMode != SuperSampling ||
                    !_cachedSuperSamplingFont.FontFamily.Equals(this.Font.FontFamily) ||
                    _cachedSuperSamplingFont.Style != this.Font.Style)
                {
                    // 缓存无效，创建新字体并更新缓存
                    _cachedSuperSamplingFont?.Dispose();
                    _cachedSuperSamplingFont = new Font(this.Font.FontFamily, scaledSize, this.Font.Style);
                    _cachedSuperSamplingSize = scaledSize;
                    _cachedSuperSamplingMode = SuperSampling;
                }

                renderFont = _cachedSuperSamplingFont;
            }

            // 绘制字符
            this._tempGraphics.DrawString(chr.ToString(), renderFont, Brushes.White, 0, 0, _format);

            // SuperSampling处理流程
            if (SuperSampling != SuperSamplingMode.None)
            {
                using (Bitmap scaledBitmap = ApplySuperSampling(_tempRenderSurface, renderer.Width, renderer.Height))
                {
                    // SuperSampling模式下，对特殊字符进行算法优化
                    int optimizedThreshold = CalculateOptimizedThreshold(scaledBitmap, this.LightThrehold, charCodePoint);
                    renderer.LoadFromBitmap(charCodePoint, scaledBitmap, 0, 0, optimizedThreshold);
                }
            }
            else
            {
                // 无SuperSampling，直接使用用户阈值
                renderer.LoadFromBitmap(charCodePoint, _tempRenderSurface, 0, 0, this.LightThrehold);
            }
        }

        /// <summary>
        /// 为SuperSampling模式计算优化的二值化阈值
        /// 对标点符号和复杂字符进行专门优化，但不依赖用户权重
        /// </summary>
        /// <param name="bitmap">SuperSampling处理后的位图</param>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <param name="charCodePoint">当前字符的Unicode码点</param>
        /// <returns>优化后的阈值</returns>
        private int CalculateOptimizedThreshold(Bitmap bitmap, int userThreshold, int charCodePoint)
        {
            try
            {
                // 检查字符类型
                bool isPunctuation = IsPunctuationCharacter(charCodePoint);
                bool isComplexCharacter = IsComplexCharacter(charCodePoint);

                // 分析图像像素分布
                var histogram = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;
                int nonBlackPixels = 0;

                // 统计灰度分布 - 使用快速像素访问
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)bitmapData.Scan0;
                        int stride = bitmapData.Stride;

                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            byte* row = ptr + (y * stride);
                            for (int x = 0; x < bitmap.Width; x++)
                            {
                                // BGR格式
                                int b = row[x * 3];
                                int g = row[x * 3 + 1];
                                int r = row[x * 3 + 2];

                                int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                                histogram[gray]++;
                                if (gray > 0) nonBlackPixels++;
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // 如果图像主要是黑色（背景），直接使用用户阈值
                if (nonBlackPixels < totalPixels * 0.05)
                {
                    return userThreshold;
                }

                // 标点符号特殊处理：保持锐利清晰
                if (isPunctuation)
                {
                    int scale = (int)SuperSampling;
                    // 标点符号使用稍微偏高的阈值，确保清晰锐利
                    int punctuationThreshold = userThreshold + (scale - 1) * 8;
                    return Math.Min(220, Math.Max(userThreshold, punctuationThreshold));
                }

                // 复杂字符特殊处理：保留更多细节
                if (isComplexCharacter)
                {
                    // 对复杂字符使用Otsu算法，但偏向保留细节
                    int otsuThreshold = CalculateOtsuThreshold(histogram, totalPixels);
                    int scale = (int)SuperSampling;

                    // 复杂字符倾向于使用稍低的阈值来保留细节
                    int complexThreshold = (int)(otsuThreshold * 0.85 + userThreshold * 0.15);
                    complexThreshold -= (scale - 1) * 3; // SuperSampling级别越高，阈值稍微降低

                    return Math.Max(32, Math.Min(200, complexThreshold));
                }

                // 普通字符：使用用户阈值，但补偿SuperSampling导致的亮度损失
                int compensatedThreshold = CompensateForSuperSamplingBrightening(userThreshold);
                return compensatedThreshold;
            }
            catch
            {
                // 如果计算失败，回退到用户阈值
                return userThreshold;
            }
        }

        /// <summary>
        /// 判断字符是否为标点符号
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <returns>是否为标点符号</returns>
        private bool IsPunctuationCharacter(int charCodePoint)
        {
            // ASCII标点符号
            if ((charCodePoint >= 0x0021 && charCodePoint <= 0x002F) ||  // !"#$%&'()*+,-./
                (charCodePoint >= 0x003A && charCodePoint <= 0x0040) ||  // :;<=>?@
                (charCodePoint >= 0x005B && charCodePoint <= 0x0060) ||  // [\]^_`
                (charCodePoint >= 0x007B && charCodePoint <= 0x007E))    // {|}~
            {
                return true;
            }

            // 常用Unicode标点符号范围
            if ((charCodePoint >= 0x2000 && charCodePoint <= 0x206F) ||  // 通用标点
                (charCodePoint >= 0x3000 && charCodePoint <= 0x303F) ||  // CJK标点
                (charCodePoint >= 0xFF00 && charCodePoint <= 0xFFEF))    // 全角标点
            {
                return true;
            }

            // 使用.NET内置的标点判断作为补充
            try
            {
                if (charCodePoint <= 0xFFFF)
                {
                    char ch = (char)charCodePoint;
                    return char.IsPunctuation(ch) || char.IsSymbol(ch);
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 判断字符是否为复杂字符（基于Unicode范围和已知复杂字符）
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <returns>是否为复杂字符</returns>
        private bool IsComplexCharacter(int charCodePoint)
        {
            // 一些已知的特别复杂的汉字
            int[] complexChars = {
                0x526A, // 剪
                0x9F52, // 齒
                0x9F61, // 齡
                0x8B9E, // 謞
                0x8B93, // 讓
                0x9EBC, // 麼
                0x7E41, // 繁
                0x9F77, // 靷
                0x9F72, // 靲
                0x9F78, // 靸
                0x8056, // 聖
                0x9F6C, // 齬
                0x9F50, // 齐
                0x8B4F  // 譏
            };

            // 检查是否为已知复杂字符
            for (int i = 0; i < complexChars.Length; i++)
            {
                if (charCodePoint == complexChars[i])
                    return true;
            }

            // 一些复杂字符的Unicode范围
            // CJK统一汉字扩展A区 (U+3400-U+4DBF) - 通常比较复杂
            if (charCodePoint >= 0x3400 && charCodePoint <= 0x4DBF)
                return true;

            // CJK兼容汉字 (U+F900-U+FAFF) - 通常是复杂的异体字
            if (charCodePoint >= 0xF900 && charCodePoint <= 0xFAFF)
                return true;

            return false;
        }

        /// <summary>
        /// 使用Otsu算法计算最佳二值化阈值
        /// </summary>
        /// <param name="histogram">像素灰度直方图</param>
        /// <param name="totalPixels">总像素数</param>
        /// <returns>最佳阈值</returns>
        private int CalculateOtsuThreshold(int[] histogram, int totalPixels)
        {
            double sum = 0;
            for (int i = 0; i < 256; i++)
                sum += i * histogram[i];

            double sumB = 0;
            int wB = 0;
            double maximum = 0.0;
            int threshold = 0;

            for (int i = 0; i < 256; i++)
            {
                wB += histogram[i];
                if (wB == 0) continue;

                int wF = totalPixels - wB;
                if (wF == 0) break;

                sumB += i * histogram[i];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;
                double between = wB * wF * (mB - mF) * (mB - mF);

                if (between > maximum)
                {
                    maximum = between;
                    threshold = i;
                }
            }

            return threshold;
        }

        /// <summary>
        /// 补偿SuperSampling导致的亮度损失
        /// SuperSampling缩放过程会让字体变浅，需要降低阈值来补偿
        /// </summary>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <returns>补偿后的阈值</returns>
        private int CompensateForSuperSamplingBrightening(int userThreshold)
        {
            int scale = (int)SuperSampling;

            // 根据SuperSampling级别计算补偿量
            // 级别越高，亮度损失越明显，需要更多补偿
            int compensation = 0;
            switch (SuperSampling)
            {
                case SuperSamplingMode.x2:
                    compensation = 15;  // 2倍采样：轻微补偿
                    break;
                case SuperSamplingMode.x4:
                    compensation = 25;  // 4倍采样：中等补偿
                    break;
                case SuperSamplingMode.x8:
                    compensation = 35;  // 8倍采样：较大补偿
                    break;
                default:
                    compensation = 0;
                    break;
            }

            // 应用补偿：降低阈值让更多像素保持黑色
            int compensatedThreshold = userThreshold - compensation;

            // 防止阈值过低导致过暗
            compensatedThreshold = Math.Max(16, compensatedThreshold);

            // 防止阈值过高导致过亮
            compensatedThreshold = Math.Min(240, compensatedThreshold);

            return compensatedThreshold;
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
            try
            {
                _cachedSuperSamplingFont?.Dispose();
            }
            catch { }
        }
    }
}
