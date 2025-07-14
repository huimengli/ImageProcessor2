using ImageProcessor2.AddFunc;
using ImageProcessor2.Enums;
using ImageProcessor2.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Encoder = System.Drawing.Imaging.Encoder;

namespace ImageProcessor2
{
    /// <summary>
    /// 应用程序入口点。
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 主窗体
        /// </summary>
        public static Form main;

        /// <summary>
        /// 屏幕截图
        /// </summary>
        public static List<Bitmap> bitmaps = new List<Bitmap>();

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 创建主窗体
            main = new Form
            {
                Text = "屏幕截图",
                ClientSize = new Size(800, 600)
            };

            // 获取屏幕截图
            int x = 0;
            Screen.AllScreens.ForEach(screen =>
            {
                var bitmap = Item.GetScreen(screen);
                PictureBox pictureBox = new PictureBox
                {
                    Dock = DockStyle.None,
                    BorderStyle = BorderStyle.FixedSingle,
                    Image = bitmap,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                pictureBox.Location = new Point(x, 0);
                x += pictureBox.Width + 10; // 每个图片之间留10像素的间隔

                // 点击显示图片
                pictureBox.Click += (sender, e) =>
                {
                    DrawImage(bitmap);
                    // 关闭当前窗体
                    main.Hide();
                };

                bitmaps.Add(bitmap);

                main.Controls.Add(pictureBox);
            });

            // 关闭主窗体
            main.FormClosed += (sender, e) =>
            {
                // 释放所有位图资源
                bitmaps.ForEach(b => b.Dispose());
                bitmaps.Clear();
            };

            // 保存图片
            bitmaps.ForEach((bitmap,index) => bitmap.Save($"screenshot_{index}.jpg", ImageFormat.Jpeg));

            // 显示主窗体
            Application.Run(main);
        }

        /// <summary>
        /// 绘制图像并处理分片。
        /// </summary>
        /// <param name="screen"></param>
        public static void DrawImage(Bitmap fullBitmap)
        {
            var jpegEncoder = ImageProcessor.GetEncoder(ImageFormat.Jpeg);
            var encoderParams = new EncoderParameters(1);
            var qualityParam = new EncoderParameter(Encoder.Quality, 75L);
            encoderParams.Param[0] = qualityParam;

            // 压缩为JPEG
            using (var memoryStream = new MemoryStream())
            {
                fullBitmap.Save(memoryStream, jpegEncoder, encoderParams);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (var compressedBitmap = new Bitmap(memoryStream))
                {
                    // 保存图片
                    compressedBitmap.Save("compressed_image.jpg", jpegEncoder, encoderParams);

                    // 分割图片
                    //var parts = ImageProcessor.SplitImage(compressedBitmap);
                    var parts = ImagePixelProcessor.SplitImageToFourBitmaps(compressedBitmap);

                    // 模拟接收部分数据
                    var receivedParts = new Bitmap[4];
                    receivedParts[(int)BlockType.TopLeft] = parts[(int)BlockType.TopLeft];
                    receivedParts[(int)BlockType.TopRight] = parts[(int)BlockType.TopRight];
                    receivedParts[(int)BlockType.BottomLeft] = parts[(int)BlockType.BottomLeft];
                    receivedParts[(int)BlockType.BottomRight] = parts[(int)BlockType.BottomRight];

                    // 保存分片图片
                    for (int i = 0; i < receivedParts.Length; i++)
                    {
                        if (receivedParts[i] != null)
                        {
                            receivedParts[i].Save($"part_{i}.jpg", jpegEncoder, encoderParams);
                        }
                    }

                    // 模拟图片丢失
                    receivedParts[(int)BlockType.BottomRight] = null; // 模拟丢失右下角部分
                    receivedParts[(int)BlockType.TopLeft] = null; // 模拟丢失左上角部分

                    // 尝试重组图片
                    var rebuiltImage = ImagePixelProcessor.RebuildImageFromFourBitmaps3(receivedParts);

                    // 保存重组后的图片
                    rebuiltImage.Save("rebuilt_image.jpg", jpegEncoder, encoderParams);

                    // 显示结果
                    using (var form = new Form())
                    {
                        form.Text = "重组后的图片";
                        form.ClientSize = new Size(rebuiltImage.Width, rebuiltImage.Height);
                        PictureBox pictureBox = new PictureBox();
                        pictureBox.Dock = DockStyle.Fill;
                        pictureBox.Image = rebuiltImage;
                        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        form.Controls.Add(pictureBox);
                        form.BackgroundImage = rebuiltImage;
                        form.ShowDialog();

                        form.FormClosed += (s, e) =>
                        {
                            // 打开主窗体
                            main.Show();
                        };
                    }
                }
            }
        }
    }
}
