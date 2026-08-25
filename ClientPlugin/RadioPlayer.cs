using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using VRage.Utils;

namespace ClientPlugin
{
    internal sealed class RadioPlayer : IDisposable
    {
        private readonly object syncRoot = new object();
        private IWavePlayer outputDevice;
        private MediaFoundationReader reader;
        private CancellationTokenSource startupCancellation;
        private float volume = 0.5f;

        public bool IsPlaying { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Math.Max(0f, Math.Min(1f, value));
                lock (syncRoot)
                {
                    if (outputDevice != null)
                        outputDevice.Volume = volume;
                }
            }
        }

        public void Play(string streamUrl, float requestedVolume)
        {
            if (string.IsNullOrWhiteSpace(streamUrl))
                throw new ArgumentException("Stream URL is empty.", nameof(streamUrl));

            Uri uri;
            if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Stream URL must be an absolute HTTP or HTTPS URL.", nameof(streamUrl));
            }

            Stop();
            Volume = requestedVolume;

            startupCancellation = new CancellationTokenSource();
            var token = startupCancellation.Token;

            Task.Run(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var newReader = new MediaFoundationReader(streamUrl);
                    token.ThrowIfCancellationRequested();

                    var newOutput = new WaveOutEvent();
                    newOutput.Init(newReader);
                    newOutput.Volume = Volume;
                    newOutput.PlaybackStopped += OnPlaybackStopped;

                    lock (syncRoot)
                    {
                        token.ThrowIfCancellationRequested();
                        reader = newReader;
                        outputDevice = newOutput;
                        IsPlaying = true;
                        newOutput.Play();
                    }

                    MyLog.Default.WriteLineAndConsole($"{Plugin.Name}: Streaming {streamUrl}");
                }
                catch (OperationCanceledException)
                {
                    MyLog.Default.WriteLine($"{Plugin.Name}: Radio startup cancelled.");
                }
                catch (Exception ex)
                {
                    IsPlaying = false;
                    MyLog.Default.Error($"{Plugin.Name}: Radio startup failed: {ex}");
                }
            }, token);
        }

        public void Stop()
        {
            startupCancellation?.Cancel();
            startupCancellation?.Dispose();
            startupCancellation = null;

            IWavePlayer outputToDispose;
            MediaFoundationReader readerToDispose;

            lock (syncRoot)
            {
                IsPlaying = false;
                outputToDispose = outputDevice;
                readerToDispose = reader;
                outputDevice = null;
                reader = null;
            }

            if (outputToDispose != null)
            {
                outputToDispose.PlaybackStopped -= OnPlaybackStopped;
                outputToDispose.Stop();
                outputToDispose.Dispose();
            }

            readerToDispose?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                MyLog.Default.Error($"{Plugin.Name}: Radio playback stopped with error: {e.Exception}");

            IsPlaying = false;
        }
    }
}
