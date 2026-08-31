using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin
{
    internal sealed class SoundBlockRadioController
    {
        private const string CustomDataKey = "atomic.fm";
        private const string CustomDataEnabledKey = "enabled";
        private const string CustomDataRangeKey = "atomic.fm.range";
        private const string CustomDataShortRangeKey = "range";
        private const string CustomDataVolumeKey = "atomic.fm.volume";
        private const string CustomDataShortVolumeKey = "volume";
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
            float baseVolume = Config.VolumeToGain(config.Volume);

            if (IsOpeningMenuActive())
            {
                lastPan = 0f;
                lastVolumeMultiplier = 1f;
                return 0f;
            }

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
            if (--framesUntilScan <= 0)
            {
                framesUntilScan = ScanIntervalFrames;
                RefreshAnchors(config.SoundBlockTag);
            }

            if (anchors.Count == 0)
            {
                lastPan = 0f;
                return 1f;
            }

            Vector3D listenerPosition;
            if (!TryGetListenerPosition(out listenerPosition))
            {
                lastPan = 0f;
                return config.MuteOutsideSpeakerRange ? 0f : 1f;
            }

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
            Vector3D right;
            if (!TryGetListenerRight(out right))
                return 0f;

            Vector3D toAnchor = anchorPosition - listenerPosition;
            double distance = toAnchor.Length();
            if (distance <= 0.001d)
                return 0f;

            Vector3D direction = toAnchor / distance;
            return Clamp((float)Vector3D.Dot(direction, right), -1f, 1f);
        }

        private static bool TryGetListenerPosition(out Vector3D position)
        {
            position = Vector3D.Zero;

            if (IsOpeningMenuActive())
                return false;

            if (MyAPIGateway.Session.Camera != null)
            {
                position = MyAPIGateway.Session.Camera.Position;
                return true;
            }

            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity;
            if (controlledEntity == null)
                return false;

            position = controlledEntity.GetPosition();
            return true;
        }

        private static bool TryGetListenerRight(out Vector3D right)
        {
            right = Vector3D.Right;

            if (IsOpeningMenuActive())
                return false;

            if (MyAPIGateway.Session.Camera != null)
            {
                right = MyAPIGateway.Session.Camera.WorldMatrix.Right;
                return true;
            }

            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity;
            if (controlledEntity == null)
                return false;

            right = controlledEntity.WorldMatrix.Right;
            return true;
        }

        private static bool IsOpeningMenuActive()
        {
            return MyAPIGateway.Session == null;
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

            return HasAtomicFmCustomData(anchor.CustomData);
        }

        private static float GetAnchorRange(IMyTerminalBlock anchor, float fallbackRange)
        {
            IMySoundBlock soundBlock = anchor as IMySoundBlock;
            if (soundBlock != null && soundBlock.Range > 1f)
                return Math.Max(1f, soundBlock.Range);

            float customRange;
            if (TryGetCustomDataFloat(anchor.CustomData, CustomDataRangeKey, CustomDataShortRangeKey, out customRange))
                return Math.Max(1f, customRange);

            return Math.Max(1f, fallbackRange);
        }

        private static float GetAnchorVolume(IMyTerminalBlock anchor)
        {
            float customVolume;
            if (TryGetCustomDataFloat(anchor.CustomData, CustomDataVolumeKey, CustomDataShortVolumeKey, out customVolume))
                return VolumeSettingToGain(customVolume);

            IMySoundBlock soundBlock = anchor as IMySoundBlock;
            return soundBlock != null ? Clamp01(soundBlock.Volume) : 0.5f;
        }

        private static bool HasAtomicFmCustomData(string customData)
        {
            if (string.IsNullOrWhiteSpace(customData))
                return false;

            foreach (CustomDataEntry entry in ReadCustomDataEntries(customData))
            {
                bool isExplicitKey = entry.Key.Equals(CustomDataKey, StringComparison.OrdinalIgnoreCase);
                bool isSectionEnabled = entry.Section.Equals(CustomDataKey, StringComparison.OrdinalIgnoreCase) &&
                    entry.Key.Equals(CustomDataEnabledKey, StringComparison.OrdinalIgnoreCase);
                if (!isExplicitKey && !isSectionEnabled)
                    continue;

                return IsTruthy(entry.Value);
            }

            return false;
        }

        private static bool TryGetCustomDataFloat(string customData, string fullKey, string sectionKey, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(customData))
                return false;

            foreach (CustomDataEntry entry in ReadCustomDataEntries(customData))
            {
                bool isFullKey = entry.Key.Equals(fullKey, StringComparison.OrdinalIgnoreCase);
                bool isSectionKey = entry.Section.Equals(CustomDataKey, StringComparison.OrdinalIgnoreCase) &&
                    entry.Key.Equals(sectionKey, StringComparison.OrdinalIgnoreCase);
                if (!isFullKey && !isSectionKey)
                    continue;

                string rawValue = entry.Value.Replace(',', '.');
                return float.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
            }

            return false;
        }

        private static IEnumerable<CustomDataEntry> ReadCustomDataEntries(string customData)
        {
            string section = string.Empty;
            string[] lines = customData.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("//"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]") && line.Length > 2)
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                    separatorIndex = line.IndexOf(':');

                if (separatorIndex < 0)
                    continue;

                yield return new CustomDataEntry
                {
                    Section = section,
                    Key = line.Substring(0, separatorIndex).Trim(),
                    Value = line.Substring(separatorIndex + 1).Trim()
                };
            }
        }

        private static bool IsTruthy(string value)
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private static float VolumeSettingToGain(float userVolume)
        {
            if (userVolume < 0f)
                userVolume = 0f;
            if (userVolume > 10f)
                userVolume = 10f;

            return userVolume / 10f;
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

        private struct CustomDataEntry
        {
            public string Section;
            public string Key;
            public string Value;
        }
    }
}
