using System;

namespace Kontract.Image.Support
{
    public class Support
    {
        public static int ChangeBitDepth(int value, int bitDepthFrom, int bitDepthTo)
        {
            if (bitDepthTo < 0 || bitDepthFrom < 0)
                throw new Exception("BitDepths can't be negative!");
            if (bitDepthFrom == 0 || bitDepthTo == 0)
                return 0;
            if (bitDepthFrom == bitDepthTo)
                return value;

            if (bitDepthFrom < bitDepthTo)
            {
                int fromMaxRange = (1 << bitDepthFrom) - 1;
                int toMaxRange = (1 << bitDepthTo) - 1;

                int div = 1;
                while (toMaxRange % fromMaxRange != 0)
                {
                    div <<= 1;
                    toMaxRange = ((toMaxRange + 1) << 1) - 1;
                }

                return value * (toMaxRange / fromMaxRange) / div;
            }
            else
            {
                int fromMax = 1 << bitDepthFrom;
                int toMax = 1 << bitDepthTo;

                int limit = fromMax / toMax;

                return value / limit;
            }
        }
    }
}
