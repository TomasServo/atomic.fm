using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin
{
    internal sealed class SoundBlockRadioController
    {
        private const string CustomDataKey = "atomic.fm";
        private const int ScanIntervalFrames = 60;
        private readonly HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        private readonly List<IMySoundBlock> speakers = new List<IMySoundBlock>();
        private int framesUntilScan;
        private float lastVolumeMultiplier = 1f;

        public float GetEffectiveVolume(Config config)
        {
            float baseVolume = Clamp01(config.Volume);

            if (!config.SoundBlockMode)
                return baseVolume;

            float multiplier = GetSpeakerMultiplier(config);
            lastVolumeMultiplier = Smooth(lastVolumeMultiplier, multiplier, 0.15f);
            return baseVolume * lastVolumeMultiplier;
        }

        private float GetSpeakerMultiplier(Config config)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return 1f;

            if (--framesUntilScan <= 0)
            {
                framesUntilScan = ScanIntervalFrames;
                RefreshSpeakers(config.SoundBlockTag);
            }

            if (speakers.Count == 0)
                return 1f;

            Vector3D listenerPosition = MyAPIGateway.Session.Camera.Position;
            float strongest = 0f;

            for (int i = speakers.Count - 1; i >= 0; i--)
            {
                IMySoundBlock speaker = speakers[i];
                if (!IsUsableSpeaker(speaker, config.SoundBlockTag))
                {
                    speakers.RemoveAt(i);
                    continue;
                }

                float range = Math.Max(1f, speaker.Range);
                if (range <= 1f)
                    range = Math.Max(1f, config.FallbackSpeakerRange);

                double distance = Vector3D.Distance(listenerPosition, speaker.GetPosition());
                if (distance > range)
                    continue;

                float distanceFactor = 1f - (float)(distance / range);
                float speakerVolume = Clamp01(speaker.Volume);
                strongest = Math.Max(strongest, distanceFactor * speakerVolume);
            }

            if (strongest <= 0f && !config.MuteOutsideSpeakerRange)
                return 1f;

            return Clamp01(strongest);
        }

        private void RefreshSpeakers(string tag)
        {
            speakers.Clear();
            entities.Clear();

            try
            {
                MyAPIGateway.Entities.GetEntities(entities, entity =>
                {
                    return entity is MyCubeGrid;
                });

                foreach (IMyEntity entity in entities)
                {
                    MyCubeGrid grid = entity as MyCubeGrid;
                    if (grid == null || grid.MarkedForClose || grid.Closed)
                        continue;

                    foreach (var fatBlock in grid.GetFatBlocks())
                    {
                        IMySoundBlock speaker = fatBlock as IMySoundBlock;
                        if (speaker != null && IsUsableSpeaker(speaker, tag))
                            speakers.Add(speaker);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.Error($"{Plugin.Name}: Sound Block scan failed: {ex}");
            }
            finally
            {
                entities.Clear();
            }
        }

        private static bool IsUsableSpeaker(IMySoundBlock speaker, string tag)
        {
            if (speaker == null || speaker.Closed || speaker.MarkedForClose)
                return false;

            if (!speaker.IsFunctional || !speaker.IsWorking || !speaker.Enabled)
                return false;

            string requiredTag = string.IsNullOrWhiteSpace(tag) ? "[atomic.fm]" : tag;
            string customName = speaker.CustomName?.ToString() ?? string.Empty;
            if (customName.IndexOf(requiredTag, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return HasAtomicFmCustomData(speaker.CustomData);
        }

        private static bool HasAtomicFmCustomData(string customData)
        {
            if (string.IsNullOrWhiteSpace(customData))
                return false;

            string[] lines = customData.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("//"))
                    continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                    separatorIndex = line.IndexOf(':');

                if (separatorIndex < 0)
                    continue;

                string key = line.Substring(0, separatorIndex).Trim();
                if (!key.Equals(CustomDataKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                string value = line.Substring(separatorIndex + 1).Trim();
                return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("1", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static float Smooth(float current, float target, float factor)
        {
            return current + ((target - current) * factor);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
