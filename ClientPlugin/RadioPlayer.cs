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
        private AtomicFmSampleProvider sampleProvider;
        private CancellationTokenSource startupCancellation;
        private float volume = Config.DefaultVolume;
        private float pan;

        public bool IsPlaying { get; private set; }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Clamp01(value);
                lock (syncRoot)
                {
                    if (sampleProvider != null)
                        sampleProvider.Volume = volume;
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
                    AssemblyResolver.Register();
                    token.ThrowIfCancellationRequested();

                    var newReader = new MediaFoundationReader(streamUrl);
                    token.ThrowIfCancellationRequested();

                    var newSampleProvider = new AtomicFmSampleProvider(new WaveToSampleProvider(newReader))
                    {
                        Volume = Volume,
                        Pan = Pan
                    };

                    var newOutput = new WaveOutEvent();
                    newOutput.Init(new SampleToWaveProvider(newSampleProvider));

                    lock (syncRoot)
                    {
                        token.ThrowIfCancellationRequested();
                        reader = newReader;
                        sampleProvider = newSampleProvider;
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

            outputToDispose?.Stop();
            outputToDispose?.Dispose();
            readerToDispose?.Dispose();
        }

        public void Dispose()
        {
            Stop();
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

        private sealed class AtomicFmSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider source;

            public AtomicFmSampleProvider(ISampleProvider source)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
            }

            public WaveFormat WaveFormat => source.WaveFormat;

            public float Volume { get; set; } = 1f;

            public float Pan { get; set; }

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = source.Read(buffer, offset, count);
                int channels = WaveFormat.Channels;
                float volume = Clamp01(Volume);

                if (channels < 2)
                {
                    for (int i = 0; i < samplesRead; i++)
                        buffer[offset + i] *= volume;

                    return samplesRead;
                }

                float pan = Clamp(Pan, -1f, 1f);
                float leftGain = volume * (pan <= 0f ? 1f : 1f - pan);
                float rightGain = volume * (pan >= 0f ? 1f : 1f + pan);

                int end = offset + samplesRead;
                for (int i = offset; i < end; i += channels)
                {
                    buffer[i] *= leftGain;
                    if (i + 1 < end)
                        buffer[i + 1] *= rightGain;

                    for (int channel = 2; channel < channels && i + channel < end; channel++)
                        buffer[i + channel] *= volume;
                }

                return samplesRead;
            }
        }
    }
}
