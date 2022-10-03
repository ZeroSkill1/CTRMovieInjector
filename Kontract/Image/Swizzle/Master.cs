using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Kontract.Image.Swizzle
{
    public class MasterSwizzle
    {
        private IEnumerable<(int, int)> BitFieldCoords { get; set; }
        private IEnumerable<(int, int)> InitPointTransformOnY { get; set; }

        public int MacroTileWidth { get; }
        public int MacroTileHeight { get; }

        private int WidthInTiles { get; set; }
        private Point Init { get; set; }

        /// <summary>
        /// Creates an instance of MasterSwizzle
        /// </summary>
        /// <param name="imageStride">Pixelcount of dimension in which should get aligned</param>
        /// <param name="init">the initial point, where the swizzle begins</param>
        /// <param name="bitFieldCoords">Array of coordinates, assigned to every bit in the macroTile</param>
        /// <param name="initPointTransformOnY">Defines a transformation array of the initial point with changing Y</param>
        public MasterSwizzle(int imageStride, Point init, IEnumerable<(int, int)> bitFieldCoords, IEnumerable<(int, int)> initPointTransformOnY = null)
        {
            this.BitFieldCoords = bitFieldCoords;
            this.InitPointTransformOnY = initPointTransformOnY ?? Enumerable.Empty<(int, int)>();
            this.Init = init;
            this.MacroTileWidth = bitFieldCoords.Select(p => p.Item1).Aggregate((x, y) => x | y) + 1;
            this.MacroTileHeight = bitFieldCoords.Select(p => p.Item2).Aggregate((x, y) => x | y) + 1;
            this.WidthInTiles = (imageStride + MacroTileWidth - 1) / MacroTileWidth;
        }

        /// <summary>
        /// Transforms a given pointCount into a point
        /// </summary>
        /// <param name="pointCount">The overall pointCount to be transformed</param>
        /// <returns>The Point, which got calculated by given settings</returns>
        public Point Get(int pointCount)
        {
            int macroTileCount = pointCount / MacroTileWidth / MacroTileHeight;
            var (macroX, macroY) = (macroTileCount % this.WidthInTiles, macroTileCount / this.WidthInTiles);

            return new[] { (macroX * MacroTileWidth, macroY * MacroTileHeight) }
                .Concat(this.BitFieldCoords.Where((v, j) => (pointCount >> j) % 2 == 1))
                .Concat(this.InitPointTransformOnY.Where((v, j) => (macroY >> j) % 2 == 1))
                .Aggregate(this.Init, (a, b) => new Point(a.X ^ b.Item1, a.Y ^ b.Item2));
        }
    }
}
