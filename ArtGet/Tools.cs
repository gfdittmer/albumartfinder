using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Net;

namespace ArtGet
{
    public class Tools
    {
        public static List<string> DirSearch(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                //MessageBox.Show("Something went wrong.\n" + excpt.Message);
            }

            return files;
        }

        public static bool isAudioFile(String path)
        {
            String[] ext = new String[] { ".mp3", ".m4a" };
            return ext.Contains(Path.GetExtension(path));
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static String getWildCardName(Song s)
        {
            String formattedArtist = s.artist.Length > 5 ? s.artist.Substring(0,5) : s.artist;
            String formattedAlbum = s.album.Length > 5 ? s.album.Substring(0,5) : s.album;
            String formattedName = (formattedArtist+formattedAlbum).Replace(" ",String.Empty);

            char[] notAccepted = "\\/:*?\"<>|".ToCharArray();
            var encodedName = Convert.ToBase64String(GetBytes(formattedName)).Where(c => !notAccepted.Contains(c)).ToArray();
            return "art" + new String(encodedName);
        }
    }
}
