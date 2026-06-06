using UnityEngine;

[AddComponentMenu("Haptics/Haptics Event Caller")]
public class HapticsEventCaller : MonoBehaviour
{
    [SerializeField] private DualSenseHaptics haptics;
    [SerializeField] private int presetIndex;
    [SerializeField] private string presetName = "Heavy Impact";

    public void PlaySelectedPreset()
    {
        if (haptics == null)
        {
            return;
        }

        haptics.PlaySelectedPresetEvent();
    }

    public void PlayPresetByIndex()
    {
        if (haptics == null)
        {
            return;
        }

        haptics.PlayPresetByIndex(presetIndex);
    }

    public void PlayPresetByName()
    {
        if (haptics == null)
        {
            return;
        }

        haptics.PlayPresetByName(presetName);
    }

    public void StopHaptics()
    {
        if (haptics == null)
        {
            return;
        }

        haptics.StopHapticsEvent();
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayPresetByIndex();
    }
}