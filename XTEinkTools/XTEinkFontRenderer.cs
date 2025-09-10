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

        /// <summary>
        /// SuperSampling模式下用户阈值的权重，范围0.0-1.0
        /// 值越高越接近用户设置，值越低越依赖自动算法
        /// 默认0.3，表示用户阈值占70%权重
        /// </summary>
        public double SuperSamplingUserWeight { get; set; } = 0.3;

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

            // 字体渲染是一次性离线工作，统一使用最高质量设置
            this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
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
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // 使用Bicubic算法更好地保留字体曲线和抗锯齿细节
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

            // 应用SuperSampling处理
            if (SuperSampling != SuperSamplingMode.None && IsSupersamplingWorthwhile(renderer.Width, renderer.Height))
            {
                using (Bitmap scaledBitmap = ApplySuperSampling(_tempRenderSurface, renderer.Width, renderer.Height))
                {
                    // SuperSampling模式使用优化的阈值策略，保留更多灰度细节
                    int optimizedThreshold = CalculateOptimizedThreshold(scaledBitmap, this.LightThrehold, charCodePoint);
                    renderer.LoadFromBitmap(charCodePoint, scaledBitmap, 0, 0, optimizedThreshold);
                }
            }
            else
            {
                // 无SuperSampling或字符过小，使用原有逻辑
                renderer.LoadFromBitmap(charCodePoint, _tempRenderSurface, 0, 0, this.LightThrehold);
            }
        }

        /// <summary>
        /// 为SuperSampling模式计算优化的二值化阈值
        /// 分析图像特征，保留更多抗锯齿细节，对标点符号特殊处理
        /// </summary>
        /// <param name="bitmap">SuperSampling处理后的位图</param>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <param name="charCodePoint">当前字符的Unicode码点</param>
        /// <returns>优化后的阈值</returns>
        private int CalculateOptimizedThreshold(Bitmap bitmap, int userThreshold, int charCodePoint)
        {
            try
            {
                // 检查是否为标点符号
                bool isPunctuation = IsPunctuationCharacter(charCodePoint);

                // 分析图像像素分布
                var histogram = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;
                int nonBlackPixels = 0;
                int uniqueGrayLevels = 0;

                // 统计灰度分布 - 使用快速像素访问
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)bitmapData.Scan0;
                        int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;

                        for (int i = 0; i < bytes; i += 3)
                        {
                            // BGR格式
                            int b = ptr[i];
                            int g = ptr[i + 1];
                            int r = ptr[i + 2];

                            int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                            histogram[gray]++;
                            if (gray > 0) nonBlackPixels++;
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // 计算实际的灰度级别数量
                for (int i = 0; i < 256; i++)
                {
                    if (histogram[i] > 0) uniqueGrayLevels++;
                }

                // 如果图像主要是黑色（背景），或者只有很少的灰度级别（可能是1bit模式），使用用户阈值
                if (nonBlackPixels < totalPixels * 0.1 || uniqueGrayLevels < 8)
                {
                    return userThreshold;
                }

                // 标点符号使用保守的阈值策略，避免模糊
                if (isPunctuation)
                {
                    // 对标点符号，主要使用用户阈值，稍微调整以保持清晰
                    int scale = (int)SuperSampling;
                    // 标点符号倾向于使用更高的阈值来保持锐利
                    int conservativeThreshold = userThreshold + (scale - 1) * 5;
                    return Math.Min(220, Math.Max(userThreshold, conservativeThreshold));
                }

                // 检测字符复杂度（针对复杂汉字如"剪"字的细节丢失问题）
                bool isComplexCharacter = IsComplexCharacter(charCodePoint, histogram, totalPixels);

                // 对普通文字字符使用改进的Otsu算法
                int otsuThreshold = CalculateOtsuThreshold(histogram, totalPixels);

                // 复杂字符优先使用用户阈值，保留细节
                double userWeight = SuperSamplingUserWeight;
                if (isComplexCharacter)
                {
                    // 复杂字符（如剪、繁体字）提高用户权重到85%，减少自动算法干预
                    userWeight = Math.Max(userWeight, 0.85);
                }

                // 根据SuperSampling级别调整策略，但对复杂字符更保守
                int scale2 = (int)SuperSampling;
                double adjustmentFactor = isComplexCharacter
                    ? 1.0 - (scale2 - 1) * 0.05  // 复杂字符：减少阈值降低幅度
                    : 1.0 - (scale2 - 1) * 0.1;  // 普通字符：原有逻辑

                // 使用用户可配置的权重混合阈值
                double otsuWeight = 1.0 - userWeight;
                int optimizedThreshold = (int)(otsuThreshold * adjustmentFactor * otsuWeight + userThreshold * userWeight);

                // 扩大阈值范围，给复杂字符更多空间
                int minThreshold = isComplexCharacter ? Math.Max(userThreshold - 20, 16) : 32;
                int maxThreshold = isComplexCharacter ? 240 : 192;

                return Math.Max(minThreshold, Math.Min(maxThreshold, optimizedThreshold));
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
            if ((charCodePoint >= 0x0020 && charCodePoint <= 0x002F) ||  // 空格和基本标点
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
                char ch = (char)charCodePoint;
                return char.IsPunctuation(ch) || char.IsSymbol(ch);
            }
            catch
            {
                return false;
            }
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
        /// 判断字符是否为复杂字符（如"剪"字）
        /// 复杂字符需要更保守的阈值处理以保留细节
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <param name="histogram">字符图像的灰度直方图</param>
        /// <param name="totalPixels">总像素数</param>
        /// <returns>是否为复杂字符</returns>
        private bool IsComplexCharacter(int charCodePoint, int[] histogram, int totalPixels)
        {
            try
            {
                // 1. 基于Unicode范围的预判断
                if (IsKnownComplexCharacter(charCodePoint))
                {
                    return true;
                }

                // 2. 分析灰度分布复杂度
                int nonZeroLevels = 0;
                int midGrayPixels = 0; // 中间灰度像素数（64-192范围）

                for (int i = 0; i < 256; i++)
                {
                    if (histogram[i] > 0)
                    {
                        nonZeroLevels++;
                        if (i >= 64 && i <= 192)
                        {
                            midGrayPixels += histogram[i];
                        }
                    }
                }

                // 3. 复杂度判断条件
                // 条件1：灰度级别多（表示细节丰富）
                bool hasRichGrayLevels = nonZeroLevels > 12;

                // 条件2：中间灰度比例高（表示抗锯齿边缘多，笔画复杂）
                double midGrayRatio = (double)midGrayPixels / totalPixels;
                bool hasComplexEdges = midGrayRatio > 0.15;

                // 条件3：非零像素密度适中（太少是简单符号，太多是粗体，适中是复杂结构）
                int nonBlackPixels = totalPixels - histogram[0];
                double pixelDensity = (double)nonBlackPixels / totalPixels;
                bool hasModerateDensity = pixelDensity > 0.1 && pixelDensity < 0.6;

                // 满足2个或以上条件认为是复杂字符
                int complexityScore = (hasRichGrayLevels ? 1 : 0) +
                                    (hasComplexEdges ? 1 : 0) +
                                    (hasModerateDensity ? 1 : 0);

                return complexityScore >= 2;
            }
            catch
            {
                return false; // 分析失败时保守处理
            }
        }

        /// <summary>
        /// 判断是否为已知的复杂字符
        /// </summary>
        /// <param name="charCodePoint">字符码点</param>
        /// <returns>是否为已知复杂字符</returns>
        private bool IsKnownComplexCharacter(int charCodePoint)
        {
            // 一些已知的特别复杂的汉字
            var complexChars = new int[] {
                0x526A, // 剪
                0x9F52, // 齒
                0x9F61, // 齡
                0x8B9E, // 謞
                0x8B93, // 讓
                0x9EBC, // 麼
                0x9F52, // 齒
                0x7E41, // 繁
                0x9F77, // 靷
                0x9F72, // 靲
                0x9F78, // 靸
                0x8056  // 聖
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
        /// 判断字符是否值得进行SuperSampling处理
        /// 对于过小的字符，SuperSampling效果有限且浪费性能
        /// </summary>
        /// <param name="width">字符宽度</param>
        /// <param name="height">字符高度</param>
        /// <returns>是否值得SuperSampling</returns>
        private bool IsSupersamplingWorthwhile(int width, int height)
        {
            // 字符面积过小时SuperSampling效果有限
            int area = width * height;
            if (area < 64) // 小于8x8像素的字符
                return false;

            // 单边过小的字符也跳过（如细线条）
            if (width < 6 || height < 6)
                return false;

            return true;
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
