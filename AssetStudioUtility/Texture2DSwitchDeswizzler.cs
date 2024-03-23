/*
Reference: https://github.com/nesrak1/UABEA/blob/239a112cc31cb46f9da235471a7b4d74894640fb/TexturePlugin/Texture2DSwitchDeswizzler.cs

MIT License

Copyright (c) 2021 nesrak1

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace AssetStudio
{
    public class Texture2DSwitchDeswizzler
    {
        // referring to block here as a compressed texture block, not a gob one
        const int GOB_X_BLOCK_COUNT = 4;
        const int GOB_Y_BLOCK_COUNT = 8;
        const int BLOCKS_IN_GOB = GOB_X_BLOCK_COUNT * GOB_Y_BLOCK_COUNT;

        /*
        sector:
        A
        B         

        gob (made of sectors):
        ABIJ
        CDKL
        EFMN
        GHOP

        gob blocks (example with height 2):
        ACEGIK... from left to right of image
        BDFHJL...
        --------- start new row of blocks
        MOQSUW...
        NPRTVX...
        */

        private static void CopyBlock(Image<Bgra32> srcImage, Image<Bgra32> dstImage, int sbx, int sby, int dbx, int dby, int blockSizeW, int blockSizeH)
        {
            for (int i = 0; i < blockSizeW; i++)
            {
                for (int j = 0; j < blockSizeH; j++)
                {
                    dstImage[dbx * blockSizeW + i, dby * blockSizeH + j] = srcImage[sbx * blockSizeW + i, sby * blockSizeH + j];
                }
            }
        }

        private static int CeilDivide(int a, int b)
        {
            return (a + b - 1) / b;
        }

        internal static Image<Bgra32> SwitchUnswizzle(Image<Bgra32> srcImage, Size blockSize, int gobsPerBlock)
        {
            Image<Bgra32> dstImage = new Image<Bgra32>(srcImage.Width, srcImage.Height);

            int width = srcImage.Width;
            int height = srcImage.Height;

            int blockCountX = CeilDivide(width, blockSize.Width);
            int blockCountY = CeilDivide(height, blockSize.Height);

            int gobCountX = blockCountX / GOB_X_BLOCK_COUNT;
            int gobCountY = blockCountY / GOB_Y_BLOCK_COUNT;

            int srcX = 0;
            int srcY = 0;
            for (int i = 0; i < gobCountY / gobsPerBlock; i++)
            {
                for (int j = 0; j < gobCountX; j++)
                {
                    for (int k = 0; k < gobsPerBlock; k++)
                    {
                        for (int l = 0; l < BLOCKS_IN_GOB; l++)
                        {
                            // todo: use table for speedy boi
                            int gobX = ((l >> 3) & 0b10) | ((l >> 1) & 0b1);
                            int gobY = ((l >> 1) & 0b110) | (l & 0b1);
                            int gobDstX = j * GOB_X_BLOCK_COUNT + gobX;
                            int gobDstY = (i * gobsPerBlock + k) * GOB_Y_BLOCK_COUNT + gobY;
                            CopyBlock(srcImage, dstImage, srcX, srcY, gobDstX, gobDstY, blockSize.Width, blockSize.Height);

                            srcX++;
                            if (srcX >= blockCountX)
                            {
                                srcX = 0;
                                srcY++;
                            }
                        }
                    }
                }
            }

            return dstImage;
        }

        internal static Image<Bgra32> SwitchSwizzle(Image<Bgra32> srcImage, Size blockSize, int gobsPerBlock)
        {
            Image<Bgra32> dstImage = new Image<Bgra32>(srcImage.Width, srcImage.Height);

            int width = srcImage.Width;
            int height = srcImage.Height;

            int blockCountX = CeilDivide(width, blockSize.Width);
            int blockCountY = CeilDivide(height, blockSize.Height);

            int gobCountX = blockCountX / GOB_X_BLOCK_COUNT;
            int gobCountY = blockCountY / GOB_Y_BLOCK_COUNT;

            int dstX = 0;
            int dstY = 0;
            for (int i = 0; i < gobCountY / gobsPerBlock; i++)
            {
                for (int j = 0; j < gobCountX; j++)
                {
                    for (int k = 0; k < gobsPerBlock; k++)
                    {
                        for (int l = 0; l < BLOCKS_IN_GOB; l++)
                        {
                            // todo: use table for speedy boi
                            int gobX = ((l >> 3) & 0b10) | ((l >> 1) & 0b1);
                            int gobY = ((l >> 1) & 0b110) | (l & 0b1);
                            int gobSrcX = j * GOB_X_BLOCK_COUNT + gobX;
                            int gobSrcY = (i * gobsPerBlock + k) * GOB_Y_BLOCK_COUNT + gobY;
                            CopyBlock(srcImage, dstImage, gobSrcX, gobSrcY, dstX, dstY, blockSize.Width, blockSize.Height);

                            dstX++;
                            if (dstX >= blockCountX)
                            {
                                dstX = 0;
                                dstY++;
                            }
                        }
                    }
                }
            }

            return dstImage;
        }

        // this should be the amount of pixels that can fit 16 bytes
        internal static Size TextureFormatToBlockSize(TextureFormat m_TextureFormat)
        {
            switch (m_TextureFormat)
            {
                case TextureFormat.Alpha8: return new Size(16, 1); // 1 byte per pixel
                case TextureFormat.ARGB4444: return new Size(8, 1); // 2 bytes per pixel
                case TextureFormat.RGBA32: return new Size(4, 1); // 4 bytes per pixel
                case TextureFormat.ARGB32: return new Size(4, 1); // 4 bytes per pixel
                case TextureFormat.ARGBFloat: return new Size(1, 1); // 16 bytes per pixel (?)
                case TextureFormat.RGB565: return new Size(8, 1); // 2 bytes per pixel
                case TextureFormat.R16: return new Size(8, 1); // 2 bytes per pixel
                case TextureFormat.DXT1: return new Size(8, 4); // 8 bytes per 4x4=16 pixels
                case TextureFormat.DXT5: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.RGBA4444: return new Size(8, 1); // 2 bytes per pixel
                case TextureFormat.BGRA32: return new Size(4, 1); // 4 bytes per pixel
                case TextureFormat.BC6H: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.BC7: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.BC4: return new Size(8, 4); // 8 bytes per 4x4=16 pixels
                case TextureFormat.BC5: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.ASTC_RGB_4x4: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.ASTC_RGB_5x5: return new Size(5, 5); // 16 bytes per 5x5=25 pixels
                case TextureFormat.ASTC_RGB_6x6: return new Size(6, 6); // 16 bytes per 6x6=36 pixels
                case TextureFormat.ASTC_RGB_8x8: return new Size(8, 8); // 16 bytes per 8x8=64 pixels
                case TextureFormat.ASTC_RGB_10x10: return new Size(10, 10); // 16 bytes per 10x10=100 pixels
                case TextureFormat.ASTC_RGB_12x12: return new Size(12, 12); // 16 bytes per 12x12=144 pixels
                case TextureFormat.ASTC_RGBA_4x4: return new Size(4, 4); // 16 bytes per 4x4=16 pixels
                case TextureFormat.ASTC_RGBA_5x5: return new Size(5, 5); // 16 bytes per 5x5=25 pixels
                case TextureFormat.ASTC_RGBA_6x6: return new Size(6, 6); // 16 bytes per 6x6=36 pixels
                case TextureFormat.ASTC_RGBA_8x8: return new Size(8, 8); // 16 bytes per 8x8=64 pixels
                case TextureFormat.ASTC_RGBA_10x10: return new Size(10, 10); // 16 bytes per 10x10=100 pixels
                case TextureFormat.ASTC_RGBA_12x12: return new Size(12, 12); // 16 bytes per 12x12=144 pixels
                case TextureFormat.RG16: return new Size(8, 1); // 2 bytes per pixel
                case TextureFormat.R8: return new Size(16, 1); // 1 byte per pixel
                default: throw new NotImplementedException();
            };
        }

        internal static Size GetPaddedTextureSize(int width, int height, int blockWidth, int blockHeight, int gobsPerBlock)
        {
            width = CeilDivide(width, blockWidth * GOB_X_BLOCK_COUNT) * blockWidth * GOB_X_BLOCK_COUNT;
            height = CeilDivide(height, blockHeight * GOB_Y_BLOCK_COUNT * gobsPerBlock) * blockHeight * GOB_Y_BLOCK_COUNT * gobsPerBlock;
            return new Size(width, height);
        }

        internal static int GetSwitchGobsPerBlock(byte[] platformBlob)
        {
            // apparently there is another value to worry about, but seeing as it's
            // always 0 and I have nothing else to test against, this will probably
            // work fine for now
            return 1 << BitConverter.ToInt32(platformBlob, 8);
        }
    }
}
