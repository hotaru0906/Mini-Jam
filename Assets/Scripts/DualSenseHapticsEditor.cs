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

        DrawPropertiesExcluding(serializedObject, "m_Script", "selectedPresetIndex");

        SerializedProperty selectedIndexProperty = serializedObject.FindProperty("selectedPresetIndex");
        DualSenseHaptics haptics = (DualSenseHaptics)target;

        EditorGUILayout.Space();
        DrawPresetSelector(haptics, selectedIndexProperty);

        EditorGUILayout.HelpBox(
            "PS5/DualSense qua Unity Input System mac dinh chi cho test 2 motor: leftMotor (low-frequency) va rightMotor (high-frequency). Khong co API dinh vi tung diem rung tren tay cam trong component nay.",
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

        if (!haptics.HasConnectedGamepad)
        {
            EditorGUILayout.HelpBox("Chua thay gamepad duoc ket noi trong Input System.", MessageType.Warning);
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
            "Left: " + selectedPreset.leftMotor.ToString("0.00")
            + " | Right: " + selectedPreset.rightMotor.ToString("0.00")
            + " | Duration: " + selectedPreset.duration.ToString("0.00") + "s");
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