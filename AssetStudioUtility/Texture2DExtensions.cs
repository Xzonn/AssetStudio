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
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace AssetStudio
{
    public static class Texture2DExtensions
    {
        public static Image<Bgra32> ConvertToImage(this Texture2D m_Texture2D, bool flip)
        {
            int width = m_Texture2D.m_Width;
            int height = m_Texture2D.m_Height;
            TextureFormat format = m_Texture2D.m_TextureFormat;
            byte[] platformBlob = m_Texture2D.m_PlatformBlob;

            int originalWidth = width;
            int originalHeight = height;
            bool isSwitch = m_Texture2D.platform == BuildTarget.Switch && platformBlob != null && platformBlob.Length > 0;
            Size blockSize = new Size(1, 1), newSize = new Size(width, height);
            int gobsPerBlock = 0;

            if (isSwitch)
            {
                gobsPerBlock = Texture2DSwitchDeswizzler.GetSwitchGobsPerBlock(platformBlob);
                blockSize = Texture2DSwitchDeswizzler.TextureFormatToBlockSize(format);
                newSize = Texture2DSwitchDeswizzler.GetPaddedTextureSize(width, height, blockSize.Width, blockSize.Height, gobsPerBlock);
                width = newSize.Width;
                height = newSize.Height;
            }

            var converter = new Texture2DConverter(m_Texture2D, width, height, format);
            var buff = BigArrayPool<byte>.Shared.Rent(width * height * 4);
            try
            {
                if (!converter.DecodeTexture2D(buff))
                {
                    return null;
                }

                var image = Image.LoadPixelData<Bgra32>(buff, width, height);

                if (isSwitch)
                {
                    image = Texture2DSwitchDeswizzler.SwitchUnswizzle(image, blockSize, gobsPerBlock);
                    if (originalWidth != width || originalHeight != height)
                    {
                        image.Mutate(x => x.Crop(originalWidth, originalHeight));
                    }
                }

                if (flip)
                {
                    image.Mutate(x => x.Flip(FlipMode.Vertical));
                }

                return image;
            }
            finally
            {
                BigArrayPool<byte>.Shared.Return(buff);
            }
        }

        public static MemoryStream ConvertToStream(this Texture2D m_Texture2D, ImageFormat imageFormat, bool flip)
        {
            var image = ConvertToImage(m_Texture2D, flip);
            if (image != null)
            {
                using (image)
                {
                    return image.ConvertToStream(imageFormat);
                }
            }
            return null;
        }
    }
}
