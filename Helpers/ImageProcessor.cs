using ImageProcessor2.Enums;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor2.Helpers
{
    /// <summary>
    /// 图片处理器
    /// </summary>
    public class ImageProcessor
    {
        public static Bitmap[] SplitImage(Bitmap original)
        {
            if (original.Width % 2 != 0 || original.Height % 2 != 0)
            {
                throw new ArgumentException("图片尺寸必须是偶数!");
            }

            Bitmap[] parts = new Bitmap[4];
            int width = original.Width / 2;
            int height = original.Height / 2;

            // 左上
            parts[(int)BlockType.TopLeft] = original.Clone(new Rectangle(0, 0, width, height), original.PixelFormat);
            // 右上
            parts[(int)BlockType.TopRight] = original.Clone(new Rectangle(width, 0, width, height), original.PixelFormat);
            // 左下
            parts[(int)BlockType.BottomLeft] = original.Clone(new Rectangle(0, height, width, height), original.PixelFormat);
            // 右下
            parts[(int)BlockType.BottomRight] = original.Clone(new Rectangle(width, height, width, height), original.PixelFormat);

            return parts;
        }

        // 重组图片（模拟数据丢失）
        public static Bitmap RebuildImage(Bitmap[] receivedParts)
        {
            if (receivedParts == null || receivedParts.Length == 0)
                throw new ArgumentException("没有接收到任何图片部分");

            // 确定最终图片尺寸
            int partWidth = receivedParts[0].Width;
            int partHeight = receivedParts[0].Height;
            Bitmap result = new Bitmap(partWidth * 2, partHeight * 2);

            using (Graphics g = Graphics.FromImage(result))
            {
                // 处理不同接收情况
                bool has0 = receivedParts.Length > 0 && receivedParts[0] != null;
                bool has1 = receivedParts.Length > 1 && receivedParts[1] != null;
                bool has2 = receivedParts.Length > 2 && receivedParts[2] != null;
                bool has3 = receivedParts.Length > 3 && receivedParts[3] != null;

                if (has0 && has1 && has2 && has3)
                {
                    // 完整接收
                    g.DrawImage(receivedParts[0], 0, 0);
                    g.DrawImage(receivedParts[1], partWidth, 0);
                    g.DrawImage(receivedParts[2], 0, partHeight);
                    g.DrawImage(receivedParts[3], partWidth, partHeight);
                }
                else if (has0 && has1 && !has2 && !has3)
                {
                    // 只有0和1
                    g.DrawImage(receivedParts[0], 0, 0);
                    g.DrawImage(receivedParts[0], 0, partHeight);
                    g.DrawImage(receivedParts[1], partWidth, 0);
                    g.DrawImage(receivedParts[1], partWidth, partHeight);
                }
                else if (!has0 && !has1 && has2 && has3)
                {
                    // 只有2和3
                    g.DrawImage(receivedParts[2], 0, 0);
                    g.DrawImage(receivedParts[2], 0, partHeight);
                    g.DrawImage(receivedParts[3], partWidth, 0);
                    g.DrawImage(receivedParts[3], partWidth, partHeight);
                }
                else if (has0 && !has1 && !has2 && has3)
                {
                    // 对角线0和3
                    Bitmap mixed = MixImages(receivedParts[0], receivedParts[3]);
                    g.DrawImage(receivedParts[0], 0, 0);
                    g.DrawImage(mixed, partWidth, 0);
                    g.DrawImage(mixed, 0, partHeight);
                    g.DrawImage(receivedParts[3], partWidth, partHeight);
                }
                else if (!has0 && has1 && has2 && !has3)
                {
                    // 对角线1和2
                    Bitmap mixed = MixImages(receivedParts[1], receivedParts[2]);
                    g.DrawImage(mixed, 0, 0);
                    g.DrawImage(receivedParts[1], partWidth, 0);
                    g.DrawImage(receivedParts[2], 0, partHeight);
                    g.DrawImage(mixed, partWidth, partHeight);
                }
                else if (receivedParts.Length == 1 || (!has1 && !has2 && !has3))
                {
                    // 只有0
                    g.DrawImage(receivedParts[0], 0, 0, result.Width, result.Height);
                }
                else if (receivedParts.Length == 2 && has1 && !has2 && !has3)
                {
                    // 只有1
                    g.DrawImage(receivedParts[1], 0, 0, result.Width, result.Height);
                }
                else if (receivedParts.Length == 2 && !has1 && has2 && !has3)
                {
                    // 只有2
                    g.DrawImage(receivedParts[2], 0, 0, result.Width, result.Height);
                }
                else if (receivedParts.Length >= 3 && !has1 && !has2 && has3)
                {
                    // 只有3
                    g.DrawImage(receivedParts[3], 0, 0, result.Width, result.Height);
                }
                else if ((has0 && has1 && has2) || (has0 && has1 && has3) ||
                         (has0 && has2 && has3) || (has1 && has2 && has3))
                {
                    // 三个部分的情况
                    Bitmap[] available = new Bitmap[3];
                    int index = 0;
                    if (has0) available[index++] = receivedParts[0];
                    if (has1) available[index++] = receivedParts[1];
                    if (has2) available[index++] = receivedParts[2];
                    if (has3 && index < 3) available[index] = receivedParts[3];

                    Bitmap mixed = MixImages(available);
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < receivedParts.Length && receivedParts[i] != null)
                        {
                            int x = (i == 1 || i == 3) ? partWidth : 0;
                            int y = (i == 2 || i == 3) ? partHeight : 0;
                            g.DrawImage(receivedParts[i], x, y);
                        }
                        else
                        {
                            int x = (i == 1 || i == 3) ? partWidth : 0;
                            int y = (i == 2 || i == 3) ? partHeight : 0;
                            g.DrawImage(mixed, x, y, partWidth, partHeight);
                        }
                    }
                }
            }

            return result;
        }

        // 混合两张图片
        private static Bitmap MixImages(Bitmap img1, Bitmap img2)
        {
            Bitmap result = new Bitmap(img1.Width, img1.Height);
            for (int x = 0; x < img1.Width; x++)
            {
                for (int y = 0; y < img1.Height; y++)
                {
                    Color c1 = img1.GetPixel(x, y);
                    Color c2 = img2.GetPixel(x, y);
                    Color mixed = Color.FromArgb(
                        (c1.A + c2.A) / 2,
                        (c1.R + c2.R) / 2,
                        (c1.G + c2.G) / 2,
                        (c1.B + c2.B) / 2);
                    result.SetPixel(x, y, mixed);
                }
            }
            return result;
        }

        // 混合三张图片
        private static Bitmap MixImages(Bitmap[] images)
        {
            if (images == null || images.Length < 2)
                throw new ArgumentException("需要至少两张图片");

            Bitmap result = new Bitmap(images[0].Width, images[0].Height);
            for (int x = 0; x < images[0].Width; x++)
            {
                for (int y = 0; y < images[0].Height; y++)
                {
                    int a = 0, r = 0, g = 0, b = 0;
                    foreach (Bitmap img in images)
                    {
                        if (img != null)
                        {
                            Color c = img.GetPixel(x, y);
                            a += c.A;
                            r += c.R;
                            g += c.G;
                            b += c.B;
                        }
                    }
                    Color mixed = Color.FromArgb(
                        a / images.Length,
                        r / images.Length,
                        g / images.Length,
                        b / images.Length);
                    result.SetPixel(x, y, mixed);
                }
            }
            return result;
        }

        // 辅助方法：获取图像编码器
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}
