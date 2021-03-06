//! \file       ImageBMP.cs
//! \date       Wed Jul 16 18:06:47 2014
//! \brief      BMP image implementation.
//
// Copyright (C) 2014-2016 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using GameRes.Utility;
using System.Windows.Media;

namespace GameRes
{
    public class BmpMetaData : ImageMetaData
    {
        public uint ImageLength;
        public uint HeaderLength;
    }

    [Export(typeof(ImageFormat))]
    public class BmpFormat : ImageFormat
    {
        public override string Tag { get { return "BMP"; } }
        public override string Description { get { return "Windows device independent bitmap"; } }
        public override uint Signature { get { return 0; } }

        public override ImageData Read (Stream file, ImageMetaData info)
        {
            var bmp_info = info as BmpMetaData;
            if (bmp_info != null && file.CanSeek)
            {
                uint bmp_length = info.Width * info.Height * (uint)info.BPP/8 + bmp_info.HeaderLength;
                if (bmp_length == bmp_info.ImageLength || bmp_length+2 == bmp_info.ImageLength)
                {
                    if (0x20 == info.BPP)
                    {
                        return ReadBitmapBGRA (file, bmp_info);
                    }
                    else if (0x18 == info.BPP
                            && (bmp_info.ImageLength + info.Width * info.Height) == file.Length)
                    {
                        return ReadBitmapWithAlpha (file, bmp_info);
                    }
                }
            }
            var decoder = new BmpBitmapDecoder (file,
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            BitmapSource frame = decoder.Frames.First();
            frame.Freeze();
            return new ImageData (frame, info);
        }

        public override void Write (Stream file, ImageData image)
        {
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add (BitmapFrame.Create (image.Bitmap, null, null, null));
            encoder.Save (file);
        }

        private ImageData ReadBitmapWithAlpha (Stream file, BmpMetaData info)
        {
            file.Position = info.ImageLength;
            var alpha = new byte[info.Width*info.Height];
            if (alpha.Length != file.Read (alpha, 0, alpha.Length))
                throw new EndOfStreamException();

            file.Position = info.HeaderLength;
            int dst_stride = (int)info.Width * 4;
            var pixels = new byte[(int)info.Height * dst_stride];
            int a_src = 0;
            for (int y = (int)info.Height-1; y >= 0; --y)
            {
                int dst = dst_stride * y;
                for (int x = 0; x < dst_stride; x += 4)
                {
                    file.Read (pixels, dst+x, 3);
                    pixels[dst+x+3] = alpha[a_src++];
                }
            }
            return ImageData.Create (info, PixelFormats.Bgra32, null, pixels, dst_stride);
        }

        private ImageData ReadBitmapBGRA (Stream file, BmpMetaData info)
        {
            file.Position = info.HeaderLength;
            int stride = (int)info.Width * 4;
            var pixels = new byte[(int)info.Height * stride];
            bool has_alpha = false;
            for (int y = (int)info.Height-1; y >= 0; --y)
            {
                int dst = stride * y;
                file.Read (pixels, dst, stride);
                for (int x = 3; !has_alpha && x < stride; x += 4)
                    has_alpha = pixels[dst+x] != 0;
            }
            PixelFormat format = has_alpha ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
            return ImageData.Create (info, format, null, pixels, stride);
        }

        void SkipBytes (BinaryReader file, uint num)
        {
            if (file.BaseStream.CanSeek)
                file.BaseStream.Seek (num, SeekOrigin.Current);
            else
            {
                for (int i = 0; i < num / 4; ++i)
                    file.ReadInt32();
                for (int i = 0; i < num % 4; ++i)
                    file.ReadByte();
            }
        }

        public override ImageMetaData ReadMetaData (Stream stream)
        {
            int c1 = stream.ReadByte();
            int c2 = stream.ReadByte();
            if (0x42 != c1 || 0x4d != c2)
                return null;
            using (var file = new ArcView.Reader (stream))
            {
                uint size = file.ReadUInt32();
                if (size < 14+40)
                    return null;
                SkipBytes (file, 8);
                uint header_size = file.ReadUInt32();
                if (header_size < 40 || size-14 < header_size)
                    return null;
                uint width = file.ReadUInt32();
                uint height = file.ReadUInt32();
                file.ReadInt16();
                int bpp = file.ReadInt16();
                return new BmpMetaData {
                    Width = width,
                    Height = height,
                    OffsetX = 0,
                    OffsetY = 0,
                    BPP = bpp,
                    ImageLength = size,
                    HeaderLength = header_size + 14,
                };
            }
        }
    }
}
