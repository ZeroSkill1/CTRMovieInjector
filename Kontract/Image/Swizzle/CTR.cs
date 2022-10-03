using System;
using Kontract.Interface;
using System.Drawing;

namespace Kontract.Image.Swizzle
{
    public class CTRSwizzle : IImageSwizzle
    {
        private byte Orientation { get; set; }
        private MasterSwizzle ZOrder { get; set; }

        public int Width { get; }
        public int Height { get; }

        public CTRSwizzle(int width, int height, byte orientation = 0, bool toPowerOf2 = true)
        {
            this.Width = (toPowerOf2) ? 2 << (int)Math.Log(width - 1, 2) : width;
            this.Height = (toPowerOf2) ? 2 << (int)Math.Log(height - 1, 2) : height;
            this.Orientation = orientation;
            this.ZOrder = new MasterSwizzle(orientation == 0 || orientation == 2 ? Width : Height, new Point(0, 0), new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) });
        }

        public Point Get(Point point)
        {
            int pointCount = point.Y * Width + point.X;
            Point newPoint = this.ZOrder.Get(pointCount);

            switch (this.Orientation)
            {
                //Transpose
                case 8: return new Point(newPoint.Y, newPoint.X);
                //Rotate90 (anti-clockwise)
                case 4: return new Point(newPoint.Y, Height - 1 - newPoint.X);
                //Y Flip (named by Neo :P)
                case 2: return new Point(newPoint.X, Height - 1 - newPoint.Y);
                default: return newPoint;
            }
        }
    }
}
