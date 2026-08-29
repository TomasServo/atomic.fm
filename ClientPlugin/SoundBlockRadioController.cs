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
        private const string CustomDataRangeKey = "atomic.fm.range";
        private const string CustomDataVolumeKey = "atomic.fm.volume";
        private const int ScanIntervalFrames = 60;
        private readonly HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        private readonly List<IMyTerminalBlock> anchors = new List<IMyTerminalBlock>();
        private int framesUntilScan;
        private float lastVolumeMultiplier = 1f;
        private float lastPan;

        public int AnchorCount => anchors.Count;
        public string NearestAnchorName { get; private set; } = string.Empty;
        public double NearestAnchorDistance { get; private set; }
        public float LastVolumeMultiplier => lastVolumeMultiplier;
        public float LastPan => lastPan;

        public void ForceRefresh(Config config)
        {
            RefreshAnchors(config.SoundBlockTag);
            framesUntilScan = ScanIntervalFrames;
        }

        public float GetEffectiveVolume(Config config)
        {
            float baseVolume = Clamp(config.Volume, 0f, Config.MaxVolume);

            if (!config.SoundBlockMode)
            {
                lastPan = 0f;
                return baseVolume;
            }

            float multiplier = GetSpeakerMultiplier(config);
            lastVolumeMultiplier = Smooth(lastVolumeMultiplier, multiplier, 0.15f);
            return baseVolume * lastVolumeMultiplier;
        }

        private float GetSpeakerMultiplier(Config config)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
            {
                lastPan = 0f;
                return 1f;
            }

            if (--framesUntilScan <= 0)
            {
                framesUntilScan = ScanIntervalFrames;
                RefreshAnchors(config.SoundBlockTag);
            }

            if (anchors.Count == 0)
            {
                lastPan = 0f;
                return config.MuteOutsideSpeakerRange ? 0f : 1f;
            }

            Vector3D listenerPosition = MyAPIGateway.Session.Camera.Position;
            float strongest = 0f;
            float selectedPan = 0f;
            double nearestDistance = double.MaxValue;
            string nearestName = string.Empty;

            for (int i = anchors.Count - 1; i >= 0; i--)
            {
                IMyTerminalBlock anchor = anchors[i];
                if (!IsUsableAnchor(anchor, config.SoundBlockTag))
                {
                    anchors.RemoveAt(i);
                    continue;
                }

                float range = GetAnchorRange(anchor, config.FallbackSpeakerRange);

                double distance = Vector3D.Distance(listenerPosition, anchor.GetPosition());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestName = anchor.CustomName?.ToString() ?? anchor.DisplayNameText ?? "marked block";
                }

                if (distance > range)
                    continue;

                float distanceFactor = 1f - (float)(distance / range);
                float anchorVolume = GetAnchorVolume(anchor);
                float score = distanceFactor * anchorVolume;
                if (score > strongest)
                {
                    strongest = score;
                    selectedPan = CalculatePan(listenerPosition, anchor.GetPosition());
                }
            }

            NearestAnchorName = nearestName;
            NearestAnchorDistance = nearestDistance == double.MaxValue ? 0d : nearestDistance;
            lastPan = strongest > 0f ? selectedPan : 0f;
            return Clamp01(strongest);
        }

        private static float CalculatePan(Vector3D listenerPosition, Vector3D anchorPosition)
        {
            Vector3D toAnchor = anchorPosition - listenerPosition;
            double distance = toAnchor.Length();
            if (distance <= 0.001d)
                return 0f;

            Vector3D direction = toAnchor / distance;
            Vector3D right = MyAPIGateway.Session.Camera.WorldMatrix.Right;
            return Clamp((float)Vector3D.Dot(direction, right), -1f, 1f);
        }

        private void RefreshAnchors(string tag)
        {
            anchors.Clear();
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
                        IMyTerminalBlock anchor = fatBlock as IMyTerminalBlock;
                        if (anchor != null && IsUsableAnchor(anchor, tag))
                            anchors.Add(anchor);
                    }
                }

                MyLog.Default.WriteLine($"{Plugin.Name}: radio anchors found: {anchors.Count}");
            }
            catch (Exception ex)
            {
                MyLog.Default.Error($"{Plugin.Name}: radio anchor scan failed: {ex}");
            }
            finally
            {
                entities.Clear();
            }
        }

        private static bool IsUsableAnchor(IMyTerminalBlock anchor, string tag)
        {
            if (anchor == null || anchor.Closed || anchor.MarkedForClose)
                return false;

            IMyFunctionalBlock functionalBlock = anchor as IMyFunctionalBlock;
            if (functionalBlock != null && (!functionalBlock.IsFunctional || !functionalBlock.IsWorking || !functionalBlock.Enabled))
                return false;

            string requiredTag = string.IsNullOrWhiteSpace(tag) ? "[atomic.fm]" : tag;
            string customName = anchor.CustomName?.ToString() ?? string.Empty;
            if (customName.IndexOf(requiredTag, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return HasEnabledCustomData(anchor.CustomData);
        }

        private static float GetAnchorRange(IMyTerminalBlock anchor, float fallbackRange)
        {
            IMySoundBlock soundBlock = anchor as IMySoundBlock;
            if (soundBlock != null && soundBlock.Range > 1f)
                return Math.Max(1f, soundBlock.Range);

            float customRange;
            if (TryGetCustomDataFloat(anchor.CustomData, CustomDataRangeKey, out customRange))
                return Math.Max(1f, customRange);

            return Math.Max(1f, fallbackRange);
        }

        private static float GetAnchorVolume(IMyTerminalBlock anchor)
        {
            float customVolume;
            if (TryGetCustomDataFloat(anchor.CustomData, CustomDataVolumeKey, out customVolume))
                return Clamp(customVolume, 0f, Config.MaxVolume) / Config.MaxVolume;

            IMySoundBlock soundBlock = anchor as IMySoundBlock;
            return soundBlock != null ? Clamp01(soundBlock.Volume) : 1f;
        }

        private static bool HasEnabledCustomData(string customData)
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

        private static bool TryGetCustomDataFloat(string customData, string expectedKey, out float value)
        {
            value = 0f;
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
                if (!key.Equals(expectedKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rawValue = line.Substring(separatorIndex + 1).Trim();
                return float.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
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

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
