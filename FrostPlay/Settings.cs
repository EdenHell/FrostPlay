using System;
using System.IO;
using System.Runtime.Serialization;

namespace FrostPlay
{
    [DataContract]
    class Settings
    {
        [DataMember]
        public Uri lastMusicUri { get; set; }
        [DataMember]
        public PlayOrder playOrder { get; set; }
        [DataMember]
        public double volumeValue { get; set; }

        public Settings(Uri settingsFileUri)
        {
            this.readFromFile(settingsFileUri);
        }

        public Settings(Uri lastMusicUri, PlayOrder playOrder, double volumeValue)
        {
            this.lastMusicUri = lastMusicUri;
            this.playOrder = playOrder;
            this.volumeValue = volumeValue;
        }

        public void readFromFile(Uri path)
        {
            System.Runtime.Serialization.Json.DataContractJsonSerializer dcjs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(this.GetType());
            Settings settings = (Settings)dcjs.ReadObject(File.OpenRead(path.LocalPath));
            this.lastMusicUri = settings.lastMusicUri;
            this.playOrder = settings.playOrder;
            this.volumeValue = settings.volumeValue;
        }

        public void writeToFile(Uri path)
        {
            System.Runtime.Serialization.Json.DataContractJsonSerializer dcjs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(this.GetType());
            dcjs.WriteObject(File.Open(path.LocalPath, FileMode.Create), this);
        }
    }
}
