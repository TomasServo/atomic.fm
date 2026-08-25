using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClientPlugin
{
    public class Config : INotifyPropertyChanged
    {
        #region Options

        private string streamUrl = "http://3.140.179.166:8000/atomic-radio";
        private float volume = 0.15f;
        private bool autoplay;
        private bool soundBlockMode = true;
        private string soundBlockTag = "[atomic.fm]";
        private float fallbackSpeakerRange = 50f;
        private bool muteOutsideSpeakerRange;

        #endregion

        #region User interface

        public readonly string Title = "atomic.fm";

        [Textbox("Stream URL", description: "HTTP or HTTPS MP3/AAC stream URL.")]
        public string StreamUrl
        {
            get => streamUrl;
            set => SetField(ref streamUrl, value);
        }

        [Slider(0f, 1f, 0.05f, SliderAttribute.SliderType.Float, label: "Volume", description: "Playback volume from 0 to 1.")]
        public float Volume
        {
            get => volume;
            set => SetField(ref volume, value);
        }

        [Checkbox("Autoplay", description: "Start the configured stream when the plugin loads.")]
        public bool Autoplay
        {
            get => autoplay;
            set => SetField(ref autoplay, value);
        }

        [Checkbox("Sound Block speakers", description: "Use tagged Sound Blocks as local radio speaker locations.")]
        public bool SoundBlockMode
        {
            get => soundBlockMode;
            set => SetField(ref soundBlockMode, value);
        }

        [Textbox("Sound Block tag", description: "Sound Blocks whose name contains this tag act as atomic.fm speakers.")]
        public string SoundBlockTag
        {
            get => soundBlockTag;
            set => SetField(ref soundBlockTag, value);
        }

        [Slider(5f, 200f, 5f, SliderAttribute.SliderType.Float, label: "Fallback range", description: "Range in meters used when a tagged Sound Block has no usable range value.")]
        public float FallbackSpeakerRange
        {
            get => fallbackSpeakerRange;
            set => SetField(ref fallbackSpeakerRange, value);
        }

        [Checkbox("Mute out of range", description: "Keep the stream running but mute it when no tagged Sound Block is nearby.")]
        public bool MuteOutsideSpeakerRange
        {
            get => muteOutsideSpeakerRange;
            set => SetField(ref muteOutsideSpeakerRange, value);
        }

        [Button("Start atomic.fm", description: "Start streaming the configured station.")]
        public void StartRadio()
        {
            Plugin.Instance?.StartPlayback();
        }

        [Button("Stop atomic.fm", description: "Stop radio playback.")]
        public void StopRadio()
        {
            Plugin.Instance?.StopPlayback();
        }

        #endregion

        #region Property change notification boilerplate

        public static readonly Config Default = new Config();
        public static readonly Config Current = ConfigStorage.Load();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
