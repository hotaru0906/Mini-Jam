using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[Serializable]
public class HapticPreset
{
    public string name = "New Preset";

    [FormerlySerializedAs("leftMotor")]
    [Range(0f, 1f)]
    public float lowFrequencyMotor = 0.5f;

    [FormerlySerializedAs("rightMotor")]
    [Range(0f, 1f)]
    public float highFrequencyMotor = 0.5f;

    [Min(0f)]
    public float duration = 0.25f;

    [Min(1)]
    public int pulseCount = 1;

    [Min(0f)]
    public float pauseBetweenPulses = 0.05f;
}

[Serializable]
public class HapticTimelineEntry
{
    public string name = "Cue";

    [Min(0f)]
    public float startTime;

    public bool usePreset = true;

    [Min(0)]
    public int presetIndex;

    [FormerlySerializedAs("leftMotor")]
    [Range(0f, 1f)]
    public float lowFrequencyMotor = 0.5f;

    [FormerlySerializedAs("rightMotor")]
    [Range(0f, 1f)]
    public float highFrequencyMotor = 0.5f;

    [Min(0f)]
    public float duration = 0.25f;

    [Min(1)]
    public int pulseCount = 1;

    [Min(0f)]
    public float pauseBetweenPulses = 0.05f;
}

[Serializable]
public class HapticTimeline
{
    public string name = "New Timeline";
    public List<HapticTimelineEntry> entries = new List<HapticTimelineEntry>();
}

[AddComponentMenu("Haptics/DualSense Haptics")]
public class DualSenseHaptics : MonoBehaviour
{
    [SerializeField] private List<HapticPreset> presets = new List<HapticPreset>();
    [SerializeField, HideInInspector] private int selectedPresetIndex;
    [SerializeField] private List<HapticTimeline> timelines = new List<HapticTimeline>();
    [SerializeField, HideInInspector] private int selectedTimelineIndex;

    private Coroutine activePlaybackRoutine;

    public IReadOnlyList<HapticPreset> Presets => presets;
    public IReadOnlyList<HapticTimeline> Timelines => timelines;
    public int SelectedPresetIndex => selectedPresetIndex;
    public int SelectedTimelineIndex => selectedTimelineIndex;
    public bool HasConnectedGamepad => Gamepad.current != null;

    private void Reset()
    {
        if (presets.Count > 0)
        {
            return;
        }

        presets.Add(new HapticPreset
        {
            name = "Soft Left",
            lowFrequencyMotor = 0.2f,
            highFrequencyMotor = 0.05f,
            duration = 0.2f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });
        presets.Add(new HapticPreset
        {
            name = "Soft Right",
            lowFrequencyMotor = 0.05f,
            highFrequencyMotor = 0.2f,
            duration = 0.2f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });
        presets.Add(new HapticPreset
        {
            name = "Balanced Pulse",
            lowFrequencyMotor = 0.45f,
            highFrequencyMotor = 0.45f,
            duration = 0.15f,
            pulseCount = 2,
            pauseBetweenPulses = 0.08f
        });
        presets.Add(new HapticPreset
        {
            name = "Heavy Impact",
            lowFrequencyMotor = 1f,
            highFrequencyMotor = 0.75f,
            duration = 0.15f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });

        selectedPresetIndex = 0;

        timelines.Add(new HapticTimeline
        {
            name = "Intro Sweep",
            entries = new List<HapticTimelineEntry>
            {
                new HapticTimelineEntry
                {
                    name = "Left Lead",
                    startTime = 0f,
                    usePreset = true,
                    presetIndex = 0
                },
                new HapticTimelineEntry
                {
                    name = "Center Pulse",
                    startTime = 0.35f,
                    usePreset = true,
                    presetIndex = 2
                },
                new HapticTimelineEntry
                {
                    name = "Impact",
                    startTime = 0.8f,
                    usePreset = true,
                    presetIndex = 3
                }
            }
        });

        selectedTimelineIndex = 0;
    }

    private void OnValidate()
    {
        if (presets == null)
        {
            presets = new List<HapticPreset>();
        }

        if (presets.Count == 0)
        {
            selectedPresetIndex = 0;
        }
        else
        {
            selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Count - 1);
        }

        if (timelines == null)
        {
            timelines = new List<HapticTimeline>();
        }

        if (timelines.Count == 0)
        {
            selectedTimelineIndex = 0;
            return;
        }

        selectedTimelineIndex = Mathf.Clamp(selectedTimelineIndex, 0, timelines.Count - 1);
    }

    private void OnDisable()
    {
        StopActivePlayback();
        StopRumble();
    }

    public void SetSelectedPresetIndex(int index)
    {
        if (presets.Count == 0)
        {
            selectedPresetIndex = 0;
            return;
        }

        selectedPresetIndex = Mathf.Clamp(index, 0, presets.Count - 1);
    }

    public HapticPreset GetSelectedPreset()
    {
        if (presets.Count == 0)
        {
            return null;
        }

        return presets[selectedPresetIndex];
    }

    public void SetSelectedTimelineIndex(int index)
    {
        if (timelines.Count == 0)
        {
            selectedTimelineIndex = 0;
            return;
        }

        selectedTimelineIndex = Mathf.Clamp(index, 0, timelines.Count - 1);
    }

    public HapticTimeline GetSelectedTimeline()
    {
        if (timelines.Count == 0)
        {
            return null;
        }

        return timelines[selectedTimelineIndex];
    }

    public void PlaySelectedPreset()
    {
        HapticPreset preset = GetSelectedPreset();
        if (preset == null)
        {
            StopRumble();
            return;
        }

        Rumble(preset.lowFrequencyMotor, preset.highFrequencyMotor);
    }

    public void PlaySelectedPresetEvent()
    {
        HapticPreset preset = GetSelectedPreset();
        if (preset == null)
        {
            StopActivePlayback();
            StopRumble();
            return;
        }

        PlayPreset(preset);
    }

    public void PlaySelectedTimelineEvent()
    {
        HapticTimeline timeline = GetSelectedTimeline();
        if (timeline == null)
        {
            StopActivePlayback();
            StopRumble();
            return;
        }

        PlayTimeline(timeline);
    }

    public void PlayPresetByIndex(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= presets.Count)
        {
            return;
        }

        SetSelectedPresetIndex(presetIndex);
        PlayPreset(presets[presetIndex]);
    }

    public void PlayPresetByName(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            return;
        }

        for (int index = 0; index < presets.Count; index++)
        {
            if (!string.Equals(presets[index].name, presetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SetSelectedPresetIndex(index);
            PlayPreset(presets[index]);
            return;
        }
    }

    public void PlayTimelineByIndex(int timelineIndex)
    {
        if (timelineIndex < 0 || timelineIndex >= timelines.Count)
        {
            return;
        }

        SetSelectedTimelineIndex(timelineIndex);
        PlayTimeline(timelines[timelineIndex]);
    }

    public void PlayTimelineByName(string timelineName)
    {
        if (string.IsNullOrWhiteSpace(timelineName))
        {
            return;
        }

        for (int index = 0; index < timelines.Count; index++)
        {
            if (!string.Equals(timelines[index].name, timelineName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SetSelectedTimelineIndex(index);
            PlayTimeline(timelines[index]);
            return;
        }
    }

    public float GetSelectedPresetDuration()
    {
        HapticPreset preset = GetSelectedPreset();
        return preset == null ? 0f : Mathf.Max(0f, preset.duration);
    }

    public float GetSelectedPresetTotalDuration()
    {
        HapticPreset preset = GetSelectedPreset();
        return preset == null ? 0f : GetTotalDuration(preset);
    }

    public float GetSelectedTimelineDuration()
    {
        HapticTimeline timeline = GetSelectedTimeline();
        return timeline == null ? 0f : GetTimelineDuration(timeline);
    }

    public void PlayPreset(HapticPreset preset)
    {
        if (preset == null)
        {
            StopActivePlayback();
            StopRumble();
            return;
        }

        if (!Application.isPlaying)
        {
            Rumble(preset.lowFrequencyMotor, preset.highFrequencyMotor);
            return;
        }

        StopActivePlayback();
        activePlaybackRoutine = StartCoroutine(PlayPresetRoutine(preset));
    }

    public void PlayTimeline(HapticTimeline timeline)
    {
        if (timeline == null)
        {
            StopActivePlayback();
            StopRumble();
            return;
        }

        if (!Application.isPlaying)
        {
            HapticPreset firstPreset = GetFirstPlayablePreset(timeline);
            if (firstPreset == null)
            {
                StopRumble();
                return;
            }

            Rumble(firstPreset.lowFrequencyMotor, firstPreset.highFrequencyMotor);
            return;
        }

        StopActivePlayback();
        activePlaybackRoutine = StartCoroutine(PlayTimelineRoutine(timeline));
    }

    public void Rumble(float low, float high)
    {
        if (Gamepad.current == null) return;

        Gamepad.current.SetMotorSpeeds(
            Mathf.Clamp01(low),
            Mathf.Clamp01(high)
        );
    }

    public void StopRumble()
    {
        Gamepad.current?.SetMotorSpeeds(0, 0);
    }

    public void StopHapticsEvent()
    {
        StopActivePlayback();
        StopRumble();
    }

    private IEnumerator PlayPresetRoutine(HapticPreset preset)
    {
        yield return RunPresetPattern(preset);
        activePlaybackRoutine = null;
    }

    private IEnumerator PlayTimelineRoutine(HapticTimeline timeline)
    {
        List<HapticTimelineEntry> sortedEntries = new List<HapticTimelineEntry>(timeline.entries ?? new List<HapticTimelineEntry>());
        sortedEntries.Sort((left, right) => left.startTime.CompareTo(right.startTime));

        float elapsed = 0f;
        for (int index = 0; index < sortedEntries.Count; index++)
        {
            HapticTimelineEntry entry = sortedEntries[index];
            HapticPreset preset = ResolveTimelinePreset(entry);
            if (preset == null)
            {
                continue;
            }

            float waitTime = Mathf.Max(0f, entry.startTime - elapsed);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
                elapsed += waitTime;
            }

            yield return RunPresetPattern(preset);
            elapsed = Mathf.Max(elapsed, entry.startTime) + GetTotalDuration(preset);
        }

        StopRumble();
        activePlaybackRoutine = null;
    }

    private IEnumerator RunPresetPattern(HapticPreset preset)
    {
        int pulseCount = Mathf.Max(1, preset.pulseCount);
        float pulseDuration = Mathf.Max(0f, preset.duration);
        float pauseDuration = Mathf.Max(0f, preset.pauseBetweenPulses);

        for (int pulseIndex = 0; pulseIndex < pulseCount; pulseIndex++)
        {
            Rumble(preset.lowFrequencyMotor, preset.highFrequencyMotor);

            if (pulseDuration > 0f)
            {
                yield return new WaitForSeconds(pulseDuration);
            }

            StopRumble();

            if (pulseIndex >= pulseCount - 1 || pauseDuration <= 0f)
            {
                continue;
            }

            yield return new WaitForSeconds(pauseDuration);
        }
    }

    private void StopActivePlayback()
    {
        if (activePlaybackRoutine == null)
        {
            return;
        }

        StopCoroutine(activePlaybackRoutine);
        activePlaybackRoutine = null;
    }

    private static float GetTotalDuration(HapticPreset preset)
    {
        int pulseCount = Mathf.Max(1, preset.pulseCount);
        float pulseDuration = Mathf.Max(0f, preset.duration);
        float pauseDuration = Mathf.Max(0f, preset.pauseBetweenPulses);

        return (pulseCount * pulseDuration) + ((pulseCount - 1) * pauseDuration);
    }

    private HapticPreset GetFirstPlayablePreset(HapticTimeline timeline)
    {
        if (timeline == null || timeline.entries == null)
        {
            return null;
        }

        HapticTimelineEntry firstEntry = null;
        for (int index = 0; index < timeline.entries.Count; index++)
        {
            HapticTimelineEntry entry = timeline.entries[index];
            if (firstEntry == null || entry.startTime < firstEntry.startTime)
            {
                firstEntry = entry;
            }
        }

        return ResolveTimelinePreset(firstEntry);
    }

    private HapticPreset ResolveTimelinePreset(HapticTimelineEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        if (entry.usePreset)
        {
            if (entry.presetIndex < 0 || entry.presetIndex >= presets.Count)
            {
                return null;
            }

            return presets[entry.presetIndex];
        }

        return new HapticPreset
        {
            name = entry.name,
            lowFrequencyMotor = entry.lowFrequencyMotor,
            highFrequencyMotor = entry.highFrequencyMotor,
            duration = entry.duration,
            pulseCount = entry.pulseCount,
            pauseBetweenPulses = entry.pauseBetweenPulses
        };
    }

    private float GetTimelineDuration(HapticTimeline timeline)
    {
        if (timeline == null || timeline.entries == null || timeline.entries.Count == 0)
        {
            return 0f;
        }

        float maxEndTime = 0f;
        for (int index = 0; index < timeline.entries.Count; index++)
        {
            HapticPreset preset = ResolveTimelinePreset(timeline.entries[index]);
            if (preset == null)
            {
                continue;
            }

            float endTime = Mathf.Max(0f, timeline.entries[index].startTime) + GetTotalDuration(preset);
            if (endTime > maxEndTime)
            {
                maxEndTime = endTime;
            }
        }

        return maxEndTime;
    }
}