using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NodeConnection
{
    public NodeDirection direction;
    public AudioNode node;
}

public class AudioNode : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Danh sách node kề theo từng hướng")]
    public List<NodeConnection> connections = new List<NodeConnection>();

    // AudioEventHandler sẽ được thêm vào ở System [5]
    // public List<AudioEventHandler> audioEvents;

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
