using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DualSenseHaptics))]
public class DualSenseHapticsEditor : Editor
{
    private static DualSenseHaptics previewTarget;
    private static double previewStopTime;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "selectedPresetIndex", "selectedTimelineIndex");

        SerializedProperty selectedIndexProperty = serializedObject.FindProperty("selectedPresetIndex");
        SerializedProperty selectedTimelineProperty = serializedObject.FindProperty("selectedTimelineIndex");
        DualSenseHaptics haptics = (DualSenseHaptics)target;

        EditorGUILayout.Space();
        DrawPresetSelector(haptics, selectedIndexProperty);
        EditorGUILayout.Space();
        DrawTimelineSelector(haptics, selectedTimelineProperty);

        EditorGUILayout.HelpBox(
            "PS5/DualSense qua Unity Input System mac dinh chi cho test 2 motor: lowFrequencyMotor va highFrequencyMotor. Day khong phai la trai/phai vat ly tren tay cam, ma la 2 kieu motor rung khac nhau.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(!haptics.HasConnectedGamepad))
        {
            if (GUILayout.Button("Play Selected Preset"))
            {
                StartPreview(haptics);
            }

            if (GUILayout.Button("Stop Rumble"))
            {
                StopPreview();
            }
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || !haptics.HasConnectedGamepad))
        {
            if (GUILayout.Button("Play Selected Timeline"))
            {
                haptics.PlaySelectedTimelineEvent();
            }
        }

        if (!haptics.HasConnectedGamepad)
        {
            EditorGUILayout.HelpBox("Chua thay gamepad duoc ket noi trong Input System.", MessageType.Warning);
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Timeline chay bang coroutine, nen nut Play Selected Timeline chi hoat dong khi dang Play Mode.", MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawPresetSelector(DualSenseHaptics haptics, SerializedProperty selectedIndexProperty)
    {
        if (haptics.Presets.Count == 0)
        {
            EditorGUILayout.HelpBox("Danh sach preset dang rong. Them preset moi trong list o tren.", MessageType.Warning);
            selectedIndexProperty.intValue = 0;
            return;
        }

        string[] options = new string[haptics.Presets.Count];
        for (int index = 0; index < haptics.Presets.Count; index++)
        {
            HapticPreset preset = haptics.Presets[index];
            options[index] = string.IsNullOrWhiteSpace(preset.name)
                ? "Preset " + (index + 1)
                : preset.name;
        }

        int safeIndex = Mathf.Clamp(selectedIndexProperty.intValue, 0, options.Length - 1);
        int newIndex = EditorGUILayout.Popup("Selected Preset", safeIndex, options);
        if (newIndex != safeIndex)
        {
            selectedIndexProperty.intValue = newIndex;
            haptics.SetSelectedPresetIndex(newIndex);
        }

        HapticPreset selectedPreset = haptics.Presets[Mathf.Clamp(newIndex, 0, haptics.Presets.Count - 1)];
        EditorGUILayout.LabelField(
            "Preview Values",
            "Low Freq: " + selectedPreset.lowFrequencyMotor.ToString("0.00")
            + " | High Freq: " + selectedPreset.highFrequencyMotor.ToString("0.00")
            + " | Duration: " + selectedPreset.duration.ToString("0.00") + "s");
    }

    private static void DrawTimelineSelector(DualSenseHaptics haptics, SerializedProperty selectedTimelineProperty)
    {
        if (haptics.Timelines.Count == 0)
        {
            EditorGUILayout.HelpBox("Danh sach timeline dang rong. Them timeline va cac moc rung trong list o tren.", MessageType.Warning);
            selectedTimelineProperty.intValue = 0;
            return;
        }

        string[] options = new string[haptics.Timelines.Count];
        for (int index = 0; index < haptics.Timelines.Count; index++)
        {
            HapticTimeline timeline = haptics.Timelines[index];
            options[index] = string.IsNullOrWhiteSpace(timeline.name)
                ? "Timeline " + (index + 1)
                : timeline.name;
        }

        int safeIndex = Mathf.Clamp(selectedTimelineProperty.intValue, 0, options.Length - 1);
        int newIndex = EditorGUILayout.Popup("Selected Timeline", safeIndex, options);
        if (newIndex != safeIndex)
        {
            selectedTimelineProperty.intValue = newIndex;
            haptics.SetSelectedTimelineIndex(newIndex);
        }

        EditorGUILayout.LabelField(
            "Timeline Duration",
            haptics.GetSelectedTimelineDuration().ToString("0.00") + "s");
    }

    private static void StartPreview(DualSenseHaptics haptics)
    {
        StopPreview();

        haptics.PlaySelectedPreset();
        previewTarget = haptics;

        float duration = haptics.GetSelectedPresetDuration();
        if (duration <= 0f)
        {
            return;
        }

        previewStopTime = EditorApplication.timeSinceStartup + duration;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        if (previewTarget == null)
        {
            StopPreview();
            return;
        }

        if (EditorApplication.timeSinceStartup < previewStopTime)
        {
            return;
        }

        StopPreview();
    }

    private static void StopPreview()
    {
        EditorApplication.update -= OnEditorUpdate;

        if (previewTarget != null)
        {
            previewTarget.StopRumble();
        }

        previewTarget = null;
        previewStopTime = 0d;
    }
}