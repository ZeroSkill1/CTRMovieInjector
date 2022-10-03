using System.Drawing;

namespace Kontract.Interface
{
    public interface IImageSwizzle
    {
        int Width { get; }
        int Height { get; }

        Point Get(Point point);
    }
}
