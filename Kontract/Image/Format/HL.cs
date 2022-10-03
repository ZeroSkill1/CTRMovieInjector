using System;
using System.Collections.Generic;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class HL : IImageFormat
    {
        public int BitDepth { get; set; }
        public string FormatName { get; set; }
        private int RDepth { get; set; }
        private int GDepth { get; set; }
        private ByteOrder byteOrder { get; set; }

        public HL(int r, int g, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = r + g;
            if (BitDepth % 4 != 0) throw new Exception($"Overall bitDepth has to be dividable by 4. Given bitDepth: {BitDepth}");
            if (BitDepth > 16) throw new Exception($"Overall bitDepth can't be bigger than 16. Given bitDepth: {BitDepth}");
            if (BitDepth < 4) throw new Exception($"Overall bitDepth can't be smaller than 4. Given bitDepth: {BitDepth}");
            if (r < 4 && g < 4) throw new Exception($"Red and Green value can't be smaller than 4.\nGiven Red: {r}; Given Green: {g}");

            this.RDepth = r;
            this.GDepth = g;
            this.FormatName = "HL" + r.ToString() + g.ToString();
            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (MemoryStream ms = new MemoryStream(tex))
            {
                using (BinaryReaderX br = new BinaryReaderX(ms, byteOrder))
                {
                    int rShift = GDepth;
                    int gBitMask = (1 << GDepth) - 1;
                    int rBitMask = (1 << RDepth) - 1;

                    while (br.BaseStream.Position < br.BaseStream.Length)
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
                            255,
                            (RDepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value >> rShift & rBitMask), RDepth, 8),
                            (GDepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value & gBitMask), GDepth, 8),
                            255);
                    }
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriterX bw = new BinaryWriterX(ms, true, byteOrder))
                {
                    foreach (Color color in colors)
                    {
                        int r = (RDepth == 0) ? 0 : Support.Support.ChangeBitDepth(color.R, 8, RDepth);
                        int g = (GDepth == 0) ? 0 : Support.Support.ChangeBitDepth(color.G, 8, GDepth);
                        int rShift = GDepth;

                        long value = g;

                        value |= (uint)(r << rShift);

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
                }

                return ms.ToArray();
            }
        }
    }
}
