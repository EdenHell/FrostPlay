using CSCore.Codecs;
using CSCore.SoundOut;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FrostPlay
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    enum PlayOrder
    {
        random,
        order,
        loop,
        reverseOrder
    }

    public sealed partial class MainWindow
    {
        AudioEngine audioEngine = new AudioEngine();
        public BindingList<Music> playList { get; set; } = new BindingList<Music>();
        System.Windows.Media.ImageSource defaultCover { get; set; } = null;
        DispatcherTimer dispatcherTimer { get; set; } = new DispatcherTimer();
        PlayOrder playOrder { get; set; } = PlayOrder.order;
        Uri settingsFileUri { get; set; } = new Uri(new FileInfo("settings.json").FullName);
        Uri playListFileUri { get; set; } = new Uri(new FileInfo("playlist.playlist").FullName);
        Music nowPlayingMusic { get; set; } = null;
        string displayStringFormat { get; set; } = "%num%.%artist%-%title%";

        public MainWindow()
        {
            InitializeComponent();
            audioEngine.AudioOpened += AudioEngine_AudioOpened;
            audioEngine.AudioEnded += AudioEngine_AudioEnded;
            dispatcherTimer.Interval = new System.TimeSpan(10);
            dispatcherTimer.Tick += new EventHandler(Progress_timer_Tick);
            defaultCover = coverImage.Source;
            readPlayList();
            readSettings();
            DataContext = this;
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((string)playBtn.Content == "▶")
            {
                play();
            }
            else
            {
                pause();
            }
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            stop();
        }

        private void addItems_Click(object sender, RoutedEventArgs e)
        {
            int startIndex = playList.Count;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = CodecFactory.SupportedFilesFilterEn;
            if (ofd.ShowDialog() == true)
            {
                foreach (var item in ofd.FileNames)
                {
                    Uri musicUri = new Uri(item);
                    if (findMusic(musicUri) == null)
                    {
                        playList.Add(new Music(musicUri));
                    }
                }
                refreshPlayListMember(startIndex);
            }
        }

        private void removeItems_Click(object sender, RoutedEventArgs e)
        {
            if (playListBox.SelectedItems.Count == 0)
                return;
            Music[] tempList = new Music[playListBox.SelectedItems.Count];
            playListBox.SelectedItems.CopyTo(tempList, 0);
            int nextNum = -1;
            for (int i = 0; i < tempList.Length; i++)
            {
                Music item = tempList[i];
                if (nowPlayingMusic != null && nowPlayingMusic.path == item.path)
                    nextNum = nowPlayingMusic.number - i;
                playList.Remove(item);
            }
            if (playList.Count != 0)
            {
                if (nextNum >= 0)
                {
                    var flag = true;
                    if ((string)playBtn.Content == "▶")
                        flag = false;
                    Music nextMusic = (nextNum == playList.Count) ? playList[0] : playList[nextNum];
                    nowPlayingMusic = nextMusic;
                    audioEngine.Source = nextMusic.path;
                    if (flag)
                        play();
                }
                int startIndex = tempList[0].number;
                foreach (var item in tempList)
                    if (item.number < startIndex)
                        startIndex = item.number;
                refreshPlayListMember(startIndex);
            }
            else
            {
                nowPlayingMusic = null;
                audioEngine.Source = null;
                playBtn.Content = "▶";
                sliderPosition.Visibility = Visibility.Hidden;
                timeLabel.Visibility = Visibility.Hidden;
                artistAndTitleLabel.Visibility = Visibility.Hidden;
                albumLabel.Visibility = Visibility.Hidden;
                coverImage.Source = defaultCover;
            }
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playOrder != PlayOrder.loop)
            {
                playNextSong(playOrder);
            }
            else
            {
                playNextSong(PlayOrder.order);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playOrder != PlayOrder.random)
            {
                playNextSong(PlayOrder.reverseOrder);
            }
            else
            {
                playNextSong(PlayOrder.random);
            }
        }

        private void order_btn(object sender, RoutedEventArgs e)
        {
            playOrder = PlayOrder.order;
            orderStatusLabel.Content = enumToString();
        }

        private void loop_btn(object sender, RoutedEventArgs e)
        {
            playOrder = PlayOrder.loop;
            orderStatusLabel.Content = enumToString();
        }

        private void random_btn(object sender, RoutedEventArgs e)
        {
            playOrder = PlayOrder.random;
            orderStatusLabel.Content = enumToString();
        }

        private void MenuItem_Click_SortByTitle(object sender, RoutedEventArgs e)
        {
            List<Music> sortList = new List<Music>();
            foreach (var item in playList)
                sortList.Add(item);
            sortList.Sort(new TitleComparer());
            for (int i = 0; i < playList.Count; i++)
            {
                playList[i] = sortList[i];
                refreshMusicMember(playList[i], i);
            }
        }

        private void MenuItem_Click_SortByArtist(object sender, RoutedEventArgs e)
        {
            List<Music> sortList = new List<Music>();
            foreach (var item in playList)
                sortList.Add(item);
            sortList.Sort(new ArtistComparer());
            for (int i = 0; i < playList.Count; i++)
            {
                playList[i] = sortList[i];
                refreshMusicMember(playList[i], i);
            }
        }

        private void MenuItem_Click_SortByAlbum(object sender, RoutedEventArgs e)
        {
            List<Music> sortList = new List<Music>();
            foreach (var item in playList)
                sortList.Add(item);
            sortList.Sort(new AlbumComparer());
            for (int i = 0; i < playList.Count; i++)
            {
                playList[i] = sortList[i];
                refreshMusicMember(playList[i], i);
            }
        }

        private void sliderPosition_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            dispatcherTimer.IsEnabled = false;
        }

        private void sliderPosition_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (nowPlayingMusic == null) return;
            audioEngine.Position = TimeSpan.FromSeconds(sliderPosition.Value / sliderPosition.Maximum * nowPlayingMusic.duration.TotalSeconds);
            dispatcherTimer.IsEnabled = true;
        }

        private void sliderPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var playedSeconds = sliderPosition.Value / sliderPosition.Maximum * nowPlayingMusic.duration.TotalSeconds;
            var currentMinutes = Math.Truncate(playedSeconds / 60);
            var currentSeconds = Math.Truncate(playedSeconds - currentMinutes * 60);
            var totalMinutes = Math.Truncate(nowPlayingMusic.duration.TotalMinutes);
            var totalSeconds = Math.Truncate(nowPlayingMusic.duration.TotalSeconds - totalMinutes * 60);
            timeLabel.Content = currentMinutes.ToString() + ":" + currentSeconds.ToString() + "/" + totalMinutes.ToString() + ":" + totalSeconds.ToString();
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            audioEngine.Volume = (float)volumeSlider.Value;
        }

        private void playListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string originalSourceTypeString = e.OriginalSource.GetType().ToString();
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right || originalSourceTypeString != "System.Windows.Controls.Grid" && originalSourceTypeString != "System.Windows.Controls.TextBlock")
                return;
            var selectedItem = (Music)playListBox.SelectedItem;
            nowPlayingMusic = selectedItem;
            audioEngine.Source = selectedItem.path;
            play();
        }

        private void AudioEngine_AudioOpened(object sender, EventArgs e)
        {
            dispatcherTimer.IsEnabled = true;
            audioEngine.Volume = (float)volumeSlider.Value;
            playListBox.ScrollIntoView(nowPlayingMusic);
            playListBox.SelectedItem = nowPlayingMusic;
            artistAndTitleLabel.Content = nowPlayingMusic.artist + "-" + nowPlayingMusic.title;
            albumLabel.Content = nowPlayingMusic.album;
            var currentMusicPicture = nowPlayingMusic.getPicture();
            if (currentMusicPicture != null)
            {
                coverImage.Source = currentMusicPicture;
            }
            else
            {
                coverImage.Source = defaultCover;
            }
            if (nowPlayingMusic.artist == null || nowPlayingMusic.title == null)
                artistAndTitleLabel.Content = "N/A";
            if (nowPlayingMusic.album == null)
                albumLabel.Content = "N/A";
            if (nowPlayingMusic.duration.TotalSeconds == 0)
            {
                timeLabel.Content = "N/A";
                sliderPosition.Visibility = Visibility.Hidden;
            }
            else
                sliderPosition.Visibility = Visibility.Visible;
            artistAndTitleLabel.Visibility = Visibility.Visible;
            albumLabel.Visibility = Visibility.Visible;
            timeLabel.Visibility = Visibility.Visible;
        }

        private void AudioEngine_AudioEnded(object sender, PlaybackStoppedEventArgs e)
        {
            if (nowPlayingMusic != null)
                playNextSong(playOrder);
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            writePlayList();
            writeSettings();
            nowPlayingMusic = null;
            audioEngine.Source = null;
            audioEngine.Dispose();
        }

        private void Progress_timer_Tick(object sender, EventArgs e)
        {
            if (nowPlayingMusic == null || nowPlayingMusic.duration.TotalSeconds == 0) return;
            sliderPosition.Value = audioEngine.Position.TotalSeconds / nowPlayingMusic.duration.TotalSeconds * sliderPosition.Maximum;
        }

        private void play()
        {
            if (nowPlayingMusic != null)
            {
                audioEngine.Play();
                playBtn.Content = "| |";
            }
        }

        private void pause()
        {
            if ((string)playBtn.Content == "| |")
            {
                audioEngine.Pause();
                playBtn.Content = "▶";
            }
        }

        private void stop()
        {
            audioEngine.Stop();
            playBtn.Content = "▶";
        }

        private void playNextSong(PlayOrder order)
        {
            if (nowPlayingMusic==null || playList.Count <= 0)
                return;
            Music nextSong = null;
            switch (order)
            {
                case PlayOrder.random:
                    if (playList.Count == 1)
                    {
                        nextSong = nowPlayingMusic;
                        break;
                    }
                    Random random = new Random();
                    while (true)
                    {
                        var randomIndex = random.Next(playList.Count);
                        if (randomIndex != nowPlayingMusic.number)
                        {
                            nextSong = playList[randomIndex];
                            break;
                        }
                    }
                    break;
                case PlayOrder.order:
                    if (nowPlayingMusic.number + 1 < playList.Count)
                        nextSong = playList[nowPlayingMusic.number + 1];
                    else
                        nextSong = playList[0];
                    break;
                case PlayOrder.loop:
                    nextSong = nowPlayingMusic;
                    break;
                case PlayOrder.reverseOrder:
                    if (nowPlayingMusic.number - 1 >= 0)
                        nextSong = playList[nowPlayingMusic.number - 1];
                    else
                        nextSong = playList[playList.Count - 1];
                    break;
                default:
                    break;
            }
            stop();
            nowPlayingMusic = nextSong;
            audioEngine.Source = nextSong.path;
            play();
        }

        private Music findMusic(Uri fileUri)
        {
            foreach (var item in playList)
            {
                if (item.path == fileUri)
                {
                    return item;
                }
            }
            return null;
        }

        private void readPlayList()
        {
            FileInfo fileInfo = new FileInfo(playListFileUri.LocalPath);
            if (fileInfo.Exists && fileInfo.Length != 0)
            {
                System.Runtime.Serialization.Json.DataContractJsonSerializer dcjs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(playList.GetType());
                playList = (BindingList<Music>)dcjs.ReadObject(File.OpenRead(playListFileUri.LocalPath));
            }
        }
        private void writePlayList()
        {
            System.Runtime.Serialization.Json.DataContractJsonSerializer dcjs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(playList.GetType());
            dcjs.WriteObject(File.Open(playListFileUri.LocalPath, FileMode.Create), playList);
        }

        private void readSettings()
        {
            FileInfo fileInfo = new FileInfo(settingsFileUri.LocalPath);
            if (!fileInfo.Exists || fileInfo.Length == 0)
                return;
            Settings settings = new Settings(settingsFileUri);
            playOrder = settings.playOrder;
            orderStatusLabel.Content = enumToString();
            volumeSlider.Value = settings.volumeValue;
            Music music = findMusic(settings.lastMusicUri);
            if (music != null)
            {
                nowPlayingMusic = music;
                audioEngine.Source = music.path;
                playListBox.ScrollIntoView(music);
                playListBox.SelectedItem = music;
            }
        }

        private void writeSettings()
        {
            Uri lastMusicUri = null;
            if (nowPlayingMusic != null)
                lastMusicUri = nowPlayingMusic.path;
            Settings settings = new Settings(lastMusicUri, playOrder, volumeSlider.Value);
            settings.writeToFile(settingsFileUri);
        }

        private void refreshMusicMember(Music item,int i)
        {
            item.number = i;
            item.displayString = displayStringFormat.Replace("%num%", (item.number + 1).ToString()).Replace("%artist%", item.artist).Replace("%title%", item.title).Replace("%album%", item.album);
            playList.ResetItem(i);
        }

        private void refreshPlayListMember(int startIndex)
        {
            for (int i = startIndex; i < playList.Count; i++)
                refreshMusicMember(playList[i],i);
        }

        private string enumToString()
        {
            switch (playOrder)
            {
                case PlayOrder.random:
                    return "Random";
                case PlayOrder.order:
                    return "Order";
                case PlayOrder.loop:
                    return "Loop";
                case PlayOrder.reverseOrder:
                    return "ReverseOrder";
                default:
                    return "";
            }
        }
    }

    public class TitleComparer : IComparer<Music>
    {
        public int Compare(Music x, Music y)
        {
            if (x.title == null)
                return 1;
            int retval = x.title.CompareTo(y.title);
            if (x.artist == null)
                return 1;
            if (retval == 0)
                retval = x.artist.CompareTo(y.artist);
            if (x.album == null)
                return 1;
            if (retval == 0)
                retval = x.album.CompareTo(y.album);
            return retval;
        }
    }

    public class ArtistComparer : IComparer<Music>
    {
        public int Compare(Music x, Music y)
        {
            if (x.artist == null)
                return 1;
            int retval = x.artist.CompareTo(y.artist);
            if (x.album == null)
                return 1;
            if (retval == 0)
                retval = x.album.CompareTo(y.album);
            if (x.title == null)
                return 1;
            if (retval == 0)
                retval = x.title.CompareTo(y.title);
            return retval;
        }
    }

    public class AlbumComparer : IComparer<Music>
    {
        public int Compare(Music x, Music y)
        {
            if (x.album == null)
                return 1;
            int retval = x.album.CompareTo(y.album);
            if (x.title == null)
                return 1;
            if (retval == 0)
                retval = x.title.CompareTo(y.title);
            return retval;
        }
    }
}