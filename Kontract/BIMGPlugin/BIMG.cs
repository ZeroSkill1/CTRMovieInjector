using Kontract.Image.Swizzle;
using System.Drawing;
using Kontract.Image;
using Kontract.IO;
using System.IO;
using System;

namespace image_nintendo.BIMG
{
    public sealed class BIMG
    {
        public Bitmap ImageBitmap;
        public ImageSettings Settings;
        private BimgHeader Header;

        public BIMG(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                this.Header = br.ReadStruct<BimgHeader>();
                var imgData = br.ReadBytes(Header.dataSize);
                this.Settings = new ImageSettings
                {
                    Width = Header.width,
                    Height = Header.height,
                    Format = Support.CTRFormat[Header.format],
                    Swizzle = new CTRSwizzle(Header.width, Header.height)
                };
                this.ImageBitmap = Common.Load(imgData, Settings);
            }
        }

        public void Save(string filename)
        {
            if (this.ImageBitmap.Width != this.Header.width)
                throw new Exception($"Image must be {this.Header.width}px in width");
            
            if (this.ImageBitmap.Height != this.Header.height)
                throw new Exception($"Image must be {this.Header.width}px in width");

            using (BinaryWriterX bw = new BinaryWriterX(File.OpenWrite(filename)))
            {
                bw.WriteStruct<BimgHeader>(this.Header);
                bw.Write(Common.Save(this.ImageBitmap, this.Settings));
            }
        }
    }
}
