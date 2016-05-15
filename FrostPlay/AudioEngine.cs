using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using System;

namespace FrostPlay
{
    class AudioEngine : IDisposable
    {
        ISoundOut soundOut { get; set; }
        IWaveSource waveSource { get; set; }
        Uri source;
        public Uri Source
        {
            get
            {
                return source;
            }
            set
            {
                soundOut.Stop();
                source = value;
                if (source != null)
                {
                    waveSource = CodecFactory.Instance.GetCodec(value.LocalPath);
                    soundOut.Initialize(waveSource);
                    AudioOpened?.Invoke(this, null);
                }
            }
        }
        float volume;
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = Math.Min(1.0f, Math.Max(value, 0f));
                if (soundOut != null)
                    soundOut.Volume = volume;
            }
        }
        public TimeSpan Position
        {
            get
            {
                if (waveSource != null)
                    return waveSource.GetPosition();
                return TimeSpan.Zero;
            }
            set
            {
                if (waveSource != null)
                {
                    TimeSpan position;
                    if (TimeSpan.Compare(value, TimeSpan.Zero) == -1)
                        position = TimeSpan.Zero;
                    else if (TimeSpan.Compare(value, waveSource.GetLength()) == 1)
                        position = waveSource.GetLength();
                    else
                        position = value;
                    waveSource.SetPosition(position);
                }
            }
        }

        public event EventHandler AudioOpened;
        public event EventHandler<PlaybackStoppedEventArgs> AudioEnded;

        public AudioEngine()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                soundOut = new WasapiOut();
            else
                soundOut = new DirectSoundOut();
            soundOut.Stopped += (s, args) =>
            {
                if (soundOut.PlaybackState == PlaybackState.Stopped)
                    AudioEnded?.Invoke(this, null);
            };
        }

        public void Play()
        {
            if (soundOut != null)
                soundOut.Play();
        }
        public void Pause()
        {
            if (soundOut != null)
                soundOut.Pause();
        }
        public void Stop()
        {
            if (soundOut != null && waveSource != null)
            {
                soundOut.Pause();
                waveSource.SetPosition(TimeSpan.Zero);
            }
        }

        public void Dispose()
        {
            soundOut.Dispose();
            waveSource.Dispose();
        }
    }
}
