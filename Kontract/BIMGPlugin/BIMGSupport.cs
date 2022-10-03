using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kontract.Interface;
using Kontract.Image.Format;

namespace image_nintendo.BIMG
{
    public class Support
    {
        public static Dictionary<int, IImageFormat> CTRFormat = new Dictionary<int, IImageFormat>
        {
            [0] = new RGBA(8, 8, 8, 8),
            [1] = new RGBA(8, 8, 8),
            [2] = new RGBA(5, 5, 5, 1),
            [3] = new RGBA(5, 6, 5),
            [4] = new RGBA(4, 4, 4, 4),
            [5] = new LA(8, 8),
            [6] = new HL(8, 8),
            [7] = new LA(8, 0),
            [8] = new LA(0, 8),
            [9] = new LA(4, 4),
            [10] = new LA(4, 0),
            [11] = new LA(0, 4),
            [12] = new ETC1(),
            [13] = new ETC1(true)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BimgHeader
    {
        public int zero1;
        public int dataSize;
        public int zero2;
        public int format;
        public short width;
        public short height;
        public int unk1;
        public int unk2;
        public uint unk3;
    }
}
