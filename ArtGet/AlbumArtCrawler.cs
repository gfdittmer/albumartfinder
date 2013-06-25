using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Text.RegularExpressions;
using MessageBox = System.Windows.MessageBox;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Reflection;

namespace ArtGet
{
    public class AlbumArtCrawler
    {
        MainWindow w;

        public AlbumArtCrawler(MainWindow w)
        {
            this.w = w;
        }

        public List<Art> getArtUrl(Song song,int workingIndex)
        {
            w.setAlbumProgress(workingIndex, 10);

            String formattedTerms = Regex.Replace(song.artist.Replace(' ', '+') + "+" + song.album.Replace(' ', '+'), @"\++", "+").ToLower();

            String searchUrl = "https://www.google.com/search?q="
                               + formattedTerms + "&um=1&hl=en&tbm=isch&sout=1&biw=1366&bih=643";

            String rawSource = new WebClient().DownloadString(searchUrl);

            w.setAlbumProgress(workingIndex, 40);

            int start = rawSource.IndexOf("<div id=\"search\">");

            rawSource = rawSource.Substring(start);

            List<Art> possibilities = new List<Art>();
            Resolution r;
            String url;

            do
            {
                int startOuterURL = rawSource.IndexOf("<a href=");

                int startURL = rawSource.IndexOf("imgurl=", startOuterURL) + 7;
                int endURL = rawSource.IndexOf("&amp;", startURL);

                url = rawSource.Substring(startURL, endURL - startURL);

                r = parseNextResolution(rawSource.Substring(endURL));
                if (r.isValidAspectRatio() && r.h + r.w > 590 && r.h + r.w < 2500)
                {
                    possibilities.Add(new Art(url, r));
                }
                rawSource = rawSource.Substring(endURL + 200);
            } while (possibilities.Count < 5 && rawSource.Contains("imgurl"));

            if (possibilities.Count == 0)
            {
                throw new NoArtworkFoundException("No artwork could be found on Google Images.");
            }
            w.setAlbumProgress(workingIndex, 50);
            return possibilities; 
        }

        public String thoroughCrawl(String artist,String album)
        {
            String formattedTerms = Regex.Replace(artist.Replace(' ', '+') + "+" + album.Replace(' ', '+'), @"\++", "+").ToLower();

            String searchUrl = "https://www.google.com/search?q="
                               + formattedTerms + "&um=1&hl=en&tbm=isch&sout=1&biw=1366&bih=643";

            String rawSource = new WebClient().DownloadString(searchUrl);

            int start = rawSource.IndexOf("<div id=\"search\">");

            rawSource = rawSource.Substring(start);

            Resolution r;
            String url;

            List<Art> possibilities = new List<Art>();

            do
            {
                int startOuterURL = rawSource.IndexOf("<a href=");

                int startURL = rawSource.IndexOf("imgurl=", startOuterURL) + 7;
                int endURL = rawSource.IndexOf("&amp;", startURL);

                url = rawSource.Substring(startURL, endURL - startURL);

                r = parseNextResolution(rawSource.Substring(endURL));
                if (r.isValidAspectRatio() && r.h + r.w > 590 && r.h + r.w < 2500)
                {
                    possibilities.Add(new Art(url, r));
                }

                rawSource = rawSource.Substring(endURL + 200);
            } while (possibilities.Count < 5 && rawSource.Contains("imgurl"));

            List<List<Color>> data = new List<List<Color>>();

            for(int i = 0;i< possibilities.Count;i++)
            {
                try
                {
                    new WebClient().DownloadFile(possibilities[i].url, "art" + i + ".jpg");
                    Bitmap b = new Bitmap("art" + i + ".jpg");
                    data.Add(splitTest(b));
                }
                catch
                {
                    MessageBox.Show("Couldn't download...");
                }
            }

            int selectedArt = 0;

            int maxReso = 0;

            foreach (int i in messing(data))
            {
                int currentReso = possibilities[i].r.w * possibilities[i].r.h;
                if (currentReso > maxReso)
                {
                    selectedArt = i;
                    maxReso = currentReso;
                }
            }
            MessageBox.Show("The selected art is "+selectedArt.ToString());
            return url; 
        }

        List<int> messing(List<List<Color>> input)
        {
            int whichIndex = 0;
            List<int> indexSimilar = new List<int>();

            for (int i = 0; i < input.Count; i++)
            {
                List<int> currentSimilar = new List<int> { i };

                for (int j = 0; j < input.Count; j++)
                {
                    if(j == i) continue;
                    List<Color> a = input[i];
                    List<Color> b = input[j];

                    List<int> differenceConstants = new List<int>();

                    //For each subsection
                    for(int s = 0;s<9;s++)
                    {
                        differenceConstants.Add(Math.Abs(a[s].R-b[s].R)+Math.Abs(a[s].G-b[s].G)+Math.Abs(a[s].B-b[s].B));
                    }

                    if (differenceConstants.Average() < 30 && differenceConstants.Max() < 150)
                    {
                        currentSimilar.Add(j);
                    }
                }

                if (currentSimilar.Count > indexSimilar.Count)
                {
                    indexSimilar = currentSimilar.ToList();
                    whichIndex = i;
                }
            }

            MessageBox.Show(String.Join(",", indexSimilar));
            return indexSimilar;
        }

        List<Color> splitTest(Bitmap bitmap)
        {
            List<Color> colors = new List<Color>();

            Resolution rs = new Resolution(bitmap.Width, bitmap.Height);
            for (int ySquare = 0; ySquare < 3; ySquare++)
            {
                for (int xSquare = 0; xSquare < 3; xSquare++)
                {
                    int x1 = 1 + xSquare * rs.w / 3;
                    int x2 = (x1 - 1) + rs.w / 3;

                    int y1 = 1 + ySquare * rs.h / 3;
                    int y2 = (y1 - 1) + rs.h / 3;

                    int rTotal = 0;
                    int gTotal = 0;
                    int bTotal = 0;

                    int pixelCount = 0;


                    //MessageBox.Show("(" + x1 + "," + y1 + ") to (" + x2 + "," + y2 + ")");
                    for (int x = x1; x < x2; x += 10)
                    {
                        for (int y = y1; y < y2; y += 10)
                        {
                            Color color = bitmap.GetPixel(x, y);

                            rTotal += color.R;
                            gTotal += color.G;
                            bTotal += color.B;

                            pixelCount++;
                        }
                    }

                    int r = rTotal / pixelCount;
                    int g = gTotal / pixelCount;
                    int b = bTotal / pixelCount;

                    //MessageBox.Show(pixelCount+" pixels => (" + r + "," + g + "," + b + ")");
                    colors.Add(Color.FromArgb(r,g,b));   
                }
            }

            return colors;
            
        }

        Resolution parseNextResolution(String source)
        {
            //int startText = source.IndexOf("</b><br>") + 8;
            int startText = Regex.Match(source, "<br>.{0,10}&times;").Index + 4;
            int endText = source.IndexOf("-", startText) - 1;

            String textReso = source.Substring(startText, endText - startText);

            int split = textReso.IndexOf("&times;");

            int width = Int32.Parse(textReso.Substring(0, split - 1));

            int height = Int32.Parse(textReso.Substring(split + 8));

            return new Resolution(width, height);
        }

        /// <summary>
        /// Adds album art to a list of songs
        /// </summary>
        /// <param name="url">URL of album art</param>
        /// <param name="songs">List of songs located on the album that will have the artwork added</param>
        public void addArt(List<Art> urls,int index,List<Song> songs,bool isDownloaded,int workingIndex)
        {
            String name = Tools.getWildCardName(songs[0]);
            try
            {
                if (!isDownloaded)
                {
                    new WebClient().DownloadFile(urls[index].url, name + ".jpg");
                }
                w.setAlbumProgress(workingIndex, 70);
                //Use working index

                int progressTicker = 0;

                foreach (Song s in songs)
                {
                    
                    TagLib.File f = TagLib.File.Create(s.path);
                    TagLib.IPicture newArt = new TagLib.Picture(name + ".jpg");
                    f.Tag.Pictures = new TagLib.IPicture[1] { newArt };
                    f.Save();
                    progressTicker++;
                    double progress = (double)progressTicker / songs.Count * 30;
                    w.setAlbumProgress(workingIndex, (int)progress);
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    if (ex.InnerException is IOException)
                    {
                        addArt(urls,index, songs, true,workingIndex);

                    }
                    else
                    {
                        if ((ex as WebException).Status == WebExceptionStatus.ProtocolError)
                        {

                            if (index == urls.Count - 1) throw;
                            addArt(urls, index + 1, songs, false,workingIndex);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    //Adding some funcitonality later
                }
                else
                {
                    throw;
                    //MessageBox.Show("Something went wrong.\n" + ex.Message);
                }
                
            }
        }

        static public void removeArtFiles()
        {
            String location = AppDomain.CurrentDomain.BaseDirectory;
            var files = Directory.GetFiles(location).Select(file => Path.GetFileName(file));
            var toBeDeleted = files.Where(file => Regex.IsMatch(file, "^(art).*(\\.jpg|\\.jpeg)$"));
            foreach (String file in toBeDeleted)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }
}
