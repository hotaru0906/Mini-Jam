using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class HapticPreset
{
    public string name = "New Preset";

    [Range(0f, 1f)]
    public float leftMotor = 0.5f;

    [Range(0f, 1f)]
    public float rightMotor = 0.5f;

    [Min(0f)]
    public float duration = 0.25f;

    [Min(1)]
    public int pulseCount = 1;

    [Min(0f)]
    public float pauseBetweenPulses = 0.05f;
}

[AddComponentMenu("Haptics/DualSense Haptics")]
public class DualSenseHaptics : MonoBehaviour
{
    [SerializeField] private List<HapticPreset> presets = new List<HapticPreset>();
    [SerializeField, HideInInspector] private int selectedPresetIndex;

    private Coroutine activeRumbleRoutine;

    public IReadOnlyList<HapticPreset> Presets => presets;
    public int SelectedPresetIndex => selectedPresetIndex;
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
            leftMotor = 0.2f,
            rightMotor = 0.05f,
            duration = 0.2f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });
        presets.Add(new HapticPreset
        {
            name = "Soft Right",
            leftMotor = 0.05f,
            rightMotor = 0.2f,
            duration = 0.2f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });
        presets.Add(new HapticPreset
        {
            name = "Balanced Pulse",
            leftMotor = 0.45f,
            rightMotor = 0.45f,
            duration = 0.15f,
            pulseCount = 2,
            pauseBetweenPulses = 0.08f
        });
        presets.Add(new HapticPreset
        {
            name = "Heavy Impact",
            leftMotor = 1f,
            rightMotor = 0.75f,
            duration = 0.15f,
            pulseCount = 1,
            pauseBetweenPulses = 0.05f
        });

        selectedPresetIndex = 0;
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
            return;
        }

        selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Count - 1);
    }

    private void OnDisable()
    {
        StopActiveRoutine();
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

    public void PlaySelectedPreset()
    {
        HapticPreset preset = GetSelectedPreset();
        if (preset == null)
        {
            StopRumble();
            return;
        }

        Rumble(preset.leftMotor, preset.rightMotor);
    }

    public void PlaySelectedPresetEvent()
    {
        HapticPreset preset = GetSelectedPreset();
        if (preset == null)
        {
            StopActiveRoutine();
            StopRumble();
            return;
        }

        PlayPreset(preset);
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

    public void PlayPreset(HapticPreset preset)
    {
        if (preset == null)
        {
            StopActiveRoutine();
            StopRumble();
            return;
        }

        if (!Application.isPlaying)
        {
            Rumble(preset.leftMotor, preset.rightMotor);
            return;
        }

        StopActiveRoutine();
        activeRumbleRoutine = StartCoroutine(PlayPresetRoutine(preset));
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
        StopActiveRoutine();
        StopRumble();
    }

    private IEnumerator PlayPresetRoutine(HapticPreset preset)
    {
        int pulseCount = Mathf.Max(1, preset.pulseCount);
        float pulseDuration = Mathf.Max(0f, preset.duration);
        float pauseDuration = Mathf.Max(0f, preset.pauseBetweenPulses);

        for (int pulseIndex = 0; pulseIndex < pulseCount; pulseIndex++)
        {
            Rumble(preset.leftMotor, preset.rightMotor);

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

        activeRumbleRoutine = null;
    }

    private void StopActiveRoutine()
    {
        if (activeRumbleRoutine == null)
        {
            return;
        }

        StopCoroutine(activeRumbleRoutine);
        activeRumbleRoutine = null;
    }

    private static float GetTotalDuration(HapticPreset preset)
    {
        int pulseCount = Mathf.Max(1, preset.pulseCount);
        float pulseDuration = Mathf.Max(0f, preset.duration);
        float pauseDuration = Mathf.Max(0f, preset.pauseBetweenPulses);

        return (pulseCount * pulseDuration) + ((pulseCount - 1) * pauseDuration);
    }
}