using System.Collections;
using UnityEngine;

[AddComponentMenu("Haptics/Global Haptics")]
public class GlobalHaptics : MonoBehaviour
{
    public static GlobalHaptics Instance { get; private set; }

    [SerializeField] private DualSenseHaptics haptics;

    private Coroutine oneshotRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveHaptics();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static bool HasHaptics()
    {
        return TryGetInstance(out GlobalHaptics globalHaptics) && globalHaptics.ResolveHaptics() != null;
    }

    public static void PlaySelectedPreset()
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlaySelectedPresetEvent();
    }

    public static void PlayPreset(int presetIndex)
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlayPresetByIndex(presetIndex);
    }

    public static void PlayPreset(string presetName)
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlayPresetByName(presetName);
    }

    public static void PlaySelectedTimeline()
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlaySelectedTimelineEvent();
    }

    public static void PlayTimeline(int timelineIndex)
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlayTimelineByIndex(timelineIndex);
    }

    public static void PlayTimeline(string timelineName)
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.PlayTimelineByName(timelineName);
    }

    public static void Rumble(float low, float high)
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.Rumble(low, high);
    }

    public static void Pulse(float low, float high, float duration)
    {
        if (!TryGetInstance(out GlobalHaptics globalHaptics))
        {
            return;
        }

        globalHaptics.PlayPulseInternal(low, high, duration);
    }

    public static void Stop()
    {
        if (!TryGetTarget(out DualSenseHaptics target))
        {
            return;
        }

        target.StopHapticsEvent();
    }

    public void SetHaptics(DualSenseHaptics target)
    {
        haptics = target;
    }

    private void PlayPulseInternal(float low, float high, float duration)
    {
        DualSenseHaptics target = ResolveHaptics();
        if (target == null)
        {
            return;
        }

        if (oneshotRoutine != null)
        {
            StopCoroutine(oneshotRoutine);
        }

        oneshotRoutine = StartCoroutine(PulseRoutine(target, low, high, duration));
    }

    private IEnumerator PulseRoutine(DualSenseHaptics target, float low, float high, float duration)
    {
        target.Rumble(low, high);

        float clampedDuration = Mathf.Max(0f, duration);
        if (clampedDuration > 0f)
        {
            yield return new WaitForSeconds(clampedDuration);
        }

        target.StopHapticsEvent();
        oneshotRoutine = null;
    }

    private DualSenseHaptics ResolveHaptics()
    {
        if (haptics == null)
        {
            haptics = FindFirstObjectByType<DualSenseHaptics>();
        }

        return haptics;
    }

    private static bool TryGetInstance(out GlobalHaptics globalHaptics)
    {
        globalHaptics = Instance;
        if (globalHaptics != null)
        {
            return true;
        }

        globalHaptics = FindFirstObjectByType<GlobalHaptics>();
        if (globalHaptics != null)
        {
            Instance = globalHaptics;
            return true;
        }

        return false;
    }

    private static bool TryGetTarget(out DualSenseHaptics target)
    {
        target = null;
        if (!TryGetInstance(out GlobalHaptics globalHaptics))
        {
            return false;
        }

        target = globalHaptics.ResolveHaptics();
        return target != null;
    }
}