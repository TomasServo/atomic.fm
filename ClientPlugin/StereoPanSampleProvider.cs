using System;
using NAudio.Wave;

namespace ClientPlugin
{
    internal sealed class StereoPanSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] monoBuffer;
        private float volume = 1f;
        private float pan;

        public StereoPanSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));

            if (source.WaveFormat.Channels == 1)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
                monoBuffer = new float[8192];
            }
            else
            {
                WaveFormat = source.WaveFormat;
            }
        }

        public WaveFormat WaveFormat { get; }

        public float Volume
        {
            get => volume;
            set => volume = Clamp(value, 0f, 1f);
        }

        public float Pan
        {
            get => pan;
            set => pan = Clamp(value, -1f, 1f);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            GetGains(out float leftGain, out float rightGain);

            if (source.WaveFormat.Channels == 1)
                return ReadMonoAsStereo(buffer, offset, count, leftGain, rightGain);

            int read = source.Read(buffer, offset, count);
            if (source.WaveFormat.Channels == 2)
            {
                for (int n = 0; n + 1 < read; n += 2)
                {
                    buffer[offset + n] *= leftGain;
                    buffer[offset + n + 1] *= rightGain;
                }
            }
            else
            {
                for (int n = 0; n < read; n++)
                    buffer[offset + n] *= volume;
            }

            return read;
        }

        private int ReadMonoAsStereo(float[] buffer, int offset, int count, float leftGain, float rightGain)
        {
            int monoSamplesRequested = Math.Min(count / 2, monoBuffer.Length);
            int monoSamplesRead = source.Read(monoBuffer, 0, monoSamplesRequested);

            for (int n = 0; n < monoSamplesRead; n++)
            {
                float sample = monoBuffer[n];
                int stereoOffset = offset + (n * 2);
                buffer[stereoOffset] = sample * leftGain;
                buffer[stereoOffset + 1] = sample * rightGain;
            }

            return monoSamplesRead * 2;
        }

        private void GetGains(out float leftGain, out float rightGain)
        {
            float currentPan = Clamp(pan, -1f, 1f);
            float currentVolume = Clamp(volume, 0f, 1f);

            leftGain = currentPan > 0f ? 1f - currentPan : 1f;
            rightGain = currentPan < 0f ? 1f + currentPan : 1f;

            leftGain *= currentVolume;
            rightGain *= currentVolume;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
