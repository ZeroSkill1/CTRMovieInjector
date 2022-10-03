using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Interface
{
    public interface IImageFormat
    {
        int BitDepth { get; }

        string FormatName { get; }

        IEnumerable<Color> Load(byte[] input);
        byte[] Save(IEnumerable<Color> colors);
    }
}
