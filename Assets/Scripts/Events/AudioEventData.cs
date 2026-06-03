using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ dữ liệu của 1 Audio Event.
/// Tạo asset: chuột phải trong Project → Create → Audio Game → Audio Event Data
/// </summary>
[CreateAssetMenu(fileName = "NewAudioEvent", menuName = "Audio Game/Audio Event Data")]
public class AudioEventData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID duy nhất, dùng để lưu trạng thái đã hoàn thành (ví dụ: forest_cat_rescue)")]
    public string eventID;

    [Header("Audio")]
    [Tooltip("Âm thanh loop khi event chưa được trigger (ví dụ: tiếng mèo kêu liên tục)")]
    public AudioClip ambientSound;

    [Tooltip("Âm thanh phát 1 lần ngay khi player trigger (ví dụ: tiếng mèo thét lên)")]
    public AudioClip triggerSound;

    [Header("Narrator Lines")]
    [Tooltip("Các dòng narrator phát lần lượt khi event bắt đầu (mô tả tình huống)")]
    public AudioClip[] narratorLines;

    [Header("Choices")]
    [Tooltip("Danh sách lựa chọn hiện ra sau khi narrator đọc xong")]
    public ChoiceData[] choices;

    [Header("Interaction")]
    [Tooltip("Khoảng cách player phải đứng để có thể tương tác")]
    public float interactRange = 5f;

    [Tooltip("Góc player phải quay về hướng event (độ) để interact được")]
    [Range(10f, 90f)]
    public float interactAngle = 45f;

}
