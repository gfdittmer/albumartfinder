using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArtGet
{
    public class Art
    {
        public String url;
        public Resolution r;
        public Art(String url, Resolution r)
        {
            this.url = url;
            this.r = r;
        }
    }
}
