using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArtGet
{
    public class Resolution
    {
        public int w;
        public int h;
        public Resolution(int width, int height)
        {
            w = width;
            h = height;
        }

        public bool isValidAspectRatio()
        {
            return (double)Math.Abs(w - h) / Math.Max(w, h) < 0.05;
        }
    }
}
