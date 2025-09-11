using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XTEinkTools
{
    // 注意：SuperSampling现在使用bool控制，true=256x终极超采样，false=无超采样
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
        public bool EnableUltimateSuperSampling { get; set; } = false;
        public bool EnableSubPixelHinting { get; set; } = true;
        public float SubPixelHintingStrength { get; set; } = 0.8f;

        #region private fields
        private Bitmap _tempRenderSurface;
        private Graphics _tempGraphics;
        private readonly StringFormat _format = new(StringFormat.GenericTypographic);
        private const int ULTRA_SCALE = 32;
        private const int BAYER_SIZE = 16;

        // 性能优化：预计算查找表
        private static readonly float[] GammaToLinearLUT = new float[256];
        private static readonly byte[] LinearToGammaLUT = new byte[65536]; // 16位精度
        private static readonly float[] BayerLUT = new float[BAYER_SIZE * BAYER_SIZE];

        // 整数Bayer抖动优化（16位定点数，精度0.0001）
        private static readonly int[] BayerLUTInt = new int[BAYER_SIZE * BAYER_SIZE];
        private static readonly int[] GammaToLinearLUTInt = new int[256];
        private const int FIXED_POINT_SCALE = 10000; // 定点数缩放因子

        // 浮点精度超采样优化（32位浮点数，更高精度）
        private static readonly float[] BayerLUTFloat = new float[BAYER_SIZE * BAYER_SIZE];
        private static readonly float[] GammaToLinearLUTFloat = new float[256];

        // 内存池
        private static readonly ConcurrentQueue<Bitmap> _bitmapPool = new();
        private static readonly object _poolLock = new object();

        // StringFormat对象复用
        private static readonly StringFormat _horizontalFormat = new(StringFormat.GenericTypographic);
        private static readonly StringFormat _verticalFormat = new(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.DirectionVertical
        };
        private static readonly int[,] BayerMatrix16x16 = {
            {0,128,32,160,8,136,40,168,2,130,34,162,10,138,42,170},
            {192,64,224,96,200,72,232,104,194,66,226,98,202,74,234,106},
            {48,176,16,144,56,184,24,152,50,178,18,146,58,186,26,154},
            {240,112,208,80,248,120,216,88,242,114,210,82,250,122,218,90},
            {12,140,44,172,4,132,36,164,14,142,46,174,6,134,38,166},
            {204,76,236,108,196,68,228,100,206,78,238,110,198,70,230,102},
            {60,188,28,156,52,180,20,148,62,190,30,158,54,182,22,150},
            {252,124,220,92,244,116,212,84,254,126,222,94,246,118,214,86},
            {3,131,35,163,11,139,43,171,1,129,33,161,9,137,41,169},
            {195,67,227,99,203,75,235,107,193,65,225,97,201,73,233,105},
            {51,179,19,147,59,187,27,155,49,177,17,145,57,185,25,153},
            {243,115,211,83,251,123,219,91,241,113,209,81,249,121,217,89},
            {15,143,47,175,7,135,39,167,13,141,45,173,5,133,37,165},
            {207,79,239,111,199,71,231,103,205,77,237,109,197,69,229,101},
            {63,191,31,159,55,183,23,151,61,189,29,157,53,181,21,149},
            {255,127,223,95,247,119,215,87,253,125,221,93,245,117,213,85}
        };

        // 静态构造函数：初始化查找表
        static XTEinkFontRenderer()
        {
            // 初始化Gamma查找表（浮点版本）
            for (int i = 0; i < 256; i++)
            {
                GammaToLinearLUT[i] = (float)Math.Pow(i / 255.0, 2.2);
                // 整数版本（16位定点数）
                GammaToLinearLUTInt[i] = (int)(GammaToLinearLUT[i] * FIXED_POINT_SCALE);
                // 高精度浮点版本（32位浮点数）
                GammaToLinearLUTFloat[i] = (float)Math.Pow(i / 255.0, 2.2);
            }

            // 初始化反Gamma查找表（16位精度）
            for (int i = 0; i < 65536; i++)
            {
                double linear = i / 65535.0;
                LinearToGammaLUT[i] = (byte)Math.Round(Math.Pow(linear, 1.0 / 2.2) * 255);
            }

            // 初始化Bayer查找表
            for (int y = 0; y < BAYER_SIZE; y++)
            {
                for (int x = 0; x < BAYER_SIZE; x++)
                {
                    int idx = y * BAYER_SIZE + x;
                    // 浮点版本
                    BayerLUT[idx] = (BayerMatrix16x16[y, x] / 255.0f - 0.5f) * 0.1f;
                    // 整数版本（16位定点数）
                    BayerLUTInt[idx] = (int)(BayerLUT[idx] * FIXED_POINT_SCALE);
                    // 高精度浮点版本（32位浮点数）
                    BayerLUTFloat[idx] = (BayerMatrix16x16[y, x] / 255.0f - 0.5f) * 0.08f; // 稍微减小抖动强度
                }
            }
        }
        #endregion

        #region public API
        public Size GetFontRenderSize()
        {
            using Bitmap bmp = new(32, 32);
            bmp.SetResolution(96, 96);
            using Graphics g = Graphics.FromImage(bmp);
            StringFormat strfmt = new(StringFormat.GenericTypographic);
            SizeF sf = g.MeasureString("坐", Font, 999, strfmt);
            Size s = new((int)Math.Round(sf.Width) + CharSpacingPx, (int)Math.Round(sf.Height) + LineSpacingPx);
            if (s.Height < 5) s.Height = 5;
            if (s.Width < 5) s.Width = 5;
            return s;
        }

        public void RenderFont(int charCodePoint, XTEinkFontBinary renderer)
        {
            EnsureRenderSurfaceSize(renderer.Width, renderer.Height);
            SyncSettings();
            char chr = (char)charCodePoint;

            if (EnableUltimateSuperSampling)
            {
                using Bitmap ultimate = RenderUltimateSuperSampling(charCodePoint, renderer.Width, renderer.Height);
                renderer.LoadFromBitmap(charCodePoint, ultimate, 0, 0, LightThrehold);
            }
            else
            {
                RenderLegacy(chr, renderer);
            }
        }

        public void Dispose()
        {
            _tempGraphics?.Dispose();
            _tempRenderSurface?.Dispose();
            _format?.Dispose();
        }
        #endregion

        #region legacy path (no super-sampling)
        private void RenderLegacy(char chr, XTEinkFontBinary renderer)
        {
            _tempGraphics!.Clear(Color.Black);
            _tempGraphics.ResetTransform();

            if (RenderBorder)
                _tempGraphics.DrawRectangle(Pens.White, 0, 0, renderer.Width - 1, renderer.Height - 1);

            if (IsVerticalFont)
            {
                _tempGraphics.TranslateTransform(0, renderer.Height);
                _tempGraphics.RotateTransform(-90);
            }

            bool center = IsOldLineAlignment ? LineSpacingPx < 0 : true;
            if (center)
            {
                if (IsVerticalFont)
                    _tempGraphics.TranslateTransform(LineSpacingPx / 2f, 0);
                else
                    _tempGraphics.TranslateTransform(0, LineSpacingPx / 2f);
            }

            if (CharSpacingPx != 0 && chr > (char)255)
            {
                if (IsVerticalFont)
                    _tempGraphics.TranslateTransform(0, CharSpacingPx / 2f);
                else
                    _tempGraphics.TranslateTransform(CharSpacingPx / 2f, 0);
            }

            _tempGraphics.DrawString(chr.ToString(), Font, Brushes.White, 0, 0, _format);
            renderer.LoadFromBitmap(chr, _tempRenderSurface!, 0, 0, LightThrehold);
        }
        #endregion

        #region 32× ultimate super-sampling
        private Bitmap RenderUltimateSuperSampling(int charCodePoint, int targetWidth, int targetHeight)
        {
            char chr = (char)charCodePoint;
            int ultraW = targetWidth * ULTRA_SCALE;
            int ultraH = targetHeight * ULTRA_SCALE;

            Bitmap ultra = GetPooledBitmap(ultraW, ultraH);
            ultra.SetResolution(96, 96);

            using (Graphics g = Graphics.FromImage(ultra))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                // 在超采样中也使用用户的AAMode设置
                TextRenderingHint ultraHint = AAMode switch
                {
                    AntiAltasMode.System1BitGridFit => TextRenderingHint.SingleBitPerPixelGridFit,
                    AntiAltasMode.System1Bit => TextRenderingHint.SingleBitPerPixel,
                    AntiAltasMode.SystemAntiAltas => TextRenderingHint.AntiAlias,
                    _ => TextRenderingHint.AntiAliasGridFit,
                };
                g.TextRenderingHint = ultraHint;

                if (RenderBorder)
                    g.DrawRectangle(Pens.White, 0, 0, ultraW - 1, ultraH - 1);

                // 方案5：使用DrawString方式，完全模拟legacy模式
                // 创建放大的Font对象
                using Font scaledFont = new Font(Font.FontFamily, Font.Size * ULTRA_SCALE, Font.Style, Font.Unit);

                // 应用与legacy模式相同的变换
                g.ResetTransform();

                // 垂直字体变换
                if (IsVerticalFont)
                {
                    g.TranslateTransform(0, ultraH);
                    g.RotateTransform(-90);
                }

                // 行对齐逻辑（与legacy路径一致）
                bool center = IsOldLineAlignment ? LineSpacingPx < 0 : true;
                if (center)
                {
                    if (IsVerticalFont)
                        g.TranslateTransform(LineSpacingPx * ULTRA_SCALE / 2f, 0);
                    else
                        g.TranslateTransform(0, LineSpacingPx * ULTRA_SCALE / 2f);
                }

                // 字符间距（仅对非ASCII字符）
                if (CharSpacingPx != 0 && chr > (char)255)
                {
                    if (IsVerticalFont)
                        g.TranslateTransform(0, CharSpacingPx * ULTRA_SCALE / 2f);
                    else
                        g.TranslateTransform(CharSpacingPx * ULTRA_SCALE / 2f, 0);
                }

                // 使用预创建的StringFormat对象（性能优化）
                StringFormat ultraFormat = IsVerticalFont ? _verticalFormat : _horizontalFormat;

                // 直接使用DrawString，完全匹配legacy模式
                g.DrawString(chr.ToString(), scaledFont, Brushes.White, 0, 0, ultraFormat);
            }

            // 应用子像素Hinting（如果启用）
            if (EnableSubPixelHinting)
            {
                ultra = ApplySubPixelHinting(ultra, targetWidth, targetHeight, charCodePoint);
            }

            var result = ApplyFloatPrecisionBayerDithering(ultra, targetWidth, targetHeight, charCodePoint);
            ReturnPooledBitmap(ultra);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bitmap ApplyBayerDithering(Bitmap grayBmp, int targetW, int targetH, int charCodePoint)
        {
            Bitmap result = new(targetW, targetH);
            result.SetResolution(96, 96);

            // 使用查找表预计算阈值
            float thrLinear = GammaToLinearLUT[LightThrehold];

            var srcData = grayBmp.LockBits(new Rectangle(0, 0, grayBmp.Width, grayBmp.Height),
                                          System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                          System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var dstData = result.LockBits(new Rectangle(0, 0, targetW, targetH),
                                          System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                          System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* srcPtr = (uint*)srcData.Scan0;
                    uint* dstPtr = (uint*)dstData.Scan0;
                    int srcStride = srcData.Stride / 4;
                    int dstStride = dstData.Stride / 4;

                    // 优化的像素处理（使用查找表）
                    int ultraScale2 = ULTRA_SCALE * ULTRA_SCALE;

                    for (int y = 0; y < targetH; y++)
                    {
                        uint* dstRow = dstPtr + y * dstStride;
                        int bayerY = y & (BAYER_SIZE - 1);

                        for (int x = 0; x < targetW; x++)
                        {
                            // 整数Bayer抖动优化（完全避免浮点运算）
                            int gammaSum = 0;
                            int srcY = y * ULTRA_SCALE;
                            int srcX = x * ULTRA_SCALE;

                            // 展开内层循环以减少分支
                            for (int dy = 0; dy < ULTRA_SCALE; dy++)
                            {
                                uint* srcRow = srcPtr + (srcY + dy) * srcStride + srcX;

                                // 处理所有像素（整数运算）
                                for (int dx = 0; dx < ULTRA_SCALE; dx++)
                                {
                                    uint c = srcRow[dx];
                                    // 快速灰度转换（整数运算）
                                    int gray = (int)(((c >> 16) & 0xFF) * 299 +
                                                    ((c >> 8) & 0xFF) * 587 +
                                                    (c & 0xFF) * 114) / 1000;
                                    gammaSum += GammaToLinearLUTInt[gray];
                                }
                            }

                            int avgGamma = gammaSum / ultraScale2;

                            // 整数Bayer抖动（使用定点数运算）
                            int bayerX = x & (BAYER_SIZE - 1);
                            int bayerIdx = bayerY * BAYER_SIZE + bayerX;
                            int bayer = BayerLUTInt[bayerIdx];
                            // 超采样模式下让字体稍微浅一点（亮一点）
                            int compensatedThreshold = Math.Min(255, LightThrehold + 15);
                            int thrLinearInt = GammaToLinearLUTInt[compensatedThreshold];
                            int combined = thrLinearInt + bayer;

                            // 边界检查（定点数）
                            int minThr = (int)(0.02f * FIXED_POINT_SCALE);
                            int maxThr = (int)(0.98f * FIXED_POINT_SCALE);
                            combined = Math.Max(minThr, Math.Min(maxThr, combined));

                            dstRow[x] = avgGamma > combined ? 0xFFFFFFFF : 0xFF000000;
                        }
                    }
                }
            }
            finally
            {
                grayBmp.UnlockBits(srcData);
                result.UnlockBits(dstData);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bitmap ApplyFloatPrecisionBayerDithering(Bitmap grayBmp, int targetW, int targetH, int charCodePoint)
        {
            Bitmap result = new(targetW, targetH);
            result.SetResolution(96, 96);

            var srcData = grayBmp.LockBits(new Rectangle(0, 0, grayBmp.Width, grayBmp.Height),
                                          System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                          System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var dstData = result.LockBits(new Rectangle(0, 0, targetW, targetH),
                                          System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                          System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* srcPtr = (uint*)srcData.Scan0;
                    uint* dstPtr = (uint*)dstData.Scan0;
                    int srcStride = srcData.Stride / 4;
                    int dstStride = dstData.Stride / 4;

                    // 浮点精度超采样处理
                    float ultraScale2 = ULTRA_SCALE * ULTRA_SCALE;

                    for (int y = 0; y < targetH; y++)
                    {
                        uint* dstRow = dstPtr + y * dstStride;
                        int bayerY = y & (BAYER_SIZE - 1);

                        for (int x = 0; x < targetW; x++)
                        {
                            // 高精度浮点Gamma校正像素平均
                            float gammaSum = 0f;
                            int srcY = y * ULTRA_SCALE;
                            int srcX = x * ULTRA_SCALE;

                            // 展开内层循环以减少分支
                            for (int dy = 0; dy < ULTRA_SCALE; dy++)
                            {
                                uint* srcRow = srcPtr + (srcY + dy) * srcStride + srcX;

                                // 处理所有像素（高精度浮点运算）
                                for (int dx = 0; dx < ULTRA_SCALE; dx++)
                                {
                                    uint c = srcRow[dx];
                                    // 快速灰度转换（整数运算）
                                    int gray = (int)(((c >> 16) & 0xFF) * 299 +
                                                    ((c >> 8) & 0xFF) * 587 +
                                                    (c & 0xFF) * 114) / 1000;
                                    // 使用高精度浮点Gamma查找表
                                    gammaSum += GammaToLinearLUTFloat[gray];
                                }
                            }

                            float avgGamma = gammaSum / ultraScale2;

                            // 高精度浮点Bayer抖动
                            int bayerX = x & (BAYER_SIZE - 1);
                            int bayerIdx = bayerY * BAYER_SIZE + bayerX;
                            float bayer = BayerLUTFloat[bayerIdx];

                            // 超采样模式下让字体稍微浅一点（亮一点）
                            int compensatedThreshold = Math.Min(255, LightThrehold + 15);
                            float thrLinear = GammaToLinearLUTFloat[compensatedThreshold];
                            float combined = thrLinear + bayer;

                            // 边界检查（浮点数）
                            combined = Math.Max(0.02f, Math.Min(0.98f, combined));

                            dstRow[x] = avgGamma > combined ? 0xFFFFFFFF : 0xFF000000;
                        }
                    }
                }
            }
            finally
            {
                grayBmp.UnlockBits(srcData);
                result.UnlockBits(dstData);
            }
            return result;
        }

        private Bitmap ApplySubPixelHinting(Bitmap ultraBmp, int targetW, int targetH, int charCodePoint)
        {
            // 字符类型检测
            var charType = GetCharacterType(charCodePoint);
            if (charType == CharacterType.Detail) return ultraBmp; // 细节部分不处理

            // 并行处理32×32块
            int blockSize = ULTRA_SCALE;

            var data = ultraBmp.LockBits(new Rectangle(0, 0, ultraBmp.Width, ultraBmp.Height),
                                        System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* pixels = (uint*)data.Scan0;
                    int stride = data.Stride / 4;

                    // 并行处理每个目标像素对应的32×32块
                    Parallel.For(0, targetH, y =>
                    {
                        for (int x = 0; x < targetW; x++)
                        {
                            ProcessPixelBlock(pixels, stride, x, y, blockSize, charType);
                        }
                    });
                }
            }
            finally
            {
                ultraBmp.UnlockBits(data);
            }

            return ultraBmp;
        }

        private unsafe void ProcessPixelBlock(uint* pixels, int stride, int blockX, int blockY, int blockSize, CharacterType charType)
        {
            int startX = blockX * blockSize;
            int startY = blockY * blockSize;

            // 检测边缘方向和位置
            var edgeInfo = DetectEdges(pixels, stride, startX, startY, blockSize);

            if (edgeInfo.HasEdge)
            {
                // 根据字符类型应用不同强度的Hinting
                float hintingStrength = charType switch
                {
                    CharacterType.StraightStroke => SubPixelHintingStrength,      // 直线笔画：强对齐
                    CharacterType.CurvedStroke => SubPixelHintingStrength * 0.5f, // 曲线笔画：轻度对齐
                    _ => 0f
                };

                if (hintingStrength > 0)
                {
                    ApplyEdgeAlignment(pixels, stride, startX, startY, blockSize, edgeInfo, hintingStrength);
                }
            }
        }

        private unsafe EdgeInfo DetectEdges(uint* pixels, int stride, int startX, int startY, int blockSize)
        {
            var edgeInfo = new EdgeInfo();

            // 检测水平边缘
            for (int y = 1; y < blockSize - 1; y++)
            {
                int whiteCount = 0;
                float edgePosition = 0;
                bool foundEdge = false;

                for (int x = 0; x < blockSize; x++)
                {
                    uint pixel = pixels[(startY + y) * stride + (startX + x)];
                    bool isWhite = (pixel & 0xFF) > 128;

                    if (isWhite) whiteCount++;

                    // 检测边缘过渡
                    if (x > 0)
                    {
                        uint prevPixel = pixels[(startY + y) * stride + (startX + x - 1)];
                        bool prevWhite = (prevPixel & 0xFF) > 128;

                        if (isWhite != prevWhite)
                        {
                            edgePosition = x - 0.5f;
                            foundEdge = true;
                            break;
                        }
                    }
                }

                if (foundEdge && whiteCount > blockSize / 4 && whiteCount < blockSize * 3 / 4)
                {
                    edgeInfo.HasEdge = true;
                    edgeInfo.IsHorizontal = false;
                    edgeInfo.Position = edgePosition;
                    edgeInfo.Row = y;
                    break;
                }
            }

            // 检测垂直边缘（如果没有找到水平边缘）
            if (!edgeInfo.HasEdge)
            {
                for (int x = 1; x < blockSize - 1; x++)
                {
                    int whiteCount = 0;
                    float edgePosition = 0;
                    bool foundEdge = false;

                    for (int y = 0; y < blockSize; y++)
                    {
                        uint pixel = pixels[(startY + y) * stride + (startX + x)];
                        bool isWhite = (pixel & 0xFF) > 128;

                        if (isWhite) whiteCount++;

                        // 检测边缘过渡
                        if (y > 0)
                        {
                            uint prevPixel = pixels[(startY + y - 1) * stride + (startX + x)];
                            bool prevWhite = (prevPixel & 0xFF) > 128;

                            if (isWhite != prevWhite)
                            {
                                edgePosition = y - 0.5f;
                                foundEdge = true;
                                break;
                            }
                        }
                    }

                    if (foundEdge && whiteCount > blockSize / 4 && whiteCount < blockSize * 3 / 4)
                    {
                        edgeInfo.HasEdge = true;
                        edgeInfo.IsHorizontal = true;
                        edgeInfo.Position = edgePosition;
                        edgeInfo.Column = x;
                        break;
                    }
                }
            }

            return edgeInfo;
        }

        private unsafe void ApplyEdgeAlignment(uint* pixels, int stride, int startX, int startY, int blockSize, EdgeInfo edgeInfo, float strength)
        {
            // 对齐到1/4像素网格（blockSize/4的倍数）
            float gridSize = blockSize / 4.0f;
            float alignedPosition = (float)Math.Round(edgeInfo.Position / gridSize) * gridSize;
            float offset = alignedPosition - edgeInfo.Position;

            // 限制调整幅度
            offset = Math.Max(-2, Math.Min(2, offset)) * strength;

            if (Math.Abs(offset) < 0.1f) return; // 调整幅度太小，跳过

            // 应用边缘调整
            if (edgeInfo.IsHorizontal)
            {
                // 调整垂直边缘
                ApplyVerticalEdgeShift(pixels, stride, startX, startY, blockSize, edgeInfo.Column, offset);
            }
            else
            {
                // 调整水平边缘
                ApplyHorizontalEdgeShift(pixels, stride, startX, startY, blockSize, edgeInfo.Row, offset);
            }
        }

        private unsafe void ApplyVerticalEdgeShift(uint* pixels, int stride, int startX, int startY, int blockSize, int edgeCol, float offset)
        {
            for (int y = 0; y < blockSize; y++)
            {
                for (int x = Math.Max(0, edgeCol - 2); x < Math.Min(blockSize, edgeCol + 3); x++)
                {
                    int pixelIndex = (startY + y) * stride + (startX + x);
                    uint originalPixel = pixels[pixelIndex];

                    // 计算新的像素强度
                    float distance = Math.Abs(x - (edgeCol + offset));
                    float newIntensity = CalculateAdjustedIntensity(originalPixel, distance);

                    // 应用新强度
                    byte intensity = (byte)Math.Max(0, Math.Min(255, newIntensity));
                    pixels[pixelIndex] = (uint)(intensity | (intensity << 8) | (intensity << 16) | 0xFF000000);
                }
            }
        }

        private unsafe void ApplyHorizontalEdgeShift(uint* pixels, int stride, int startX, int startY, int blockSize, int edgeRow, float offset)
        {
            for (int x = 0; x < blockSize; x++)
            {
                for (int y = Math.Max(0, edgeRow - 2); y < Math.Min(blockSize, edgeRow + 3); y++)
                {
                    int pixelIndex = (startY + y) * stride + (startX + x);
                    uint originalPixel = pixels[pixelIndex];

                    // 计算新的像素强度
                    float distance = Math.Abs(y - (edgeRow + offset));
                    float newIntensity = CalculateAdjustedIntensity(originalPixel, distance);

                    // 应用新强度
                    byte intensity = (byte)Math.Max(0, Math.Min(255, newIntensity));
                    pixels[pixelIndex] = (uint)(intensity | (intensity << 8) | (intensity << 16) | 0xFF000000);
                }
            }
        }

        private float CalculateAdjustedIntensity(uint pixel, float distance)
        {
            byte originalIntensity = (byte)(pixel & 0xFF);

            // 使用平滑的过渡函数
            float factor = 1.0f - Math.Max(0, Math.Min(1, distance - 0.5f));
            return originalIntensity * factor;
        }

        private CharacterType GetCharacterType(int charCodePoint)
        {
            char ch = (char)charCodePoint;

            // ASCII字符分类
            if (charCodePoint <= 127)
            {
                // 直线笔画字符
                if ("EFHILT1|[]{}".Contains(ch)) return CharacterType.StraightStroke;

                // 曲线笔画字符
                if ("BCDGJOPQRS036689abcdegopqsuy()@&".Contains(ch)) return CharacterType.CurvedStroke;

                // 细节字符（标点符号等）
                if (".,;:!?'\"".Contains(ch)) return CharacterType.Detail;

                // 默认为曲线笔画
                return CharacterType.CurvedStroke;
            }

            // 中文字符分类（简化版本）
            if (charCodePoint >= 0x4E00 && charCodePoint <= 0x9FFF)
            {
                // 这里可以根据具体需求添加更复杂的中文字符分类逻辑
                // 暂时都归类为直线笔画，因为中文字符多包含横竖笔画
                return CharacterType.StraightStroke;
            }

            // 其他字符默认为曲线笔画
            return CharacterType.CurvedStroke;
        }

        private enum CharacterType
        {
            StraightStroke,  // 直线笔画
            CurvedStroke,    // 曲线笔画
            Detail           // 细节部分
        }

        private struct EdgeInfo
        {
            public bool HasEdge;
            public bool IsHorizontal;
            public float Position;
            public int Row;
            public int Column;
        }


        #region memory pool
        private static Bitmap GetPooledBitmap(int width, int height)
        {
            // 尝试从池中获取合适的Bitmap
            while (_bitmapPool.TryDequeue(out Bitmap pooled))
            {
                if (pooled.Width == width && pooled.Height == height)
                {
                    // 清空画布
                    using (Graphics g = Graphics.FromImage(pooled))
                    {
                        g.Clear(Color.Black);
                    }
                    return pooled;
                }
                else
                {
                    // 尺寸不匹配，释放资源
                    pooled.Dispose();
                }
            }

            // 池中没有合适的，创建新的
            return new Bitmap(width, height);
        }

        private static void ReturnPooledBitmap(Bitmap bitmap)
        {
            if (bitmap == null) return;

            // 限制池的大小，避免内存泄漏
            lock (_poolLock)
            {
                if (_bitmapPool.Count < 10) // 最多缓存10个Bitmap
                {
                    _bitmapPool.Enqueue(bitmap);
                }
                else
                {
                    bitmap.Dispose();
                }
            }
        }
        #endregion

        #region helpers
        private void EnsureRenderSurfaceSize(int width, int height)
        {
            if (_tempRenderSurface != null && _tempGraphics != null &&
                _tempRenderSurface.Width == width && _tempRenderSurface.Height == height) return;

            _tempGraphics?.Dispose();
            _tempRenderSurface?.Dispose();

            _tempRenderSurface = new Bitmap(width, height);
            _tempRenderSurface.SetResolution(96, 96);
            _tempGraphics = Graphics.FromImage(_tempRenderSurface);
            _tempGraphics.CompositingMode = CompositingMode.SourceOver;
            _tempGraphics.SmoothingMode = SmoothingMode.HighQuality;
            _tempGraphics.CompositingQuality = CompositingQuality.HighQuality;
            _tempGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _tempGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        private void SyncSettings()
        {
            if (_tempGraphics == null) return;
            TextRenderingHint hint = AAMode switch
            {
                AntiAltasMode.System1Bit => TextRenderingHint.SingleBitPerPixel,
                AntiAltasMode.System1BitGridFit => TextRenderingHint.SingleBitPerPixelGridFit,
                AntiAltasMode.SystemAntiAltas => TextRenderingHint.AntiAlias,
                _ => TextRenderingHint.AntiAliasGridFit,
            };
            if (_tempGraphics.TextRenderingHint != hint)
                _tempGraphics.TextRenderingHint = hint;

            if (IsVerticalFont) _format.FormatFlags |= StringFormatFlags.DirectionVertical;
            else _format.FormatFlags &= ~StringFormatFlags.DirectionVertical;
        }
        #endregion
    }
}
#endregion