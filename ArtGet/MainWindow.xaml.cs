using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Text.RegularExpressions;

namespace ArtGet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            bBrowse.WorkerSupportsCancellation = true;
            bBrowse.DoWork += bBrowse_DoWork;
            bBrowse.RunWorkerCompleted += bBrowse_RunWorkerCompleted;

            bCrawl.WorkerSupportsCancellation = true;
            bCrawl.DoWork += bCrawl_DoWork;
        }

        String libraryDirectory;
        
        //Stores grouped data about albums that are missing artwork.
        List<IGrouping<String, Song>> needsArt;

        //Stores grouped data about albums for which artwork could not be found. I might start storing the Exception type in here as well.
        List<IGrouping<String, Song>> failureReport = new List<IGrouping<String, Song>>();

        readonly int numThreads = 4;

        //Needed to determine if the browse folder operation finished so the go-ahead can be given to the crawler.
        bool isBrowseValid;

        BackgroundWorker bBrowse = new BackgroundWorker();
        BackgroundWorker bCrawl = new BackgroundWorker();

        //Isn't being used beyond background data for the time being.
        int corruptedSongs = 0;

        //Used for success rate - number of albums successfully fixed, and number failed.
        int y = 0, n = 0;

        //Instance of AlbumArtCrawler - needed because I pass in 'this' for proper progress reporting. 
        AlbumArtCrawler a;

        //I might be able to ditch this whole method soon. Need to figure out Microsoft's included .NET testing.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.Version.Major < 4)
            {
                VersionBox v = new VersionBox()
                {
                    netVersion = Environment.Version
                };
                v.ShowDialog();
                while (v.IsActive);
                this.Close();
            }
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            //Browse for folder
            using (System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    libraryDirectory = f.SelectedPath;
                }
                else
                {
                    return;
                }
            }

            //Clear listview so items dont build up with multiple runs
            consoleListView.Items.Clear();

            //Button configs
            buttonBrowse.IsEnabled = false;
            buttonCancel.IsEnabled = true;
            
            bBrowse.RunWorkerAsync();
        }

        void bBrowse_DoWork(object sender, DoWorkEventArgs e)
        {
            //Will only be false if cancelled
            isBrowseValid = true;

            setProgressText("Analyzing Library...");

            //Non-formatted, just paths of all files
            var files = Tools.DirSearch(libraryDirectory);

            reportProgress(100);

            var music = files.Where(file => Tools.isAudioFile(file)).ToList();

            reportProgress(200);

            List<Song> needsArtUngrouped = new List<Song>();

            setProgressText("Gathering music data...");

            int progressCounter = 0;

            foreach (var song in music)
            {
                if (bBrowse.CancellationPending)
                {
                    break;
                }

                progressCounter++;

                try
                {
                    Song s = new Song(song);

                    if (!s.hasArt)
                    {
                        needsArtUngrouped.Add(s);
                    }
                }
                catch
                {
                    corruptedSongs++;
                }

                double progress = 200 + 600 * (double)progressCounter / music.Count;
                reportProgress(progress);
            }

            if (bBrowse.CancellationPending)
            {
                isBrowseValid = false;
                return;
            }



            if(corruptedSongs > 0)
            {
                //Might add some reporting later on.
            }

            int oldCount = needsArtUngrouped.Count;

            needsArtUngrouped = needsArtUngrouped.Where(s => s.artist != String.Empty && s.album != String.Empty && s.album != null).ToList();

            int blankTagSongs = oldCount - needsArtUngrouped.Count;

            if (blankTagSongs > 0)
            {
                ///Might add some reporting later on as to songs that are missing needed ID3 tags.
            }

            needsArt = needsArtUngrouped
                         .GroupBy(song => song.artist + "-" + song.album)
                         .ToList();

            reportProgress(1000);
        }

        void bBrowse_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            buttonBrowse.IsEnabled = true;

            if (isBrowseValid)
            {
                if (needsArt.Count > 0)
                {
                    buttonGetArt.IsEnabled = true;
                    setProgressText("Ready to crawl for album art.");
                    foreach (var i in needsArt)
                    {
                        addItem(i.Key);
                    }
                }
                else
                {
                    buttonGetArt.IsEnabled = false;
                    setProgressText("No albums in this folder are missing artwork.");
                }
            }
            else
            {
                buttonGetArt.IsEnabled = false;
                setProgressText("Operation was cancelled.");
                reportProgress(0);
            }
            
        }

        private void buttonGetArt_Click(object sender, RoutedEventArgs e)
        {
            buttonGetArt.IsEnabled = false;
            buttonBrowse.IsEnabled = false;
            buttonCancel.IsEnabled = true;
            bCrawl.RunWorkerAsync();
        }

        void bCrawl_DoWork(object sender, DoWorkEventArgs e)
        {
            reportProgress(0);

            if (!Tools.CheckForInternetConnection())
            {
                MessageBox.Show("You need a working internet connection to run ArtGet.");
                return;
            }

            a = new AlbumArtCrawler(this);

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < numThreads && i < needsArt.Count; i++)
            {
                threads.Add(new Thread(new ThreadStart(threadGate)));
                threads[threads.Count - 1].Start();
                Thread.Sleep(10);
            }
            while (threads.Any(t => t.IsAlive));
            
            reportProgress(900);
            setProgressText("Finishing up...");

            if (failureReport.Count > 0)
            {
                MessageBox.Show("Artwork could not be downloaded for " + failureReport.Count + " albums.");
            }

            if (!bCrawl.CancellationPending)
            {
                double successRate = (double)y / (y + n);
                MessageBox.Show("Successfully grabbed art for " + y + " out of " + needsArt.Count + " albums.\n Success Rate: " + successRate.ToString("P2"));
            }
            else
            {
                MessageBox.Show("Operation cancelled. Finished grabbing art for " + y + " out of " + needsArt.Count + " albums.");
            }
            AlbumArtCrawler.removeArtFiles();

            reportProgress(1000);
            setProgressText(String.Empty);
            this.Dispatcher.Invoke((Action)(() =>
            {
                buttonCancel.IsEnabled = false;
                buttonBrowse.IsEnabled = true;
                if (bCrawl.CancellationPending)
                {
                    buttonGetArt.IsEnabled = true;
                }
                else
                {
                    buttonGetArt.IsEnabled = false;
                }
            }));
        }

        void reportProgress(double value)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                progressBar.Value = value;
            }));
        }

        void setProgressText(String text)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                progressLabel.Content = text;
            }));
        }

        public void setAlbumProgress(int index,int progress)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                Grid item = consoleListView.Items[index] as Grid;
                ProgressBar p = item.Children[1] as ProgressBar;
                p.Value = progress;
            }));
        }

        void setAlbumTooltip(int index, String tooltip)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                Grid item = consoleListView.Items[index] as Grid;
                item.ToolTip = tooltip;
            }));
        }

        void updateGUIAlbumFailed(int index)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                Grid item = consoleListView.Items[index] as Grid;
                ProgressBar p = item.Children[1] as ProgressBar;
                p.Value = 100;
                p.Foreground = Brushes.Red;
            }));
        }

        int threadCounter = 0;

        int albumsFinished = 1;

        void threadGate()
        {
            while (threadCounter < needsArt.Count)
            {
                var albumData = needsArt[threadCounter];
                threadCounter++;
                int workingIndex = needsArt.IndexOf(albumData);
                Song s = albumData.ElementAt(0);

                try
                {
                    List<Art> urls = a.getArtUrl(s,workingIndex);

                    a.addArt(urls,0, albumData.ToList(),false,workingIndex);
                    setAlbumProgress(workingIndex, 100);

                    y++;
                }
                catch(Exception ex)
                {
                    failureReport.Add(albumData);
                    n++;
                    updateGUIAlbumFailed(workingIndex);
                    if (ex is NoArtworkFoundException)
                    {
                        setAlbumTooltip(workingIndex, "No artwork was found.");
                    }
                    else
                    {
                        setAlbumTooltip(workingIndex, "Something went wrong.");
                    }
                }
                
                reportProgress(1000 * (double)albumsFinished / needsArt.Count);

                setProgressText("Working (" + albumsFinished + "/" + needsArt.Count + ")");
                albumsFinished++;
                if (bCrawl.CancellationPending) break;
            }
        }

        void addItem(String albumName)
        {
            /*
             *<Grid HorizontalAlignment="Stretch" Height="28">
                <Label HorizontalAlignment="Left" Content="Here is an album name woop" />
                <ProgressBar HorizontalAlignment="Right" Width="100" Height="20" Margin="0,0,5,0"/>
            </Grid>
             */
            this.Dispatcher.Invoke((Action)(() =>
            {

                Grid g = new Grid()
                {
                    HorizontalAlignment= System.Windows.HorizontalAlignment.Stretch,
                    Height = 28
                };
                g.Children.Add(new Label()
                {
                    Content = albumName,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                });
                g.Children.Add(new ProgressBar()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Width = 100,
                    Height = 20,
                    Margin = new Thickness(0,0,5,0)
                });

                consoleListView.Items.Add(g);
            }));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            buttonCancel.IsEnabled = false;

            if (bBrowse.IsBusy && !bBrowse.CancellationPending)
            {
                bBrowse.CancelAsync();
                setProgressText("Cancelling...");
            }
            else if (bCrawl.IsBusy && !bCrawl.CancellationPending)
            {
                bCrawl.CancelAsync();
                setProgressText("Cancelling...");
            }
        }
    }
}