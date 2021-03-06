//! \file       ImageABM.cs
//! \date       Tue Aug 04 22:58:17 2015
//! \brief      LiLiM/Le.Chocolat compressed image format.
//
// Copyright (C) 2015 by morkt
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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Utility;

namespace GameRes.Formats.Lilim
{
    [Export(typeof(ImageFormat))]
    public class AbmFormat : ImageFormat
    {
        public override string         Tag { get { return "ABM"; } }
        public override string Description { get { return "LiLiM/Le.Chocolat compressed bitmap"; } }
        public override uint     Signature { get { return 0; } }

        public override ImageMetaData ReadMetaData (Stream stream)
        {
            var header = new byte[0x46];
            if (header.Length != stream.Read (header, 0, header.Length))
                return null;
            if ('B' != header[0] || 'M' != header[1])
                return null;
            int type = (sbyte)header[0x1C];
            uint frame_offset;
            if (1 == type || 2 == type)
            {
                int count = LittleEndian.ToUInt16 (header, 0x3A);
                if (count > 0xFF)
                    return null;
                frame_offset = LittleEndian.ToUInt32 (header, 0x42);
            }
            else if (32 == type || 24 == type)
            {
                uint unpacked_size = LittleEndian.ToUInt32 (header, 2);
                if (unpacked_size == stream.Length) // probably an ordinary bmp file
                    return null;
                frame_offset = LittleEndian.ToUInt32 (header, 0xA);
            }
            else
                return null;
            if (frame_offset >= stream.Length)
                return null;
            return new AbmImageData
            {
                Width = LittleEndian.ToUInt32 (header, 0x12),
                Height = LittleEndian.ToUInt32 (header, 0x16),
                BPP = 24,
                Mode = type,
                BaseOffset = frame_offset,
            };
        }

        public override ImageData Read (Stream stream, ImageMetaData info)
        {
            var meta = info as AbmImageData;
            if (null == meta)
                throw new ArgumentException ("AbmFormat.Read should be supplied with AbmMetaData", "info");

            using (var reader = new AbmReader (stream, meta))
            {
                var pixels = reader.Unpack();
                PixelFormat format = 32 == reader.BPP ? PixelFormats.Bgra32 : PixelFormats.Bgr24;
                return ImageData.Create (info, format, null, pixels);
            }
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new System.NotImplementedException ("AbmFormat.Write not implemented");
        }
    }
}
