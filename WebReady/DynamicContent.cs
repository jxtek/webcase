﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebCase
{
    /// <summary>
    /// A dynamically generated content in format of either bytes or chars. It always uses the buffer pool. 
    /// </summary>
    public abstract class DynamicContent : HttpContent, IContent
    {
        static readonly char[] DIGIT =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
        };

        // sexagesimal numbers
        protected static readonly string[] SEX =
        {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59"
        };

        protected static readonly short[] SHORT =
        {
            1,
            10,
            100,
            1000,
            10000
        };

        protected static readonly int[] INT =
        {
            1,
            10,
            100,
            1000,
            10000,
            100000,
            1000000,
            10000000,
            100000000,
            1000000000
        };

        protected static readonly long[] LONG =
        {
            1L,
            10L,
            100L,
            1000L,
            10000L,
            100000L,
            1000000L,
            10000000L,
            100000000L,
            1000000000L,
            10000000000L,
            100000000000L,
            1000000000000L,
            10000000000000L,
            100000000000000L,
            1000000000000000L,
            10000000000000000L,
            100000000000000000L,
            1000000000000000000L
        };

        // NOTE: HttpResponseStream doesn't have internal buffer
        protected byte[] bytebuf;

        protected char[] charbuf;

        // number of bytes or chars
        protected int count;

        // byte-wise etag checksum, for char-based output only
        protected ulong checksum;

        protected DynamicContent(bool binary, int capacity)
        {
            if (binary)
            {
                bytebuf = BufferUtility.GetByteBuffer(capacity);
            }
            else
            {
                charbuf = BufferUtility.GetCharBuffer(capacity);
            }
            count = 0;
        }

        public abstract string Type { get; set; }

        public bool IsBinary => bytebuf != null;

        public byte[] ByteBuffer => bytebuf;

        public char[] CharBuffer => charbuf;

        public int Size => count;

        string etag;

        public string ETag => etag ?? (etag = TextUtility.ToHex(checksum));

        void AddByte(byte b)
        {
            // ensure capacity
            int olen = bytebuf.Length; // old length
            if (count >= olen)
            {
                int nlen = olen * 4; // new length
                byte[] obuf = bytebuf;
                bytebuf = BufferUtility.GetByteBuffer(nlen);
                Array.Copy(obuf, 0, bytebuf, 0, olen);
                BufferUtility.Return(obuf);
            }
            bytebuf[count++] = b;

            // calculate checksum
            ulong cs = checksum;
            cs ^= b; // XOR
            checksum = cs >> 57 | cs << 7; // circular left shift 7 bit
        }

        public void Add(char c)
        {
            if (IsBinary) // byte-oriented
            {
                // UTF-8 encoding but without surrogate support
                if (c < 0x80)
                {
                    // have at most seven bits
                    AddByte((byte) c);
                }
                else if (c < 0x800)
                {
                    // 2 char, 11 bits
                    AddByte((byte) (0xc0 | (c >> 6)));
                    AddByte((byte) (0x80 | (c & 0x3f)));
                }
                else
                {
                    // 3 char, 16 bits
                    AddByte((byte) (0xe0 | ((c >> 12))));
                    AddByte((byte) (0x80 | ((c >> 6) & 0x3f)));
                    AddByte((byte) (0x80 | (c & 0x3f)));
                }
            }
            else // char-oriented
            {
                // ensure capacity
                int olen = charbuf.Length; // old length
                if (count >= olen)
                {
                    int nlen = olen * 4; // new length
                    char[] obuf = charbuf;
                    charbuf = BufferUtility.GetCharBuffer(nlen);
                    Array.Copy(obuf, 0, charbuf, 0, olen);
                    BufferUtility.Return(obuf);
                }
                charbuf[count++] = c;
            }
        }

        public void Add(bool v)
        {
            Add(v ? "true" : "false");
        }

        public void Add(char[] v)
        {
            if (v == null) return;
            Add(v, 0, v.Length);
        }

        public void Add(char[] v, int offset, int len)
        {
            if (v == null) return;
            for (int i = offset; i < len; i++)
            {
                Add(v[i]);
            }
        }

        public void Add(string v)
        {
            if (v == null) return;
            Add(v, 0, v.Length);
        }

        public void Add(string v, int offset, int length)
        {
            if (v == null) return;
            int len = Math.Min(length, v.Length);
            for (int i = offset; i < len; i++)
            {
                Add(v[i]);
            }
        }

        public void Add(StringBuilder v)
        {
            if (v == null) return;
            Add(v, 0, v.Length);
        }

        public void Add(StringBuilder v, int offset, int len)
        {
            if (v == null) return;
            for (int i = offset; i < len; i++)
            {
                Add(v[i]);
            }
        }

        public void Add(sbyte v)
        {
            Add((short) v);
        }

        public void Add(byte v)
        {
            Add((short) v);
        }

        public void Add(short v)
        {
            if (v == 0)
            {
                Add('0');
                return;
            }
            int x = v; // convert to int
            if (v < 0)
            {
                Add('-');
                x = -x;
            }
            bool bgn = false;
            for (int i = SHORT.Length - 1; i > 0; i--)
            {
                int bas = SHORT[i];
                int q = x / bas;
                x = x % bas;
                if (q != 0 || bgn)
                {
                    Add(DIGIT[q]);
                    bgn = true;
                }
            }
            Add(DIGIT[x]); // last reminder
        }

        public void Add(int v)
        {
            if (v >= short.MinValue && v <= short.MaxValue)
            {
                Add((short) v);
                return;
            }

            if (v < 0)
            {
                Add('-');
                v = -v;
            }
            bool bgn = false;
            for (int i = INT.Length - 1; i > 0; i--)
            {
                int bas = INT[i];
                int q = v / bas;
                v = v % bas;
                if (q != 0 || bgn)
                {
                    Add(DIGIT[q]);
                    bgn = true;
                }
            }
            Add(DIGIT[v]); // last reminder
        }

        public void Add(long v)
        {
            if (v >= int.MinValue && v <= int.MaxValue)
            {
                Add((int) v);
                return;
            }

            if (v < 0)
            {
                Add('-');
                v = -v;
            }
            bool bgn = false;
            for (int i = LONG.Length - 1; i > 0; i--)
            {
                long bas = LONG[i];
                long q = v / bas;
                v = v % bas;
                if (q != 0 || bgn)
                {
                    Add(DIGIT[q]);
                    bgn = true;
                }
            }
            Add(DIGIT[v]); // last reminder
        }

        public void Add(double v)
        {
            Add(v.ToString(CultureInfo.CurrentCulture));
        }

        // sign mask
        const int Sign = unchecked((int) 0x80000000);

        ///
        /// This method outputs decimal numbers fastly.
        ///
        public void Add(decimal v)
        {
            int[] bits = decimal.GetBits(v); // get the binary representation
            int low = bits[0], mid = bits[1], hi = bits[2], flags = bits[3];
            int scale = (bits[3] >> 16) & 0x7F;

            if (hi != 0) // if 96 bits, use system api
            {
                Add(v.ToString(CultureInfo.CurrentCulture));
                return;
            }

            // output a minus if negative
            if ((flags & Sign) != 0)
            {
                Add('-');
            }

            if (mid != 0) // if 64 bits
            {
                long x = ((long) mid << 32) + low;
                bool bgn = false;
                for (int i = LONG.Length - 1; i > 0; i--)
                {
                    long bas = LONG[i];
                    long q = x / bas;
                    x = x % bas;
                    if (q != 0 || bgn)
                    {
                        Add(DIGIT[q]);
                        bgn = true;
                    }
                    if (i == scale)
                    {
                        if (!bgn)
                        {
                            Add('0'); // 0.XX
                            bgn = true;
                        }
                        Add('.');
                    }
                }
                Add(DIGIT[x]); // last reminder
            }
            else // 32 bits
            {
                int x = low;
                bool bgn = false;
                for (int i = INT.Length - 1; i > 0; i--)
                {
                    int bas = INT[i];
                    int q = x / bas;
                    x = x % bas;
                    if (q != 0 || bgn)
                    {
                        Add(DIGIT[q]);
                        bgn = true;
                    }
                    if (i == scale)
                    {
                        if (!bgn)
                        {
                            Add('0'); // 0.XX
                            bgn = true;
                        }
                        Add('.');
                    }
                }
                Add(DIGIT[x]); // last reminder
            }

            // to pad extra zeros for monetary output
            if (scale == 0)
            {
                Add(".00");
            }
            else if (scale == 1)
            {
                Add('0');
            }
        }

        public void Add(JNumber v)
        {
            Add(v.Long);
            if (v.Pt)
            {
                Add('.');
                Add(v.fract);
            }
        }

        public void Add(DateTime v, bool time = false)
        {
            short yr = (short) v.Year;

            // yyyy-mm-dd
            if (yr < 1000) Add('0');
            if (yr < 100) Add('0');
            if (yr < 10) Add('0');
            Add(v.Year);
            Add('-');
            Add(SEX[v.Month]);
            Add('-');
            Add(SEX[v.Day]);

            if (time)
            {
                Add(' '); // a space for separation
                Add(SEX[v.Hour]);
                Add(':');
                Add(SEX[v.Minute]);
                Add(':');
                Add(SEX[v.Second]);
            }
        }

        //
        // CLIENT CONTENT
        //
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(bytebuf, 0, count);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = count;
            return true;
        }

        public ArraySegment<byte> ToBytea()
        {
            return new ArraySegment<byte>(bytebuf, 0, count);
        }

        public override string ToString()
        {
            return charbuf == null ? null : new string(charbuf, 0, count);
        }
    }
}