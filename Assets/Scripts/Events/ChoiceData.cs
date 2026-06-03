using UnityEngine;

/// <summary>
/// Dữ liệu 1 lựa chọn trong event.
/// Được nhúng trong AudioEventData, không phải ScriptableObject riêng.
/// (Full ChoiceSystem sẽ mở rộng ở System [7])
/// </summary>
[System.Serializable]
public class ChoiceData
{
    [Tooltip("Tên hiển thị / narrator đọc cho lựa chọn này")]
    public string choiceText;

    [Tooltip("Voice clip narrator đọc tên lựa chọn (accessibility)")]
    public AudioClip choiceVoice;

    [Tooltip("Các dòng narrator phát sau khi chọn option này")]
    public AudioClip[] resultLines;

    [Tooltip("Kiến thức mở khóa khi chọn option này (để trống nếu không có)")]
    public string knowledgeID;
}
