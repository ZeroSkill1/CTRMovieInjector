using System.Collections.Generic;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class ETC1 : IImageFormat
    {
        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }
        public string FormatName { get; set; }
        private bool Alpha { get; set; }
        private bool ThreeDsOrder { get; set; }
        private ByteOrder ByteOrder { get; set; }

        public ETC1(bool alpha = false, bool threeDsOrder = true, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            this.BitDepth = alpha ? 8 : 4;
            this.BlockBitDepth = alpha ? 128 : 64;
            this.Alpha = alpha;
            this.ThreeDsOrder = threeDsOrder;
            this.FormatName = (alpha) ? "ETC1A4" : "ETC1";
            this.ByteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (MemoryStream ms = new MemoryStream(tex))
            {
                using (BinaryReaderX br = new BinaryReaderX(ms, this.ByteOrder))
                {
                    Support.ETC1.Decoder etc1decoder = new Support.ETC1.Decoder(this.ThreeDsOrder);

                    while (true)
                    {
                        yield return etc1decoder.Get(() =>
                        {
                            ulong etc1Alpha = this.Alpha ? br.ReadUInt64() : ulong.MaxValue;
                            ulong colorBlock = br.ReadUInt64();

                            Support.ETC1.Block etc1Block = new Support.ETC1.Block
                            {
                                LSB = (ushort)(colorBlock & 0xFFFF),
                                MSB = (ushort)((colorBlock >> 16) & 0xFFFF),
                                flags = (byte)((colorBlock >> 32) & 0xFF),
                                B = (byte)((colorBlock >> 40) & 0xFF),
                                G = (byte)((colorBlock >> 48) & 0xFF),
                                R = (byte)((colorBlock >> 56) & 0xFF)
                            };

                            return new Support.ETC1.PixelData { Alpha = etc1Alpha, Block = etc1Block };
                        });
                    }
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            Support.ETC1.Encoder etc1encoder = new Support.ETC1.Encoder(ThreeDsOrder);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriterX bw = new BinaryWriterX(ms, this.ByteOrder))
                {
                    foreach (Color color in colors)
                        etc1encoder.Set(color, data =>
                        {
                            if (this.Alpha) bw.Write(data.Alpha);

                            ulong colorBlock = 0;

                            colorBlock |= data.Block.LSB;
                            colorBlock |= ((ulong)data.Block.MSB << 16);
                            colorBlock |= ((ulong)data.Block.flags << 32);
                            colorBlock |= ((ulong)data.Block.B << 40);
                            colorBlock |= ((ulong)data.Block.G << 48);
                            colorBlock |= ((ulong)data.Block.R << 56);

                            bw.Write(colorBlock);
                        });

                    return ms.ToArray();
                }
            }
        }
    }
}
