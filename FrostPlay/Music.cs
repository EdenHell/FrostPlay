using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace FrostPlay
{
    //注释的内容是可以替代调用 playList.ResetItem(i) 通知列表中元素属性更新的方法
    [DataContract]
    public class Music //: INotifyPropertyChanged
    {
        [DataMember]
        public int number { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string artist { get; set; }
        [DataMember]
        public string album { get; set; }
        [DataMember]
        public TimeSpan duration { get; set; }
        [DataMember]
        public Uri path { get; set; }
        [DataMember]
        public string displayString { get; set; }

        //private string _displayString;
        //[DataMember]
        //public string displayString
        //{
        //    get { return _displayString; }
        //    set
        //    {
        //        _displayString = value;
        //        OnPropertyChanged(new PropertyChangedEventArgs("displayString"));
        //    }
        //}

        public Music()
        {
            path = null;
        }

        public Music(Uri path)
        {
            try
            {
                var file = TagLib.File.Create(path.LocalPath);
                title = file.Tag.Title;
                if (file.Tag.Performers.Length != 0)
                    artist = file.Tag.Performers[0];
                else
                    artist = null;
                album = file.Tag.Album;
                duration = file.Properties.Duration;
                this.path = path;
            }
            catch
            {
                title = null;
                artist = null;
                album = null;
                duration = TimeSpan.Zero;
                this.path = path;
            }
        }

        public BitmapImage getPicture()
        {
            TagLib.File file;
            try
            {
                file = TagLib.File.Create(path.LocalPath);
            }
            catch
            {
                return null;
            }
            if (file.Tag.Pictures.Length > 0)
            {
                var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(bin);
                bi.EndInit();
                return bi;
            }
            else
                return null;
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //public void OnPropertyChanged(PropertyChangedEventArgs e)
        //{
        //    if (PropertyChanged != null)
        //        PropertyChanged(this, e);
        //}
    }
}
