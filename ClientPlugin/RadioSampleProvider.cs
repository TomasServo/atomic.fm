using System;
using NAudio.Wave;

namespace ClientPlugin
{
    /// <summary>
    /// Applies volume and stereo pan. Always outputs stereo so mono streams can pan.
    /// Pan is -1 (full left) to +1 (full right).
    /// </summary>
    internal sealed class RadioSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly int sourceChannels;
        private readonly WaveFormat waveFormat;
        private readonly float[] sourceBuffer = new float[4096];
        private float volume = 1f;
        private float pan;

        public RadioSampleProvider(ISampleProvider source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            this.source = source;
            sourceChannels = source.WaveFormat.Channels;
            if (sourceChannels < 1 || sourceChannels > 2)
                throw new ArgumentException("RadioSampleProvider supports mono or stereo sources only.", nameof(source));

            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
        }

        public WaveFormat WaveFormat => waveFormat;

        public float Volume
        {
            get => volume;
            set => volume = Clamp01(value);
        }

        public float Pan
        {
            get => pan;
            set => pan = Clamp(value, -1f, 1f);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (count % 2 != 0)
                throw new ArgumentException("Stereo sample count must be even.", nameof(count));

            float leftGain;
            float rightGain;
            CalculatePanGains(pan, out leftGain, out rightGain);
            leftGain *= volume;
            rightGain *= volume;

            if (sourceChannels == 2)
            {
                int samplesRead = source.Read(buffer, offset, count);
                int end = offset + samplesRead;
                for (int i = offset; i < end; i += 2)
                {
                    buffer[i] *= leftGain;
                    if (i + 1 < end)
                        buffer[i + 1] *= rightGain;
                }

                return samplesRead;
            }

            // Mono source: upmix to stereo with pan.
            int monoSamplesNeeded = count / 2;
            int stereoWritten = 0;

            while (monoSamplesNeeded > 0)
            {
                int toRead = Math.Min(monoSamplesNeeded, sourceBuffer.Length);
                int read = source.Read(sourceBuffer, 0, toRead);
                if (read <= 0)
                    break;

                for (int i = 0; i < read; i++)
                {
                    float sample = sourceBuffer[i];
                    buffer[offset + stereoWritten] = sample * leftGain;
                    buffer[offset + stereoWritten + 1] = sample * rightGain;
                    stereoWritten += 2;
                }

                monoSamplesNeeded -= read;
            }

            return stereoWritten;
        }

        private static void CalculatePanGains(float panValue, out float leftGain, out float rightGain)
        {
            // Constant-power pan: equal perceived loudness across the stereo field.
            float angle = (panValue + 1f) * 0.25f * (float)Math.PI;
            leftGain = (float)Math.Cos(angle);
            rightGain = (float)Math.Sin(angle);
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
