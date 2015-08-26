//! \file       BitStream.cs
//! \date       Sat Aug 22 21:33:39 2015
//! \brief      Bit stream on top of the IO.Stream
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
using System.Diagnostics;
using System.IO;

namespace GameRes.Formats
{
    public class MsbBitStream : IDisposable
    {
        Stream      m_input;
        bool        m_should_dispose;

        public Stream Input { get { return m_input; } }

        public MsbBitStream (Stream file, bool leave_open = false)
        {
            m_input = file;
            m_should_dispose = !leave_open;
        }

        int m_bits = 0;
        int m_cached_bits = 0;

        public void Reset ()
        {
            m_cached_bits = 0;
        }

        public int GetNextBit ()
        {
            return GetBits (1);
        }

        public int GetBits (int count)
        {
            Debug.Assert (count <= 24, "MsbBitStream does not support sequences longer than 24 bits");
            while (m_cached_bits < count)
            {
                int b = m_input.ReadByte();
                if (-1 == b)
                    return -1;
                m_bits = (m_bits << 8) | b;
                m_cached_bits += 8;
            }
            int mask = (1 << count) - 1;
            m_cached_bits -= count;
            return (m_bits >> m_cached_bits) & mask;
        }

        #region IDisposable Members
        bool m_disposed = false;

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing && m_should_dispose && null != m_input)
                    m_input.Dispose();
                m_disposed = true;
            }
        }
        #endregion
    }
}
