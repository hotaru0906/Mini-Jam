using UnityEngine;

/// <summary>
/// Dữ liệu 1 mục kiến thức trong Knowledge Archive.
/// Tạo asset: Create -> Audio Game -> Knowledge Entry
/// </summary>
[CreateAssetMenu(fileName = "KnowledgeEntry", menuName = "Audio Game/Knowledge Entry")]
public class KnowledgeEntry : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID duy nhất để unlock từ ChoiceData. Ví dụ: forest_firstaid")]
    public string knowledgeID;

    [Tooltip("Tiêu đề mục kiến thức")]
    public string title;

    [Tooltip("Nhóm nội dung: Forest / Sea / City / Culture")]
    public string category;

    [Header("Preview")]
    [Tooltip("Voice đọc tiêu đề mục này khi mở Archive hoặc khi đổi mục")]
    public AudioClip titleAudio;

    [Header("Content")]
    [Tooltip("Voice narrator đọc nội dung kiến thức")]
    public AudioClip[] contentAudio;
}
