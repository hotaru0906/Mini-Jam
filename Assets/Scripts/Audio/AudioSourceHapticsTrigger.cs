using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Haptics/Audio Source Haptics Trigger")]
public class AudioSourceHapticsTrigger : MonoBehaviour
{
    private enum HapticTriggerMode
    {
        SelectedPreset,
        PresetByIndex,
        PresetByName,
        SelectedTimeline,
        TimelineByIndex,
        TimelineByName
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private DualSenseHaptics haptics;
    [SerializeField] private HapticTriggerMode triggerMode = HapticTriggerMode.SelectedTimeline;
    [SerializeField] private int presetIndex;
    [SerializeField] private string presetName = "Heavy Impact";
    [SerializeField] private int timelineIndex;
    [SerializeField] private string timelineName = "Intro Sweep";
    [SerializeField] private bool triggerIfAlreadyPlayingOnEnable = true;
    [SerializeField] private bool stopHapticsWhenAudioStops = true;

    private bool wasPlaying;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        bool isPlaying = audioSource != null && audioSource.isPlaying;
        wasPlaying = isPlaying;

        if (isPlaying && triggerIfAlreadyPlayingOnEnable)
        {
            TriggerHaptics();
        }
    }

    private void Update()
    {
        if (audioSource == null || haptics == null)
        {
            return;
        }

        bool isPlaying = audioSource.isPlaying;
        if (isPlaying && !wasPlaying)
        {
            TriggerHaptics();
        }
        else if (!isPlaying && wasPlaying && stopHapticsWhenAudioStops)
        {
            haptics.StopHapticsEvent();
        }

        wasPlaying = isPlaying;
    }

    public void TriggerHaptics()
    {
        if (haptics == null)
        {
            return;
        }

        switch (triggerMode)
        {
            case HapticTriggerMode.SelectedPreset:
                haptics.PlaySelectedPresetEvent();
                break;

            case HapticTriggerMode.PresetByIndex:
                haptics.PlayPresetByIndex(presetIndex);
                break;

            case HapticTriggerMode.PresetByName:
                haptics.PlayPresetByName(presetName);
                break;

            case HapticTriggerMode.SelectedTimeline:
                haptics.PlaySelectedTimelineEvent();
                break;

            case HapticTriggerMode.TimelineByIndex:
                haptics.PlayTimelineByIndex(timelineIndex);
                break;

            case HapticTriggerMode.TimelineByName:
                haptics.PlayTimelineByName(timelineName);
                break;
        }
    }

    public void StopHaptics()
    {
        if (haptics == null)
        {
            return;
        }

        haptics.StopHapticsEvent();
    }
}