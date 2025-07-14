using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor2.Helpers
{
    /// <summary>
    /// 颜色与位图转换器
    /// </summary>
    public static class ColorBitmapConverter
    {
        /// <summary>
        /// 将Color[,]二维数组转换为Bitmap
        /// </summary>
        public static Bitmap ColorArrayToBitmap(Color[,] colorArray)
        {
            int width = colorArray.GetLength(1);
            int height = colorArray.GetLength(0);
            Bitmap bitmap = new Bitmap(width, height);

            // 使用LockBits提升性能
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color c = colorArray[y, x];
                        ptr[0] = c.B;  // 蓝色分量
                        ptr[1] = c.G;  // 绿色分量
                        ptr[2] = c.R;  // 红色分量
                        ptr[3] = c.A;  // Alpha通道
                        ptr += 4;
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            return bitmap;
        }

        /// <summary>
        /// 将Bitmap转换为Color[,]二维数组
        /// </summary>
        public static Color[,] BitmapToColorArray(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            Color[,] colorArray = new Color[height, width];

            // 使用LockBits提升性能
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        colorArray[y, x] = Color.FromArgb(
                            ptr[3],  // Alpha通道
                            ptr[2],  // 红色分量
                            ptr[1],  // 绿色分量
                            ptr[0]); // 蓝色分量
                        ptr += 4;
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            return colorArray;
        }
    }
}
