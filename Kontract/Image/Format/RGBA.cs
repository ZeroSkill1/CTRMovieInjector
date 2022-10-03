using System;
using System.Collections.Generic;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class RGBA : IImageFormat
    {
        public int BitDepth { get; set; }
        public string FormatName { get; set; }
        private int RDepth { get; set; }
        private int GDepth { get; set; }
        private int BDepth { get; set; }
        private int ADepth { get; set; }
        private bool SwapChannels { get; set; }
        private ByteOrder ByteOrder { get; set; }


        public RGBA(int r, int g, int b, int a = 0, ByteOrder byteOrder = ByteOrder.LittleEndian, bool swapChannels = false, bool standard = false)
        {
            BitDepth = r + g + b + a;
            if (BitDepth < 8) throw new Exception($"Overall bitDepth can't be smaller than 8. Given bitDepth: {BitDepth}");
            if (BitDepth > 32) throw new Exception($"Overall bitDepth can't be bigger than 32. Given bitDepth: {BitDepth}");

            this.ByteOrder = byteOrder;
            this.SwapChannels = swapChannels;

            this.RDepth = r;
            this.GDepth = g;
            this.BDepth = b;
            this.ADepth = a;

            this.FormatName = ((standard) ? "s" : "") + ((swapChannels) ?
                ((a != 0) ? "A" : "") + "BGR" + ((a != 0) ? a.ToString() : "") + b.ToString() + g.ToString() + r.ToString() :   //ABGR
                "RGB" + ((a != 0) ? "A" : "") + r.ToString() + g.ToString() + b.ToString() + ((a != 0) ? a.ToString() : ""));   //RGBA
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (MemoryStream ms = new MemoryStream(tex))
            {
                using (BinaryReaderX br = new BinaryReaderX(ms, this.ByteOrder))
                {
                    int bShift, gShift, rShift, aShift;

                    if (this.SwapChannels)
                    {
                        rShift = 0;
                        gShift = this.RDepth;
                        bShift = gShift + this.GDepth;
                        aShift = bShift + this.BDepth;
                    }
                    else
                    {
                        aShift = 0;
                        bShift = this.ADepth;
                        gShift = bShift + this.BDepth;
                        rShift = gShift + this.GDepth;
                    }

                    int aBitMask = (1 << this.ADepth) - 1;
                    int bBitMask = (1 << this.BDepth) - 1;
                    int gBitMask = (1 << this.GDepth) - 1;
                    int rBitMask = (1 << this.RDepth) - 1;

                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        long value = 0;

                        if (BitDepth <= 8)
                            value = br.ReadByte();
                        else if (BitDepth <= 16)
                            value = br.ReadUInt16();
                        else if (BitDepth <= 24)
                        {
                            byte[] tmp = br.ReadBytes(3);
                            value = (this.ByteOrder == ByteOrder.LittleEndian) ? tmp[2] << 16 | tmp[1] << 8 | tmp[0] : tmp[0] << 16 | tmp[1] << 8 | tmp[0];
                        }
                        else if (BitDepth <= 32)
                            value = br.ReadUInt32();
                        else
                            throw new Exception($"BitDepth {BitDepth} not supported!");

                        yield return Color.FromArgb(
                            (this.ADepth == 0) ? 255 : Support.Support.ChangeBitDepth((int)(value >> aShift & aBitMask), this.ADepth, 8),
                            Support.Support.ChangeBitDepth((int)(value >> rShift & rBitMask), this.RDepth, 8),
                            Support.Support.ChangeBitDepth((int)(value >> gShift & gBitMask), this.GDepth, 8),
                            Support.Support.ChangeBitDepth((int)(value >> bShift & bBitMask), this.BDepth, 8));
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
                        int a = (this.ADepth == 0) ? 0 : Support.Support.ChangeBitDepth(color.A, 8, this.ADepth);
                        int r = Support.Support.ChangeBitDepth(color.R, 8, this.RDepth);
                        int g = Support.Support.ChangeBitDepth(color.G, 8, this.GDepth);
                        int b = Support.Support.ChangeBitDepth(color.B, 8, this.BDepth);

                        int rShift, bShift, gShift, aShift;

                        if (this.SwapChannels)
                        {
                            rShift = 0;
                            gShift = this.RDepth;
                            bShift = gShift + this.GDepth;
                            aShift = bShift + this.BDepth;
                        }
                        else
                        {
                            aShift = 0;
                            bShift = this.ADepth;
                            gShift = bShift + this.BDepth;
                            rShift = gShift + this.GDepth;
                        }

                        long value = 0;

                        value |= (uint)(a << aShift);
                        value |= (uint)(b << bShift);
                        value |= (uint)(g << gShift);
                        value |= (uint)(r << rShift);

                        if (BitDepth <= 8)
                            bw.Write((byte)value);
                        else if (BitDepth <= 16)
                            bw.Write((ushort)value);
                        else if (BitDepth <= 24)
                        {
                            byte[] tmp = (this.ByteOrder == ByteOrder.LittleEndian) ?
                                    new byte[] { (byte)(value & 0xff), (byte)(value >> 8 & 0xff), (byte)(value >> 16 & 0xff) } :
                                    new byte[] { (byte)(value >> 16 & 0xff), (byte)(value >> 8 & 0xff), (byte)(value & 0xff) };
                            bw.Write(tmp);
                        }
                        else if (BitDepth <= 32)
                            bw.Write((uint)value);
                        else
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}
