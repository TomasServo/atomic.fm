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
        private object waveChannel;
        private CancellationTokenSource startupCancellation;
        private float volume = Config.DefaultVolume;
        private float pan;
        private Type waveOutType;
        private Type mediaFoundationReaderType;
        private Type waveChannel32Type;
        private Type waveProviderType;

        public bool IsPlaying { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Clamp01(value);
                lock (syncRoot)
                {
                    SetStreamVolume(waveChannel, volume);
                }
            }
        }

        public float Pan
        {
            get => pan;
            set
            {
                pan = Clamp(value, -1f, 1f);
                lock (syncRoot)
                {
                    SetStreamPan(waveChannel, pan);
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
                    EnsureAudioTypesLoaded();
                    token.ThrowIfCancellationRequested();

                    var newReader = Activator.CreateInstance(mediaFoundationReaderType, streamUrl);
                    token.ThrowIfCancellationRequested();

                    var newWaveChannel = Activator.CreateInstance(waveChannel32Type, newReader);
                    SetStreamVolume(newWaveChannel, Volume);
                    SetStreamPan(newWaveChannel, Pan);

                    var newOutput = Activator.CreateInstance(waveOutType);
                    waveOutType.GetMethod("Init", new[] { waveProviderType })?.Invoke(newOutput, new[] { newWaveChannel });

                    lock (syncRoot)
                    {
                        token.ThrowIfCancellationRequested();
                        reader = newReader;
                        waveChannel = newWaveChannel;
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
            object channelToDispose;
            object readerToDispose;

            lock (syncRoot)
            {
                IsPlaying = false;
                outputToDispose = outputDevice;
                channelToDispose = waveChannel;
                readerToDispose = reader;
                outputDevice = null;
                waveChannel = null;
                reader = null;
            }

            if (outputToDispose != null)
            {
                outputToDispose.GetType().GetMethod("Stop")?.Invoke(outputToDispose, null);
                (outputToDispose as IDisposable)?.Dispose();
            }

            (channelToDispose as IDisposable)?.Dispose();
            (readerToDispose as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }

        private void EnsureAudioTypesLoaded()
        {
            if (waveOutType != null && mediaFoundationReaderType != null && waveChannel32Type != null && waveProviderType != null)
                return;

            AssemblyResolver.Register();

            waveOutType = Type.GetType("NAudio.Wave.WaveOutEvent, NAudio.WinMM", true);
            mediaFoundationReaderType = Type.GetType("NAudio.Wave.MediaFoundationReader, NAudio.Wasapi", true);
            waveChannel32Type = Type.GetType("NAudio.Wave.WaveChannel32, NAudio.Core", true);
            waveProviderType = Type.GetType("NAudio.Wave.IWaveProvider, NAudio.Core", true);
        }

        private static void SetStreamVolume(object stream, float requestedVolume)
        {
            SetFloatProperty(stream, "Volume", Clamp01(requestedVolume));
        }

        private static void SetStreamPan(object stream, float requestedPan)
        {
            SetFloatProperty(stream, "Pan", Clamp(requestedPan, -1f, 1f));
        }

        private static void SetFloatProperty(object target, string propertyName, float value)
        {
            if (target == null)
                return;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
                property.SetValue(target, value, null);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
