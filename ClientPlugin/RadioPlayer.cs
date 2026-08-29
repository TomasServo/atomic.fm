using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VRage.Utils;

namespace ClientPlugin
{
    internal sealed class RadioPlayer : IDisposable
    {
        private readonly object syncRoot = new object();
        private WaveOutEvent outputDevice;
        private MediaFoundationReader reader;
        private RadioSampleProvider sampleProvider;
        private CancellationTokenSource startupCancellation;
        private float volume = Config.DefaultVolume;
        private float pan;
        private bool disposed;

        public bool IsPlaying { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Clamp(value, 0f, Config.MaxVolume);
                lock (syncRoot)
                {
                    if (sampleProvider != null)
                        sampleProvider.Volume = ToAudioGain(volume);
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
                    if (sampleProvider != null)
                        sampleProvider.Pan = pan;
                }
            }
        }

        public void Play(string streamUrl, float requestedVolume)
        {
            ThrowIfDisposed();

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
                MediaFoundationReader newReader = null;
                RadioSampleProvider newSampleProvider = null;
                WaveOutEvent newOutput = null;

                try
                {
                    AssemblyResolver.Register();
                    token.ThrowIfCancellationRequested();

                    newReader = new MediaFoundationReader(streamUrl);
                    token.ThrowIfCancellationRequested();

                    newSampleProvider = new RadioSampleProvider(new WaveToSampleProvider(newReader))
                    {
                        Volume = ToAudioGain(Volume),
                        Pan = Pan
                    };

                    newOutput = new WaveOutEvent();
                    newOutput.Init(new SampleToWaveProvider(newSampleProvider));

                    lock (syncRoot)
                    {
                        token.ThrowIfCancellationRequested();
                        if (disposed)
                            throw new OperationCanceledException(token);

                        reader = newReader;
                        sampleProvider = newSampleProvider;
                        outputDevice = newOutput;
                        IsPlaying = true;
                        newOutput.Play();

                        // Ownership transferred to instance fields.
                        newReader = null;
                        newSampleProvider = null;
                        newOutput = null;
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
                finally
                {
                    // Dispose anything that failed to transfer into the instance.
                    if (newOutput != null)
                    {
                        try { newOutput.Stop(); } catch { /* ignore */ }
                        try { newOutput.Dispose(); } catch { /* ignore */ }
                    }

                    if (newReader != null)
                    {
                        try { newReader.Dispose(); } catch { /* ignore */ }
                    }
                }
            }, token);
        }

        public void Stop()
        {
            startupCancellation?.Cancel();
            startupCancellation?.Dispose();
            startupCancellation = null;

            WaveOutEvent outputToDispose;
            MediaFoundationReader readerToDispose;

            lock (syncRoot)
            {
                IsPlaying = false;
                outputToDispose = outputDevice;
                readerToDispose = reader;
                outputDevice = null;
                reader = null;
                sampleProvider = null;
            }

            if (outputToDispose != null)
            {
                try { outputToDispose.Stop(); } catch { /* ignore */ }
                try { outputToDispose.Dispose(); } catch { /* ignore */ }
            }

            if (readerToDispose != null)
            {
                try { readerToDispose.Dispose(); } catch { /* ignore */ }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Stop();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(RadioPlayer));
        }

        private static float ToAudioGain(float userVolume)
        {
            return Clamp(userVolume, 0f, Config.MaxVolume) / Config.MaxVolume;
        }


        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
