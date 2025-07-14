using ImageProcessor2.AddFunc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageProcessor2.Helpers
{
    /// <summary>
    /// 图片像素处理器
    /// </summary>
    public class ImagePixelProcessor
    {
        // 高效的像素复制方法
        private static unsafe void CopyPixel(byte[] srcBuffer, int srcIndex,
                                            BitmapData destData, int destIndex)
        {
            byte* destPtr = (byte*)destData.Scan0;
            fixed (byte* srcPtr = &srcBuffer[srcIndex])
            {
                for (int i = 0; i < 4; i++) // 复制4个字节（ARGB）
                {
                    destPtr[destIndex + i] = srcPtr[i];
                }
            }
        }

        private static unsafe void CopyPixel(BitmapData srcData, int srcIndex,
                                            byte[] destBuffer, int destIndex)
        {
            byte* srcPtr = (byte*)srcData.Scan0;
            fixed (byte* destPtr = &destBuffer[destIndex])
            {
                for (int i = 0; i < 4; i++) // 复制4个字节（ARGB）
                {
                    destPtr[i] = srcPtr[srcIndex + i];
                }
            }
        }

        // 将图像分割成四张独立的Bitmap
        public static Bitmap[] SplitImageToFourBitmaps(Bitmap original)
        {
            if (original.Width % 2 != 0 || original.Height % 2 != 0)
                throw new ArgumentException("图片尺寸必须是偶数");

            int halfWidth = original.Width / 2;
            int halfHeight = original.Height / 2;

            // 创建四张新位图
            Bitmap[] resultBitmaps = new Bitmap[4];
            for (int i = 0; i < 4; i++)
            {
                resultBitmaps[i] = new Bitmap(halfWidth, halfHeight, PixelFormat.Format32bppArgb);
            }

            // 使用LockBits高效处理像素
            BitmapData originalData = null;
            BitmapData[] newData = new BitmapData[4];

            try
            {
                originalData = original.LockBits(
                    new Rectangle(0, 0, original.Width, original.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int pixelSize = 4; // 32bpp = 4字节/像素
                int origStride = originalData.Stride;
                byte[] origBuffer = new byte[origStride * original.Height];
                System.Runtime.InteropServices.Marshal.Copy(originalData.Scan0, origBuffer, 0, origBuffer.Length);

                // 为每个新位图准备数据
                for (int i = 0; i < 4; i++)
                {
                    newData[i] = resultBitmaps[i].LockBits(
                        new Rectangle(0, 0, halfWidth, halfHeight),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                }

                // 处理每个2x2块
                for (int y = 0; y < halfHeight; y++)
                {
                    for (int x = 0; x < halfWidth; x++)
                    {
                        // 原始坐标
                        int origX1 = x * 2;
                        int origX2 = x * 2 + 1;
                        int origY1 = y * 2;
                        int origY2 = y * 2 + 1;

                        // 计算原始缓冲区的索引
                        int origIndex1 = origY1 * origStride + origX1 * pixelSize;
                        int origIndex2 = origY1 * origStride + origX2 * pixelSize;
                        int origIndex3 = origY2 * origStride + origX1 * pixelSize;
                        int origIndex4 = origY2 * origStride + origX2 * pixelSize;

                        // 计算新位图的索引
                        int newIndex = y * newData[0].Stride + x * pixelSize;

                        // 左上角像素 -> 第一张位图
                        CopyPixel(origBuffer, origIndex1, newData[0], newIndex);

                        // 右上角像素 -> 第二张位图
                        CopyPixel(origBuffer, origIndex2, newData[1], newIndex);

                        // 左下角像素 -> 第三张位图
                        CopyPixel(origBuffer, origIndex3, newData[2], newIndex);

                        // 右下角像素 -> 第四张位图
                        CopyPixel(origBuffer, origIndex4, newData[3], newIndex);
                    }
                }
            }
            finally
            {
                // 解锁所有位图
                if (originalData != null) original.UnlockBits(originalData);
                for (int i = 0; i < 4; i++)
                {
                    if (resultBitmaps[i] != null && newData[i] != null)
                        resultBitmaps[i].UnlockBits(newData[i]);
                }
            }

            return resultBitmaps;
        }

        // 从四张分割图中重建原始图像
        public static Bitmap RebuildImageFromFourBitmaps(Bitmap[] splitBitmaps)
        {
            if (splitBitmaps.Length != 4)
                throw new ArgumentException("需要四张分割图像");

            if (splitBitmaps.Any(b => b == null))
                throw new ArgumentException("分割图像不能为空");

            int halfWidth = splitBitmaps[0].Width;
            int halfHeight = splitBitmaps[0].Height;

            if (splitBitmaps.Any(b => b.Width != halfWidth || b.Height != halfHeight))
                throw new ArgumentException("所有分割图像尺寸必须相同");

            // 创建原始尺寸的图像
            Bitmap original = new Bitmap(halfWidth * 2, halfHeight * 2, PixelFormat.Format32bppArgb);

            BitmapData origData = null;
            BitmapData[] splitData = new BitmapData[4];

            try
            {
                origData = original.LockBits(
                    new Rectangle(0, 0, original.Width, original.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                int pixelSize = 4; // 32bpp = 4字节/像素
                int origStride = origData.Stride;
                byte[] origBuffer = new byte[origStride * original.Height];

                // 锁定所有分割图像
                for (int i = 0; i < 4; i++)
                {
                    splitData[i] = splitBitmaps[i].LockBits(
                        new Rectangle(0, 0, halfWidth, halfHeight),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                }

                // 重建图像
                for (int y = 0; y < halfHeight; y++)
                {
                    for (int x = 0; x < halfWidth; x++)
                    {
                        int newIndex = y * splitData[0].Stride + x * pixelSize;
                        int origX1 = x * 2;
                        int origX2 = x * 2 + 1;
                        int origY1 = y * 2;
                        int origY2 = y * 2 + 1;

                        // 左上角
                        int origIndex1 = origY1 * origStride + origX1 * pixelSize;
                        CopyPixel(splitData[0], newIndex, origBuffer, origIndex1);

                        // 右上角
                        int origIndex2 = origY1 * origStride + origX2 * pixelSize;
                        CopyPixel(splitData[1], newIndex, origBuffer, origIndex2);

                        // 左下角
                        int origIndex3 = origY2 * origStride + origX1 * pixelSize;
                        CopyPixel(splitData[2], newIndex, origBuffer, origIndex3);

                        // 右下角
                        int origIndex4 = origY2 * origStride + origX2 * pixelSize;
                        CopyPixel(splitData[3], newIndex, origBuffer, origIndex4);
                    }
                }

                // 将数据复制回原始位图
                System.Runtime.InteropServices.Marshal.Copy(
                    origBuffer, 0, origData.Scan0, origBuffer.Length);
            }
            finally
            {
                // 解锁所有位图
                if (origData != null) original.UnlockBits(origData);
                for (int i = 0; i < 4; i++)
                {
                    if (splitBitmaps[i] != null && splitData[i] != null)
                        splitBitmaps[i].UnlockBits(splitData[i]);
                }
            }

            return original;
        }

        /// <summary>
        /// 从四张分割图中重建原始图像（按新逻辑实现）
        /// 允许图片丢失部分数据时，仍能尽可能重建原图。
        /// </summary>
        /// <param name="splitBitmaps"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Bitmap RebuildImageFromFourBitmaps2(Bitmap[] splitBitmaps)
        {
            if (splitBitmaps.Length != 4)
                throw new ArgumentException("需要四张分割图像");

            if (splitBitmaps.All(b => b == null))
                throw new ArgumentException("分割图像不能全部为空");

            var haveImage = splitBitmaps.Map(bitmap => bitmap != null);

            int halfWidth = splitBitmaps.FirstNotNull().Width;
            int halfHeight = splitBitmaps.FirstNotNull().Height;

            if (splitBitmaps.Where(b => b != null).Any(b => b.Width != halfWidth || b.Height != halfHeight))
                throw new ArgumentException("所有分割图像尺寸必须相同");

            // 创建原始尺寸的图像
            Bitmap original = new Bitmap(halfWidth * 2, halfHeight * 2, PixelFormat.Format32bppArgb);

            // 使用LockBits高效处理像素
            BitmapData origData = original.LockBits(
                new Rectangle(0, 0, original.Width, original.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            BitmapData[] splitData = new BitmapData[4];
            try
            {
                int pixelSize = 4; // 32bpp = 4字节/像素
                int origStride = origData.Stride;
                byte[] origBuffer = new byte[origStride * original.Height];

                // 获取所有分割图像的数据
                for (int i = 0; i < 4; i++)
                {
                    splitData[i] = splitBitmaps[i] != null ? splitBitmaps[i].LockBits(
                        new Rectangle(0, 0, halfWidth, halfHeight),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb) : 
                        new Bitmap(halfWidth,halfHeight).LockBits(new Rectangle(
                            0,0, halfWidth, halfHeight
                        ),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                }

                // 为每个分割图创建缓冲区
                byte[][] splitBuffers = new byte[4][];
                var length = halfWidth * halfHeight * pixelSize;
                for (int i = 0; i < 4; i++)
                {
                    splitBuffers[i] = new byte[length];
                    System.Runtime.InteropServices.Marshal.Copy(
                        splitData[i].Scan0, splitBuffers[i], 0, splitBuffers[i].Length);
                }

                // 重建图像 - 按新逻辑处理每个2x2块
                for (int y = 0; y < halfHeight; y++)
                {
                    for (int x = 0; x < halfWidth; x++)
                    {
                        // 计算新位图的索引
                        int splitIndex = y * splitData[0].Stride + x * pixelSize;

                        // 原始像素位置
                        int origX1 = x * 2;
                        int origX2 = x * 2 + 1;
                        int origY1 = y * 2;
                        int origY2 = y * 2 + 1;

                        // 原始缓冲区索引
                        int origIndexTL = origY1 * origStride + origX1 * pixelSize; // 左上
                        int origIndexTR = origY1 * origStride + origX2 * pixelSize; // 右上
                        int origIndexBL = origY2 * origStride + origX1 * pixelSize; // 左下
                        int origIndexBR = origY2 * origStride + origX2 * pixelSize; // 右下

                        // 获取当前块的四个像素（可能为空）
                        Color?[] pixels = new Color?[4];
                        for (int i = 0; i < 4; i++)
                        {
                            if (haveImage[i])
                            {
                                pixels[i] = Color.FromArgb(
                                    splitBuffers[i][splitIndex + 3], // A
                                    splitBuffers[i][splitIndex + 2], // R
                                    splitBuffers[i][splitIndex + 1], // G
                                    splitBuffers[i][splitIndex + 0]  // B
                                );
                            }
                        }

                        // 根据接收情况重建2x2块
                        RebuildBlock(
                            pixels,
                            origBuffer,
                            origIndexTL, origIndexTR, origIndexBL, origIndexBR);
                    }
                }

                // 将数据复制回原始位图
                System.Runtime.InteropServices.Marshal.Copy(
                    origBuffer, 0, origData.Scan0, origBuffer.Length);
            }
            finally
            {
                original.UnlockBits(origData);

                // 解锁所有分割图像
                for (int i = 0; i < 4; i++)
                {
                    if (splitBitmaps[i] != null)
                        splitBitmaps[i].UnlockBits(splitData[i]);
                }
            }

            return original;
        }

        // 从四张分割图中重建原始图像（使用卷积计算）
        public static Bitmap RebuildImageFromFourBitmaps3(Bitmap[] splitBitmaps)
        {
            if (splitBitmaps.Length != 4)
                throw new ArgumentException("需要四张分割图像");

            if (splitBitmaps.All(b => b == null))
                throw new ArgumentException("分割图像不能全部为空");

            // 获取有效图像和存在标记
            bool[] haveImage = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                haveImage[i] = splitBitmaps[i] != null;
            }

            // 获取参考尺寸
            Bitmap reference = splitBitmaps.FirstOrDefault(b => b != null);
            if (reference == null) throw new InvalidOperationException("找不到有效参考图像");

            int halfWidth = reference.Width;
            int halfHeight = reference.Height;

            // 验证所有有效图像的尺寸
            if (splitBitmaps.Where(b => b != null).Any(b => b.Width != halfWidth || b.Height != halfHeight))
                throw new ArgumentException("所有分割图像尺寸必须相同");

            // 创建原始尺寸的图像
            Bitmap original = new Bitmap(halfWidth * 2, halfHeight * 2, PixelFormat.Format32bppArgb);

            // 使用LockBits高效处理像素
            BitmapData origData = original.LockBits(
                new Rectangle(0, 0, original.Width, original.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            BitmapData[] splitData = new BitmapData[4];
            try
            {
                int pixelSize = 4; // 32bpp = 4字节/像素
                int origStride = origData.Stride;
                int origBufferSize = origStride * original.Height;
                byte[] origBuffer = new byte[origBufferSize];

                // 创建有效像素地图（标记哪些位置有有效数据）
                bool[,] validMap = new bool[original.Width, original.Height];

                // 锁定所有分割图像并填充有效像素地图
                for (int i = 0; i < 4; i++)
                {
                    if (!haveImage[i]) continue;

                    splitData[i] = splitBitmaps[i].LockBits(
                        new Rectangle(0, 0, halfWidth, halfHeight),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    int splitStride = splitData[i].Stride;
                    int splitBufferSize = splitStride * halfHeight;
                    byte[] splitBuffer = new byte[splitBufferSize];
                    System.Runtime.InteropServices.Marshal.Copy(
                        splitData[i].Scan0, splitBuffer, 0, splitBufferSize);

                    // 填充有效像素地图
                    for (int y = 0; y < halfHeight; y++)
                    {
                        for (int x = 0; x < halfWidth; x++)
                        {
                            int origX = 0, origY = 0;

                            // 根据分割图索引确定原始位置
                            switch (i)
                            {
                                case 0: // 左上
                                    origX = x * 2;
                                    origY = y * 2;
                                    break;
                                case 1: // 右上
                                    origX = x * 2 + 1;
                                    origY = y * 2;
                                    break;
                                case 2: // 左下
                                    origX = x * 2;
                                    origY = y * 2 + 1;
                                    break;
                                case 3: // 右下
                                    origX = x * 2 + 1;
                                    origY = y * 2 + 1;
                                    break;
                            }

                            // 标记有效位置
                            validMap[origX, origY] = true;

                            // 将像素复制到原始缓冲区
                            int splitIndex = y * splitStride + x * pixelSize;
                            int origIndex = origY * origStride + origX * pixelSize;

                            Buffer.BlockCopy(splitBuffer, splitIndex, origBuffer, origIndex, pixelSize);
                        }
                    }
                }

                // 卷积核尺寸（5x5）
                const int kernelSize = 5;
                const int kernelRadius = kernelSize / 2;

                // 高斯卷积核（用于加权平均）
                float[,] kernel = CreateGaussianKernel(kernelSize, 1.0f);

                // 使用卷积计算填充缺失像素
                for (int y = 0; y < original.Height; y++)
                {
                    for (int x = 0; x < original.Width; x++)
                    {
                        int origIndex = y * origStride + x * pixelSize;

                        // 如果已有有效像素，跳过
                        if (validMap[x, y]) continue;

                        // 收集周围有效像素
                        List<ColorWeight> neighbors = new List<ColorWeight>();

                        for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                        {
                            int ny = y + ky;
                            if (ny < 0 || ny >= original.Height) continue;

                            for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                            {
                                int nx = x + kx;
                                if (nx < 0 || nx >= original.Width) continue;

                                // 跳过自身位置
                                if (nx == x && ny == y) continue;

                                // 只考虑有效像素
                                if (validMap[nx, ny])
                                {
                                    int neighborIndex = ny * origStride + nx * pixelSize;
                                    float weight = kernel[ky + kernelRadius, kx + kernelRadius];

                                    neighbors.Add(new ColorWeight
                                    {
                                        B = origBuffer[neighborIndex + 0],
                                        G = origBuffer[neighborIndex + 1],
                                        R = origBuffer[neighborIndex + 2],
                                        A = origBuffer[neighborIndex + 3],
                                        Weight = weight
                                    });
                                }
                            }
                        }

                        // 如果有有效邻居，使用加权平均填充缺失像素
                        if (neighbors.Count > 0)
                        {
                            Color weightedAvg = CalculateWeightedAverage(neighbors);

                            origBuffer[origIndex + 0] = weightedAvg.B; // Blue
                            origBuffer[origIndex + 1] = weightedAvg.G; // Green
                            origBuffer[origIndex + 2] = weightedAvg.R; // Red
                            origBuffer[origIndex + 3] = weightedAvg.A; // Alpha

                            // 标记为已填充（可选）
                            validMap[x, y] = true;
                        }
                        else
                        {
                            // 没有有效邻居，使用默认值（黑色）
                            origBuffer[origIndex + 0] = 0; // Blue
                            origBuffer[origIndex + 1] = 0; // Green
                            origBuffer[origIndex + 2] = 0; // Red
                            origBuffer[origIndex + 3] = 255; // Alpha
                        }
                    }
                }

                // 将数据复制回原始位图
                System.Runtime.InteropServices.Marshal.Copy(
                    origBuffer, 0, origData.Scan0, origBuffer.Length);
            }
            finally
            {
                original.UnlockBits(origData);

                // 解锁所有分割图像
                for (int i = 0; i < 4; i++)
                {
                    if (splitBitmaps[i] != null && splitData[i] != null)
                        splitBitmaps[i].UnlockBits(splitData[i]);
                }
            }

            return original;
        }

        // 创建高斯卷积核
        private static float[,] CreateGaussianKernel(int size, float sigma)
        {
            float[,] kernel = new float[size, size];
            float sum = 0f;
            int radius = size / 2;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float value = (float)Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[y + radius, x + radius] = value;
                    sum += value;
                }
            }

            // 归一化
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= sum;
                }
            }

            return kernel;
        }

        // 计算加权平均颜色
        private static Color CalculateWeightedAverage(List<ColorWeight> colors)
        {
            float totalA = 0, totalR = 0, totalG = 0, totalB = 0;
            float totalWeight = 0;

            foreach (var cw in colors)
            {
                totalA += cw.A * cw.Weight;
                totalR += cw.R * cw.Weight;
                totalG += cw.G * cw.Weight;
                totalB += cw.B * cw.Weight;
                totalWeight += cw.Weight;
            }

            if (totalWeight == 0) return Color.Black;

            return Color.FromArgb(
                (byte)Clamp(totalA / totalWeight, 0, 255),
                (byte)Clamp(totalR / totalWeight, 0, 255),
                (byte)Clamp(totalG / totalWeight, 0, 255),
                (byte)Clamp(totalB / totalWeight, 0, 255));
        }

        // 辅助结构：带权重的颜色
        private struct ColorWeight
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
            public float Weight;
        }

        // 根据接收情况重建2x2像素块
        private static void RebuildBlock(
            Color?[] pixels,
            byte[] origBuffer,
            int indexTL, int indexTR, int indexBL, int indexBR)
        {
            // 统计有效像素数量
            int validCount = pixels.Count(p => p.HasValue);

            // 情况1: 所有像素都有效
            if (validCount == 4)
            {
                SetColor(origBuffer, indexTL, pixels[0].Value);
                SetColor(origBuffer, indexTR, pixels[1].Value);
                SetColor(origBuffer, indexBL, pixels[2].Value);
                SetColor(origBuffer, indexBR, pixels[3].Value);
                return;
            }

            // 情况2: 只有单个像素有效
            if (validCount == 1)
            {
                Color color = pixels.First(p => p.HasValue).Value;
                SetColor(origBuffer, indexTL, color);
                SetColor(origBuffer, indexTR, color);
                SetColor(origBuffer, indexBL, color);
                SetColor(origBuffer, indexBR, color);
                return;
            }

            // 情况3: 水平相邻像素有效 (0和1 或 2和3)
            if ((pixels[0].HasValue && pixels[1].HasValue) ||
                (pixels[2].HasValue && pixels[3].HasValue))
            {
                // 处理第一行 (0和1)
                if (pixels[0].HasValue && pixels[1].HasValue)
                {
                    SetColor(origBuffer, indexTL, pixels[0].Value);
                    SetColor(origBuffer, indexTR, pixels[1].Value);

                    // 第二行复制第一行
                    SetColor(origBuffer, indexBL, pixels[0].Value);
                    SetColor(origBuffer, indexBR, pixels[1].Value);
                }
                // 处理第二行 (2和3)
                else
                {
                    SetColor(origBuffer, indexTL, pixels[2].Value);
                    SetColor(origBuffer, indexTR, pixels[3].Value);

                    // 第一行复制第二行
                    SetColor(origBuffer, indexBL, pixels[2].Value);
                    SetColor(origBuffer, indexBR, pixels[3].Value);
                }
                return;
            }

            // 情况4: 对角线像素有效 (0和3 或 1和2)
            if ((pixels[0].HasValue && pixels[3].HasValue) ||
                (pixels[1].HasValue && pixels[2].HasValue))
            {
                // 处理主对角线 (0和3)
                if (pixels[0].HasValue && pixels[3].HasValue)
                {
                    SetColor(origBuffer, indexTL, pixels[0].Value);
                    SetColor(origBuffer, indexBR, pixels[3].Value);

                    // 混合颜色填充另外两个位置
                    Color mixed = MixColors(pixels[0].Value, pixels[3].Value);
                    SetColor(origBuffer, indexTR, mixed);
                    SetColor(origBuffer, indexBL, mixed);
                }
                // 处理副对角线 (1和2)
                else
                {
                    SetColor(origBuffer, indexTR, pixels[1].Value);
                    SetColor(origBuffer, indexBL, pixels[2].Value);

                    // 混合颜色填充另外两个位置
                    Color mixed = MixColors(pixels[1].Value, pixels[2].Value);
                    SetColor(origBuffer, indexTL, mixed);
                    SetColor(origBuffer, indexBR, mixed);
                }
                return;
            }

            // 情况5: 三个像素有效
            if (validCount == 3)
            {
                // 找出缺失的像素位置
                int missingIndex = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (!pixels[i].HasValue)
                    {
                        missingIndex = i;
                        break;
                    }
                }

                // 计算有效像素的平均颜色
                Color avgColor = CalculateAverageColor(pixels.Where(p => p.HasValue).Select(p => p.Value));

                // 设置所有像素
                SetColor(origBuffer, indexTL, pixels[0] ?? avgColor);
                SetColor(origBuffer, indexTR, pixels[1] ?? avgColor);
                SetColor(origBuffer, indexBL, pixels[2] ?? avgColor);
                SetColor(origBuffer, indexBR, pixels[3] ?? avgColor);

                // 特别处理缺失的像素位置（使用平均值）
                switch (missingIndex)
                {
                    case 0: SetColor(origBuffer, indexTL, avgColor); break;
                    case 1: SetColor(origBuffer, indexTR, avgColor); break;
                    case 2: SetColor(origBuffer, indexBL, avgColor); break;
                    case 3: SetColor(origBuffer, indexBR, avgColor); break;
                }
                return;
            }

            // 情况6: 没有有效像素（默认黑色）
            SetColor(origBuffer, indexTL, Color.Black);
            SetColor(origBuffer, indexTR, Color.Black);
            SetColor(origBuffer, indexBL, Color.Black);
            SetColor(origBuffer, indexBR, Color.Black);
        }

        // 混合两种颜色（取平均）
        private static Color MixColors(Color c1, Color c2)
        {
            return Color.FromArgb(
                (c1.A + c2.A) / 2,
                (c1.R + c2.R) / 2,
                (c1.G + c2.G) / 2,
                (c1.B + c2.B) / 2);
        }

        // 计算多种颜色的平均值
        private static Color CalculateAverageColor(System.Collections.Generic.IEnumerable<Color> colors)
        {
            int a = 0, r = 0, g = 0, b = 0;
            int count = 0;

            foreach (Color color in colors)
            {
                a += color.A;
                r += color.R;
                g += color.G;
                b += color.B;
                count++;
            }

            if (count == 0) return Color.Black;

            return Color.FromArgb(
                a / count,
                r / count,
                g / count,
                b / count);
        }

        // 设置颜色到缓冲区
        private static void SetColor(byte[] buffer, int index, Color color)
        {
            buffer[index + 0] = color.B; // Blue
            buffer[index + 1] = color.G; // Green
            buffer[index + 2] = color.R; // Red
            buffer[index + 3] = color.A; // Alpha
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static byte Clamp(double value, byte min, byte max)
        {
            return (byte)Math.Max(min, Math.Min(max, value));
        }
    }
}
