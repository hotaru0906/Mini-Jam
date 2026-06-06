using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Serialization;

[System.Serializable]
public class NodeConnection
{
    public NodeDirection direction;
    public AudioNode node;
}

[System.Serializable]
public class NodeHapticCue
{
    [Min(0f)] public float delay;
    public AudioNodeHapticsMode mode = AudioNodeHapticsMode.CustomPulse;
    public int presetIndex;
    public string presetName = "Heavy Impact";
    public int timelineIndex;
    public string timelineName = "Intro Sweep";
    [FormerlySerializedAs("leftMotor")]
    [Range(0f, 1f)] public float leftMotor = 0.4f;
    [FormerlySerializedAs("rightMotor")]
    [Range(0f, 1f)] public float rightMotor = 0.7f;
    [Min(0f)] public float pulseDuration = 0.25f;
}

public enum AudioNodeHapticsMode
{
    SelectedPreset,
    PresetByIndex,
    PresetByName,
    SelectedTimeline,
    TimelineByIndex,
    TimelineByName,
    CustomPulse
}

public class AudioNode : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Danh sách node kề theo từng hướng")]
    public List<NodeConnection> connections = new List<NodeConnection>();

    [Header("Node Haptics")]
    [Tooltip("Tự rung khi player đến node này")]
    [SerializeField] private bool playHapticsOnEnter;
    [Tooltip("Mỗi cue sẽ chạy một lần theo delay tính từ lúc player vào node")]
    [SerializeField] private List<NodeHapticCue> hapticCues = new List<NodeHapticCue>
    {
        new NodeHapticCue()
    };

    // AudioEventHandler sẽ được thêm vào ở System [5]
    // public List<AudioEventHandler> audioEvents;

    private Coroutine hapticsRoutine;

    // -------------------------------------------------------
    // Navigation
    // -------------------------------------------------------

    /// <summary>Trả về node kề theo hướng chỉ định, hoặc null nếu không có.</summary>
    public AudioNode GetNeighbor(NodeDirection direction)
    {
        foreach (var connection in connections)
        {
            if (connection.direction == direction)
                return connection.node;
        }
        return null;
    }

    public bool HasNeighbor(NodeDirection direction)
    {
        return GetNeighbor(direction) != null;
    }

    public void TriggerEnterHaptics()
    {
        if (!playHapticsOnEnter || hapticCues == null || hapticCues.Count == 0)
        {
            return;
        }

        if (hapticsRoutine != null)
        {
            StopCoroutine(hapticsRoutine);
        }

        hapticsRoutine = StartCoroutine(TriggerEnterHapticsRoutine());
    }

    private IEnumerator TriggerEnterHapticsRoutine()
    {
        List<NodeHapticCue> sortedCues = new List<NodeHapticCue>(hapticCues);
        sortedCues.Sort((left, right) => left.delay.CompareTo(right.delay));

        float elapsed = 0f;
        for (int index = 0; index < sortedCues.Count; index++)
        {
            NodeHapticCue cue = sortedCues[index];
            if (cue == null)
            {
                continue;
            }

            float waitTime = Mathf.Max(0f, cue.delay - elapsed);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
                elapsed += waitTime;
            }

            PlayHapticCue(cue);
            elapsed = Mathf.Max(elapsed, cue.delay);
        }

        hapticsRoutine = null;
    }

    private static void PlayHapticCue(NodeHapticCue cue)
    {
        switch (cue.mode)
        {
            case AudioNodeHapticsMode.SelectedPreset:
                GlobalHaptics.PlaySelectedPreset();
                break;

            case AudioNodeHapticsMode.PresetByIndex:
                GlobalHaptics.PlayPreset(cue.presetIndex);
                break;

            case AudioNodeHapticsMode.PresetByName:
                GlobalHaptics.PlayPreset(cue.presetName);
                break;

            case AudioNodeHapticsMode.SelectedTimeline:
                GlobalHaptics.PlaySelectedTimeline();
                break;

            case AudioNodeHapticsMode.TimelineByIndex:
                GlobalHaptics.PlayTimeline(cue.timelineIndex);
                break;

            case AudioNodeHapticsMode.TimelineByName:
                GlobalHaptics.PlayTimeline(cue.timelineName);
                break;

            case AudioNodeHapticsMode.CustomPulse:
                GlobalHaptics.Pulse(cue.leftMotor, cue.rightMotor, cue.pulseDuration);
                break;
        }
    }

    // -------------------------------------------------------
    // Editor Gizmos — chỉ hiện trong Scene View, không ảnh hưởng build
    // -------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Vẽ hình cầu tại vị trí node
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Vẽ đường nối tới các node kề
        Gizmos.color = Color.yellow;
        foreach (var connection in connections)
        {
            if (connection.node != null)
                Gizmos.DrawLine(transform.position, connection.node.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Khi select node này, hiện nhãn hướng
        UnityEditor.Handles.color = Color.white;
        foreach (var connection in connections)
        {
            if (connection.node != null)
            {
                Vector3 mid = (transform.position + connection.node.transform.position) * 0.5f;
                UnityEditor.Handles.Label(mid, connection.direction.ToString());
            }
        }
    }
#endif
}
