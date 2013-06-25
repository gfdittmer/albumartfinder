using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtGet
{
    public class Song
    {
        public bool hasArt;
        public String artist;
        public String album;
        public String path;

        public Song(String location)
        {
                TagLib.File tags = TagLib.File.Create(location);
                hasArt = tags.Tag.Pictures.Length > 0;
                artist = tags.Tag.Performers.Length > 0 ? tags.Tag.Performers[0] : String.Empty;
                album = tags.Tag.Album;
                path = location;
        }

        public static bool doesHaveArt(String path)
        {
            TagLib.File tags = TagLib.File.Create(path);
            return tags.Tag.Pictures.Length > 0;
        }
    }
}