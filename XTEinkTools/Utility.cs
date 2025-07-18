using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTEinkTools
{
    public static class Utility
    {
        /// <summary>
        /// 在控制台打印指定字形的像素呈现
        /// </summary>
        /// <param name="font">字体</param>
        /// <param name="charCode">码点</param>
        public static void PrintCharInConsole(this XTEinkFontBinary font, int charCode)
        {
            for (int y = 0; y < font.Height; y++)
            {
                Console.Write("[");
                for (int x = 0; x < font.Width; x++)
                {
                    if (font.GetPixel(charCode, x, y))
                    {
                        Console.Write("██");
                    }
                    else
                    {
                        Console.Write("  ");
                    }
                }
                Console.Write("]");
                Console.WriteLine();
            }
            
        }

        public static void LoadFromBitmap(this XTEinkFontBinary font, int charCode, Bitmap bmp, int sx = 0, int sy = 0, int threhold = 128) {
            for (int y = 0; y < font.Height; y++)
            {
                for (int x = 0; x < font.Width; x++)
                {
                    bool isOn = bmp.GetPixel(x + sx, y + sy).G > 128;
                    font.SetPixel(charCode, x, y,isOn);
                }
            }
        }


    }
}
