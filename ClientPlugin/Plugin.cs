using System;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;

namespace ClientPlugin
{
    // ReSharper disable once UnusedType.Global
    public class Plugin : IPlugin, IDisposable
    {
        public const string Name = "atomic.fm";
        public static Plugin Instance { get; private set; }
        private SettingsGenerator settingsGenerator;
        private RadioPlayer radioPlayer;
        private SoundBlockRadioController soundBlockController;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;
            settingsGenerator = new SettingsGenerator();
            radioPlayer = new RadioPlayer();
            soundBlockController = new SoundBlockRadioController();
            Config.Current.PropertyChanged += OnConfigChanged;

            if (Config.Current.Autoplay)
                radioPlayer.Play(Config.Current.StreamUrl, Config.Current.Volume);
        }

        public void Dispose()
        {
            Config.Current.PropertyChanged -= OnConfigChanged;
            ConfigStorage.Save(Config.Current);
            soundBlockController = null;
            radioPlayer?.Dispose();
            radioPlayer = null;
            Instance = null;
        }

        public void Update()
        {
            if (MyInput.Static.IsAnyCtrlKeyPressed() &&
                MyInput.Static.IsAnyAltKeyPressed() &&
                MyInput.Static.IsNewKeyPressed(MyKeys.F))
            {
                TogglePlayback();
            }

            if (radioPlayer != null && radioPlayer.IsPlaying)
                radioPlayer.Volume = soundBlockController.GetEffectiveVolume(Config.Current);
        }

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            Instance.settingsGenerator.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(Instance.settingsGenerator.Dialog);
        }

        public void TogglePlayback()
        {
            if (radioPlayer == null)
                return;

            if (radioPlayer.IsPlaying)
                StopPlayback();
            else
                StartPlayback();
        }

        public void StartPlayback()
        {
            if (radioPlayer == null)
                return;

            try
            {
                radioPlayer.Play(Config.Current.StreamUrl, Config.Current.Volume);
            }
            catch (Exception ex)
            {
                MyLog.Default.Error($"{Name}: Failed to start stream: {ex}");
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: new System.Text.StringBuilder(ex.Message),
                    messageCaption: new System.Text.StringBuilder("atomic.fm")));
            }
        }

        public void StopPlayback()
        {
            radioPlayer?.Stop();
        }

        private void OnConfigChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (radioPlayer != null)
                radioPlayer.Volume = soundBlockController.GetEffectiveVolume(Config.Current);
        }
    }
}
