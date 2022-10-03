using System;
using System.Collections.Generic;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class LA : IImageFormat
    {
        public int BitDepth { get; set; }
        public string FormatName { get; set; }
        private int LDepth { get; set; }
        private int ADepth { get; set; }
        private ByteOrder ByteOrder { get; set; }

        public LA(int l, int a, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            int bitDepth = a + l;

            if (bitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (bitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (bitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (l < 4 && a < 4) throw new Exception($"Luminance and Alpha value can't be smaller than 4.\nGiven Luminance: {l}; Given Alpha: {a}");

            this.BitDepth = bitDepth;
            this.LDepth = l;
            this.ADepth = a;
            this.FormatName = ((l != 0) ? "L" : "") + ((a != 0) ? "A" : "") + ((l != 0) ? l.ToString() : "") + ((a != 0) ? a.ToString() : "");
            this.ByteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (MemoryStream ms = new MemoryStream(tex))
            {
                using (BinaryReaderX br = new BinaryReaderX(ms, this.ByteOrder))
                {
                    int lShift = this.ADepth;
                    int aBitMask = (1 << this.ADepth) - 1;
                    int lBitMask = (1 << this.LDepth) - 1;

                    while (true)
                    {
                        long value = 0;

                        switch (BitDepth)
                        {
                            case 4:
                                value = br.ReadNibble();
                                break;
                            case 8:
                                value = br.ReadByte();
                                break;
                            case 16:
                                value = br.ReadUInt16();
                                break;
                            default:
                                throw new Exception($"BitDepth {BitDepth} not supported!");
                        }

                        yield return Color.FromArgb(
                            (ADepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value & aBitMask), ADepth, 8),
                            (LDepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value >> lShift & lBitMask), LDepth, 8),
                            (LDepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value >> lShift & lBitMask), LDepth, 8),
                            (LDepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value >> lShift & lBitMask), LDepth, 8));
                    }
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriterX bw = new BinaryWriterX(ms, true, this.ByteOrder))
                {
                    foreach (Color color in colors)
                    {
                        int a = (ADepth == 0) ? 0 : Support.Support.ChangeBitDepth(color.A, 8, ADepth);
                        int l = (LDepth == 0) ? 0 : Support.Support.ChangeBitDepth(color.G, 8, LDepth);
                        int lShift = ADepth;

                        long value = a;
                        value |= (uint)(l << lShift);

                        switch (BitDepth)
                        {
                            case 4:
                                bw.WriteNibble((int)value);
                                break;
                            case 8:
                                bw.Write((byte)value);
                                break;
                            case 16:
                                bw.Write((ushort)value);
                                break;
                            default:
                                throw new Exception($"BitDepth {BitDepth} not supported!");
                        }
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}
