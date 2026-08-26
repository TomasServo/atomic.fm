using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VRage.Utils;

namespace ClientPlugin
{
    internal sealed class RadioPlayer : IDisposable
    {
        private readonly object syncRoot = new object();
        private object outputDevice;
        private object reader;
        private CancellationTokenSource startupCancellation;
        private float volume = MaxOutputVolume;
        private float pan;
        private Type waveOutType;
        private Type mediaFoundationReaderType;
        private Type waveProviderType;
        private const float MaxOutputVolume = Config.DefaultVolume;

        public bool IsPlaying { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Math.Max(0f, Math.Min(MaxOutputVolume, value));
                lock (syncRoot)
                {
                    SetOutputVolume(outputDevice, volume);
                }
            }
        }

        public float Pan
        {
            get => pan;
            set
            {
                pan = Math.Max(-1f, Math.Min(1f, value));
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
                    EnsureAudioTypesLoaded();
                    token.ThrowIfCancellationRequested();
                    var newReader = Activator.CreateInstance(mediaFoundationReaderType, streamUrl);
                    token.ThrowIfCancellationRequested();

                    var newOutput = Activator.CreateInstance(waveOutType);
                    SetOutputVolume(newOutput, Volume);
                    waveOutType.GetMethod("Init", new[] { waveProviderType })?.Invoke(newOutput, new[] { newReader });
                    SetOutputVolume(newOutput, Volume);

                    lock (syncRoot)
                    {
                        token.ThrowIfCancellationRequested();
                        reader = newReader;
                        outputDevice = newOutput;
                        IsPlaying = true;
                        waveOutType.GetMethod("Play")?.Invoke(newOutput, null);
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

            object outputToDispose;
            object readerToDispose;

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
                outputToDispose.GetType().GetMethod("Stop")?.Invoke(outputToDispose, null);
                (outputToDispose as IDisposable)?.Dispose();
            }

            (readerToDispose as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }

        private void EnsureAudioTypesLoaded()
        {
            if (waveOutType != null && mediaFoundationReaderType != null && waveProviderType != null)
                return;

            AssemblyResolver.Register();

            waveOutType = Type.GetType("NAudio.Wave.WaveOutEvent, NAudio.WinMM", true);
            mediaFoundationReaderType = Type.GetType("NAudio.Wave.MediaFoundationReader, NAudio.Wasapi", true);
            waveProviderType = Type.GetType("NAudio.Wave.IWaveProvider, NAudio.Core", true);
        }

        private static void SetOutputVolume(object output, float requestedVolume)
        {
            if (output == null)
                return;

            var volumeProperty = output.GetType().GetProperty("Volume", BindingFlags.Instance | BindingFlags.Public);
            if (volumeProperty != null && volumeProperty.CanWrite)
                volumeProperty.SetValue(output, Math.Max(0f, Math.Min(MaxOutputVolume, requestedVolume)), null);
        }
    }
}
