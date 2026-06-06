using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[AddComponentMenu("Haptics/Haptics Auto Trigger")]
public class HapticsAutoTrigger : MonoBehaviour
{
    private enum HapticsMode
    {
        SelectedPreset,
        PresetByIndex,
        PresetByName,
        SelectedTimeline,
        TimelineByIndex,
        TimelineByName,
        CustomPulse
    }

    [Header("Target")]
    [SerializeField] private DualSenseHaptics haptics;

    [Header("Trigger")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0f)] private float delay = 0.5f;
    [SerializeField] private bool repeat;
    [SerializeField, Min(0f)] private float repeatInterval = 1f;
    [SerializeField] private bool useUnscaledTime;

    [Header("Haptics")]
    [SerializeField] private HapticsMode mode = HapticsMode.SelectedTimeline;
    [SerializeField] private int presetIndex;
    [SerializeField] private string presetName = "Heavy Impact";
    [SerializeField] private int timelineIndex;
    [SerializeField] private string timelineName = "Intro Sweep";
    [FormerlySerializedAs("leftMotor")]
    [SerializeField, Range(0f, 1f)] private float lowFrequencyMotor = 0.4f;
    [FormerlySerializedAs("rightMotor")]
    [SerializeField, Range(0f, 1f)] private float highFrequencyMotor = 0.7f;
    [SerializeField, Min(0f)] private float pulseDuration = 0.25f;

    private Coroutine scheduleRoutine;

    private void Awake()
    {
        ResolveHaptics();
    }

    private void OnEnable()
    {
        if (!playOnEnable)
        {
            return;
        }

        Schedule();
    }

    private void OnDisable()
    {
        StopScheduledTrigger();
    }

    public void Schedule()
    {
        ResolveHaptics();

        if (scheduleRoutine != null)
        {
            StopCoroutine(scheduleRoutine);
        }

        scheduleRoutine = StartCoroutine(ScheduleRoutine());
    }

    public void PlayNow()
    {
        DualSenseHaptics target = ResolveHaptics();
        if (target == null)
        {
            return;
        }

        switch (mode)
        {
            case HapticsMode.SelectedPreset:
                target.PlaySelectedPresetEvent();
                break;

            case HapticsMode.PresetByIndex:
                target.PlayPresetByIndex(presetIndex);
                break;

            case HapticsMode.PresetByName:
                target.PlayPresetByName(presetName);
                break;

            case HapticsMode.SelectedTimeline:
                target.PlaySelectedTimelineEvent();
                break;

            case HapticsMode.TimelineByIndex:
                target.PlayTimelineByIndex(timelineIndex);
                break;

            case HapticsMode.TimelineByName:
                target.PlayTimelineByName(timelineName);
                break;

            case HapticsMode.CustomPulse:
                GlobalHaptics.Pulse(lowFrequencyMotor, highFrequencyMotor, pulseDuration);
                break;
        }
    }

    public void StopScheduledTrigger()
    {
        if (scheduleRoutine == null)
        {
            return;
        }

        StopCoroutine(scheduleRoutine);
        scheduleRoutine = null;
    }

    public void StopHaptics()
    {
        DualSenseHaptics target = ResolveHaptics();
        if (target == null)
        {
            return;
        }

        target.StopHapticsEvent();
    }

    private IEnumerator ScheduleRoutine()
    {
        do
        {
            if (delay > 0f)
            {
                yield return Wait(delay);
            }

            PlayNow();

            if (!repeat)
            {
                break;
            }

            float interval = Mathf.Max(0f, repeatInterval);
            if (interval > 0f)
            {
                yield return Wait(interval);
            }
        }
        while (repeat);

        scheduleRoutine = null;
    }

    private object Wait(float seconds)
    {
        return useUnscaledTime
            ? new WaitForSecondsRealtime(seconds)
            : new WaitForSeconds(seconds);
    }

    private DualSenseHaptics ResolveHaptics()
    {
        if (haptics == null)
        {
            haptics = FindFirstObjectByType<DualSenseHaptics>();
        }

        if (haptics != null && GlobalHaptics.Instance != null)
        {
            GlobalHaptics.Instance.SetHaptics(haptics);
        }

        return haptics;
    }
}