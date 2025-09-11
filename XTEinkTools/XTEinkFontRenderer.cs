﻿﻿﻿﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;

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

        #region private fields
        private Bitmap _tempRenderSurface;
        private Graphics _tempGraphics;
        private Font _cachedSuperSamplingFont;
        private readonly StringFormat _format = new(StringFormat.GenericTypographic);
        private const int ULTRA_SCALE = 32;
        private const int BAYER_SIZE = 16;

        // 性能优化：预计算查找表
        private static readonly float[] GammaToLinearLUT = new float[256];
        private static readonly byte[] LinearToGammaLUT = new byte[65536]; // 16位精度
        private static readonly float[] BayerLUT = new float[BAYER_SIZE * BAYER_SIZE];
        private static readonly int[] GrayWeights = { 299, 587, 114 }; // RGB到灰度的权重

        // 内存池
        private static readonly ConcurrentQueue<Bitmap> _bitmapPool = new();
        private static readonly object _poolLock = new object();
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
            // 初始化Gamma查找表
            for (int i = 0; i < 256; i++)
            {
                GammaToLinearLUT[i] = (float)Math.Pow(i / 255.0, 2.2);
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
                    BayerLUT[idx] = (BayerMatrix16x16[y, x] / 255.0f - 0.5f) * 0.1f;
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
            _cachedSuperSamplingFont?.Dispose();
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
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                if (RenderBorder)
                    g.DrawRectangle(Pens.White, 0, 0, ultraW - 1, ultraH - 1);

                using GraphicsPath gp = new();
                // 使用像素大小而不是点大小，确保字体大小正确
                float emSizeInPixels = Font.Size;

                gp.AddString(chr.ToString(), Font.FontFamily, (int)Font.Style,
                             emSizeInPixels, PointF.Empty, StringFormat.GenericTypographic);

                using Matrix m = new();

                // 先应用字体变换（垂直、间距等），在缩放之前
                ApplyUltraVectorTransforms(m, targetWidth, targetHeight, ULTRA_SCALE, charCodePoint);

                // 最后应用32倍缩放，以匹配超采样画布
                m.Scale(ULTRA_SCALE, ULTRA_SCALE, MatrixOrder.Append);

                gp.Transform(m);

                g.FillPath(Brushes.White, gp);
            }

            var result = ApplyBayerDithering(ultra, targetWidth, targetHeight, charCodePoint);
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
                            // 快速Gamma校正像素平均（使用查找表）
                            float gammaSum = 0f;
                            int srcY = y * ULTRA_SCALE;
                            int srcX = x * ULTRA_SCALE;

                            // 展开内层循环以减少分支
                            for (int dy = 0; dy < ULTRA_SCALE; dy++)
                            {
                                uint* srcRow = srcPtr + (srcY + dy) * srcStride + srcX;

                                // 处理所有像素
                                for (int dx = 0; dx < ULTRA_SCALE; dx++)
                                {
                                    uint c = srcRow[dx];
                                    // 快速灰度转换（整数运算）
                                    int gray = (int)(((c >> 16) & 0xFF) * 299 +
                                                    ((c >> 8) & 0xFF) * 587 +
                                                    (c & 0xFF) * 114) / 1000;
                                    gammaSum += GammaToLinearLUT[gray];
                                }
                            }

                            float avgGamma = gammaSum / ultraScale2;

                            // 优化的Bayer抖动（使用预计算查找表）
                            int bayerX = x & (BAYER_SIZE - 1);
                            int bayerIdx = bayerY * BAYER_SIZE + bayerX;
                            float bayer = BayerLUT[bayerIdx];
                            float combined = Math.Max(0.02f, Math.Min(0.98f, thrLinear + bayer));

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

        private void ApplyDirectionalSmoothing(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                    System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* p = (uint*)data.Scan0;
                    int stride = data.Stride / 4;
                    byte* g = stackalloc byte[9];
                    for (int y = 1; y < bmp.Height - 1; y++)
                    {
                        for (int x = 1; x < bmp.Width - 1; x++)
                        {
                            // 快速灰度+梯度
                            for (int dy = -1; dy <= 1; dy++)
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    uint c = p[(y + dy) * stride + (x + dx)];
                                    int b = (int)(c & 0xFF), g_ = (int)((c >> 8) & 0xFF), r = (int)((c >> 16) & 0xFF);
                                    g[(dy + 1) * 3 + (dx + 1)] = (byte)((r * 299 + g_ * 587 + b * 114 + 500) / 1000);
                                }
                            int gh = Math.Abs(g[3 + 0] - g[3 + 2]), gv = Math.Abs(g[0 + 1] - g[2 + 1]);
                            if (gv > gh * 2 || gh > gv * 2) continue; // 横/竖线跳过
                            // 3×3 高斯
                            float sum = 0, k = 0.0625f;
                            for (int dy = -1; dy <= 1; dy++)
                                for (int dx = -1; dx <= 1; dx++)
                                    sum += g[(dy + 1) * 3 + (dx + 1)] * (dy == 0 && dx == 0 ? 0.25f : k);
                            byte v = (byte)Math.Min(255, Math.Max(0, sum));
                            uint rgb = (uint)(v | (v << 8) | (v << 16) | 0xFF000000);
                            p[y * stride + x] = rgb;
                        }
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
        }

        private static void ApplyInkExpansionCompensation(GraphicsPath path, float shrinkPx)
        {
            if (shrinkPx * 2 < 0.7f) return; // 太细不补偿
            try
            {
                using Pen pen = new(Color.Black, -shrinkPx * 2f) { LineJoin = LineJoin.Round };
                path.Widen(pen);
            }
            catch { /* 太细失败就原样 */ }
        }

        private void ApplyUltraVectorTransforms(Matrix m, int targetW, int targetH, int scale, int charCodePoint)
        {
            char chr = (char)charCodePoint;
            if (chr > 255 && char.IsControl(chr)) return;

            // 垂直字体变换（需要按scale缩放）
            if (IsVerticalFont)
            {
                m.Translate(0, targetH * scale);
                m.Rotate(-90);
            }

            // 行对齐逻辑必须与legacy路径一致！
            // 注意：间距不需要按scale缩放，因为已经在矩阵缩放后应用
            bool center = IsOldLineAlignment ? LineSpacingPx < 0 : true;
            if (center)
            {
                if (IsVerticalFont)
                    m.Translate(LineSpacingPx / 2f, 0);
                else
                    m.Translate(0, LineSpacingPx / 2f);
            }

            // 字符间距（仅对非ASCII字符，不需要按scale缩放）
            if (CharSpacingPx != 0 && chr > (char)255)
            {
                if (IsVerticalFont)
                    m.Translate(0, CharSpacingPx / 2f);
                else
                    m.Translate(CharSpacingPx / 2f, 0);
            }
        }

        private bool ShouldApplyInkCompensation(int charCodePoint)
        {
            if (charCodePoint <= 127)
            {
                char ch = (char)charCodePoint;
                if ("1iIl|!.,:;".Contains(ch)) return false;
            }
            return !IsPunctuationCharacter(charCodePoint);
        }

        private bool IsPunctuationCharacter(int charCodePoint)
        {
            if (charCodePoint is >= 0x21 and <= 0x2F or >= 0x3A and <= 0x40 or >= 0x5B and <= 0x60 or >= 0x7B and <= 0x7E) return true;
            if (charCodePoint is >= 0x2000 and <= 0x206F or >= 0x3000 and <= 0x303F or >= 0xFF00 and <= 0xFFEF) return true;
            if (charCodePoint <= 0xFFFF) return char.IsPunctuation((char)charCodePoint) || char.IsSymbol((char)charCodePoint);
            return false;
        }

        private static bool NeedsSmoothCurveProcessing(int charCodePoint)
        {
            if (charCodePoint <= 127)
            {
                char ch = (char)charCodePoint;
                if ("036689BCDGJOPQRSabcdegopqsuy()[]{}@&".Contains(ch)) return true;
                return false;
            }
            // 已知弯钩/撇捺/圆弧字符表略，同旧逻辑
            return false; // 保守返回
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