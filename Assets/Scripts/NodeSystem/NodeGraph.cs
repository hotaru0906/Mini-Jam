using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý toàn bộ node trong 1 khu vực (scene).
/// Đặt script này lên 1 GameObject duy nhất tên "NodeGraph" trong scene.
/// </summary>
public class NodeGraph : MonoBehaviour
{
    public static NodeGraph Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Node đầu tiên player xuất hiện khi vào khu vực này")]
    public AudioNode startNode;

    [Header("All Nodes (tự động điền bằng nút Refresh bên dưới)")]
    public List<AudioNode> allNodes = new List<AudioNode>();

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // -------------------------------------------------------
    // Queries
    // -------------------------------------------------------

    /// <summary>Tìm node theo tên GameObject.</summary>
    public AudioNode GetNodeByName(string nodeName)
    {
        foreach (var node in allNodes)
        {
            if (node.gameObject.name == nodeName)
                return node;
        }
        return null;
    }

    // -------------------------------------------------------
    // Editor Utility
    // -------------------------------------------------------

    /// <summary>
    /// Quét toàn scene, thu thập tất cả AudioNode vào danh sách.
    /// Gọi bằng nút trong Inspector (xem NodeGraphEditor).
    /// </summary>
    public void RefreshNodeList()
    {
        allNodes.Clear();
        var found = FindObjectsByType<AudioNode>(FindObjectsSortMode.None);
        allNodes.AddRange(found);
        Debug.Log($"[NodeGraph] Tìm thấy {allNodes.Count} node.");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Highlight start node bằng màu xanh lá
        if (startNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startNode.transform.position, 0.55f);
        }
    }
#endif
}
