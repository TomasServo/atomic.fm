using System;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
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
        private int framesUntilStatusNotification;
        private int framesUntilAmbientScan;
        private bool manualStopRequested = true;

        private const int AmbientScanIntervalFrames = 300;

        static Plugin()
        {
            AssemblyResolver.Register();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;
            settingsGenerator = new SettingsGenerator();
            radioPlayer = new RadioPlayer();
            soundBlockController = new SoundBlockRadioController();
            Config.Current.PropertyChanged += OnConfigChanged;
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
            if (IsMainMenuOpen())
            {
                StopPlayback(showNotification: false);

                return;
            }

            if (MyInput.Static.IsAnyCtrlKeyPressed() &&
                MyInput.Static.IsAnyAltKeyPressed() &&
                MyInput.Static.IsNewKeyPressed(MyKeys.M))
            {
                TogglePlayback();
            }

            if (radioPlayer != null && radioPlayer.IsPlaying)
            {
                manualStopRequested = false;
                radioPlayer.Volume = soundBlockController.GetEffectiveVolume(Config.Current);
                radioPlayer.Pan = soundBlockController.LastPan;
                ShowAnchorStatusPeriodically();
                return;
            }

            TryStartAmbientPlayback();
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

            if (IsMainMenuOpen() || MyAPIGateway.Session == null)
            {
                manualStopRequested = true;
                radioPlayer.Stop();
                return;
            }

            try
            {
                manualStopRequested = false;
                framesUntilAmbientScan = AmbientScanIntervalFrames;
                soundBlockController.ForceRefresh(Config.Current);
                radioPlayer.Play(Config.Current.StreamUrl, soundBlockController.GetEffectiveVolume(Config.Current));
                ShowNotification($"atomic.fm starting - anchors found: {soundBlockController.AnchorCount}", 3000);
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
            StopPlayback(showNotification: true);
        }

        private void StopPlayback(bool showNotification)
        {
            manualStopRequested = true;
            radioPlayer?.Stop();
            if (showNotification)
                ShowNotification("atomic.fm stopped", 2000);
        }

        private void TryStartAmbientPlayback()
        {
            if (manualStopRequested || radioPlayer == null || radioPlayer.IsPlaying || !Config.Current.SoundBlockMode)
                return;

            if (--framesUntilAmbientScan > 0)
                return;

            framesUntilAmbientScan = AmbientScanIntervalFrames;
            soundBlockController.ForceRefresh(Config.Current);
            if (soundBlockController.AnchorCount <= 0)
                return;

            StartPlayback();
        }

        private void OnConfigChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (radioPlayer != null)
            {
                radioPlayer.Volume = soundBlockController.GetEffectiveVolume(Config.Current);
                radioPlayer.Pan = soundBlockController.LastPan;
            }
        }

        private void ShowAnchorStatusPeriodically()
        {
            if (--framesUntilStatusNotification > 0)
                return;

            framesUntilStatusNotification = 600;
            if (soundBlockController.AnchorCount <= 0)
            {
                ShowNotification("atomic.fm: no block anchors found", 2500);
                return;
            }

            ShowNotification(
                $"atomic.fm: {soundBlockController.AnchorCount} anchor(s), nearest {soundBlockController.NearestAnchorDistance:0}m, volume {soundBlockController.LastVolumeMultiplier:0.00}, pan {soundBlockController.LastPan:0.00}",
                2500);
        }

        private static void ShowNotification(string message, int aliveTimeMs)
        {
            try
            {
                MyAPIGateway.Utilities?.ShowNotification(message, aliveTimeMs, "White");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"{Name}: notification failed: {ex.Message}");
            }
        }

        private static bool IsMainMenuOpen()
        {
            try
            {
                return MyScreenManager.IsScreenOfTypeOpen(typeof(MyGuiScreenMainMenu));
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"{Name}: main menu detection failed: {ex.Message}");
                return MyAPIGateway.Session == null;
            }
        }
    }
}
