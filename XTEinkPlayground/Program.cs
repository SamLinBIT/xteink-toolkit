using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTEinkTools;

namespace XTEinkPlayground
{
    internal class Program
    {
        static void Main(string[] args)
        {
            YokokuJouMaker();
        }


        private static void YokokuJouMaker()
        {
            string[] families = { "华文仿宋","华文宋体","华文新魏","华文楷体","华文隶书","方正姚体","方正舒体","楷体","隶书"};
            List<Font> allFonts = new List<Font>();
            foreach (var item in families)
            {
                for (int i = 20; i <= 24; i+=2)
                {
                    allFonts.Add(new Font(new FontFamily(item), i));
                }
            }
            Random rnd = new Random();

            Bitmap bmp = new Bitmap(42, 42);
            XTEinkFontBinary fontOut = new XTEinkFontBinary(42, 42);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            Brush white = new SolidBrush(Color.White);
            Pen whiteLine = new Pen(white);
            Rectangle rcOutline = new Rectangle(-16, -16, 32, 32);
            StringFormat centerStr = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.FitBlackBox
            };
            for (int chr = 33; chr < 65536; chr++)
            {
                graphics.Clear(Color.Black);
                graphics.ResetTransform();
                string charStr = ((char)chr).ToString();
                Font usesFont = allFonts[rnd.Next(allFonts.Count)];
                GraphicsPath gp = new GraphicsPath();
                gp.AddString(charStr, usesFont.FontFamily, 1, usesFont.Size, Point.Empty,centerStr);
                var bound = gp.GetBounds();
                graphics.TranslateTransform(21, 21);
                var drawX = - bound.Width / 2 - bound.X;
                var drawY = - bound.Height / 2 - bound.Y;
                graphics.RotateTransform((float)((rnd.NextDouble() - 0.5) * 30f));
                graphics.TranslateTransform(drawX, drawY);
                graphics.FillPath( white, gp);
                graphics.TranslateTransform((float)(-drawX + (rnd.NextDouble() - 0.5) * 2.2f), (float)(-drawY + (rnd.NextDouble() - 0.5f) * 2.2));
                graphics.DrawRectangle(whiteLine, rcOutline);
                graphics.DrawLine(whiteLine, 17, -14, 17, 18);
                graphics.DrawLine(whiteLine, -16, 17, 18, 17);
                GC.KeepAlive(gp);
                gp.Dispose();
                fontOut.LoadFromBitmap(chr, bmp,0,0,32);
                if (chr % 1024 == 56)
                {
                    Console.WriteLine(chr);
                    fontOut.PrintCharInConsole(chr);
                }
            }



            Console.ReadLine();


            using (var f = File.Create(fontOut.GetSuggestedFileName("YokokuJou")))
            {
                fontOut.saveToFile(f);
            }
            Console.WriteLine("Done");

        }

        private static void Test0()
        {
            XTEinkFontBinary sourceFont = new XTEinkFontBinary(24, 27);
            using (var s = File.OpenRead("test24×27.bin"))
            {
                sourceFont.loadFromFile(s);
            }
            sourceFont.PrintCharInConsole('是');
            Console.ReadLine();
        }

        private static void TestAddLineSpace()
        {
            XTEinkFontBinary sourceFont = new XTEinkFontBinary(24, 27);
            using (var s = File.OpenRead("test24×27.bin"))
            {
                sourceFont.loadFromFile(s);
            }
            XTEinkFontBinary targetFont = new XTEinkFontBinary(26, 34);
            for (int i = 0; i < 65536; i++)
            {
                if (i % 1024 == 0) { Console.WriteLine(i); }
                for (int y = 0; y < sourceFont.Height; y++)
                {
                    for (int x = 0; x < sourceFont.Width; x++)
                    {
                        targetFont.SetPixel(i, x + 1, y + 3, sourceFont.GetPixel(i, x, y));
                    }
                }
            }
            using (var f = File.Create("testoutframe 26×34.bin"))
            {
                targetFont.saveToFile(f);
            }
            Console.WriteLine("Done");
        }

    }




}
