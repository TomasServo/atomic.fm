using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VRage.Input;

namespace ClientPlugin
{
    public class Config : INotifyPropertyChanged
    {
        #region Options

        private string streamUrl = "http://3.140.179.166:8000/atomic-radio";
        private float volume = 0.35f;
        private bool autoplay;
        private Binding togglePlaybackKey = new Binding(MyKeys.R, ctrl: true, alt: true);

        #endregion

        #region User interface

        public readonly string Title = "Atomic Radio";

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

        [Keybind("Toggle Key", description: "Press this keybind in game to start or stop the stream. Unbind by right clicking the button.")]
        public Binding TogglePlaybackKey
        {
            get => togglePlaybackKey;
            set => SetField(ref togglePlaybackKey, value);
        }

        [Button("Start Radio", description: "Start streaming the configured station.")]
        public void StartRadio()
        {
            Plugin.Instance?.StartPlayback();
        }

        [Button("Stop Radio", description: "Stop radio playback.")]
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
