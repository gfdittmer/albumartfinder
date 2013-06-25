using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArtGet
{
    class NoArtworkFoundException : Exception
    {
        public NoArtworkFoundException(string message) : base(message)
        {
            
        }
    }
}
