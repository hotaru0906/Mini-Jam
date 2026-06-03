using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpatialAudioSource))]
public class SpatialAudioSourceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpatialAudioSource src = (SpatialAudioSource)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("── Test trong Editor ──", EditorStyles.boldLabel);

        // Chỉ dùng được khi đang Play Mode
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Play Ambient"))
            src.PlayAmbient();
        if (GUILayout.Button("▶ Play Trigger"))
            src.PlayTrigger();
        if (GUILayout.Button("■ Stop"))
            src.Stop();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fade In (1s)"))
            src.FadeIn(1f);
        if (GUILayout.Button("Fade Out (1s)"))
            src.FadeOut(1f);
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Nhấn Play trong Unity để dùng các nút test.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("── Preset khoảng cách ──", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chọn preset phù hợp với loại âm thanh, rồi chỉnh tay nếu cần.",
            MessageType.None
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Rất gần\n(min 1 / max 8)"))
            ApplyDistancePreset(src, 1f, 8f);

        if (GUILayout.Button("Gần\n(min 2 / max 15)"))
            ApplyDistancePreset(src, 2f, 15f);

        if (GUILayout.Button("Vừa\n(min 3 / max 22)"))
            ApplyDistancePreset(src, 3f, 22f);

        if (GUILayout.Button("Xa\n(min 5 / max 40)"))
            ApplyDistancePreset(src, 5f, 40f);

        EditorGUILayout.EndHorizontal();
    }

    private void ApplyDistancePreset(SpatialAudioSource src, float min, float max)
    {
        Undo.RecordObject(src, "Apply Distance Preset");
        src.minDistance = min;
        src.maxDistance = max;
        EditorUtility.SetDirty(src);
    }
}
