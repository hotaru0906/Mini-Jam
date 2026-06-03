using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Giai đoạn 2 — Save System bản đầu.
/// Tự động lưu khi người chơi hoàn thành 1 event/choice.
/// Lưu: event đã hoàn thành, knowledge đã mở khóa, lựa chọn đã chọn.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private SaveData _data = new SaveData();
    private string _savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        Load();
    }

    private void OnEnable()
    {
        ChoiceManager.OnChoiceConfirmed += OnChoiceConfirmed;
    }

    private void OnDisable()
    {
        ChoiceManager.OnChoiceConfirmed -= OnChoiceConfirmed;
    }

    private void Start()
    {
        RestoreKnowledgeArchive();
    }

    private void OnChoiceConfirmed(AudioEventData eventData, int selectedChoiceIndex, string knowledgeID)
    {
        if (eventData != null && !string.IsNullOrWhiteSpace(eventData.eventID))
        {
            AddCompletedEvent(eventData.eventID);
            SetChoice(eventData.eventID, selectedChoiceIndex);
        }

        if (!string.IsNullOrWhiteSpace(knowledgeID))
            AddUnlockedKnowledge(knowledgeID);

        Save();
    }

    public bool IsEventCompleted(string eventID)
    {
        return !string.IsNullOrWhiteSpace(eventID) && _data.completedEventIDs.Contains(eventID);
    }

    public bool IsKnowledgeUnlocked(string knowledgeID)
    {
        return !string.IsNullOrWhiteSpace(knowledgeID) && _data.unlockedKnowledgeIDs.Contains(knowledgeID);
    }

    public int GetSelectedChoiceIndex(string eventID)
    {
        for (int i = 0; i < _data.choiceRecords.Count; i++)
        {
            SaveChoiceRecord record = _data.choiceRecords[i];
            if (record != null && record.eventID == eventID)
                return record.selectedChoiceIndex;
        }
        return -1;
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(_savePath, json);
        Debug.Log($"[SaveManager] Saved: {_savePath}");
    }

    public void Load()
    {
        if (!File.Exists(_savePath))
        {
            _data = new SaveData();
            Debug.Log("[SaveManager] Chưa có file save, tạo dữ liệu mới.");
            return;
        }

        string json = File.ReadAllText(_savePath);
        _data = JsonUtility.FromJson<SaveData>(json);
        if (_data == null)
            _data = new SaveData();

        _data.completedEventIDs ??= new List<string>();
        _data.unlockedKnowledgeIDs ??= new List<string>();
        _data.choiceRecords ??= new List<SaveChoiceRecord>();

        Debug.Log($"[SaveManager] Loaded: {_savePath}");
    }

    public void ResetSave()
    {
        _data = new SaveData();
        if (File.Exists(_savePath))
            File.Delete(_savePath);

        Debug.Log("[SaveManager] Save đã được reset.");
    }

    private void AddCompletedEvent(string eventID)
    {
        if (!_data.completedEventIDs.Contains(eventID))
            _data.completedEventIDs.Add(eventID);
    }

    private void AddUnlockedKnowledge(string knowledgeID)
    {
        if (!_data.unlockedKnowledgeIDs.Contains(knowledgeID))
            _data.unlockedKnowledgeIDs.Add(knowledgeID);
    }

    private void SetChoice(string eventID, int selectedChoiceIndex)
    {
        for (int i = 0; i < _data.choiceRecords.Count; i++)
        {
            SaveChoiceRecord record = _data.choiceRecords[i];
            if (record != null && record.eventID == eventID)
            {
                record.selectedChoiceIndex = selectedChoiceIndex;
                return;
            }
        }

        _data.choiceRecords.Add(new SaveChoiceRecord
        {
            eventID = eventID,
            selectedChoiceIndex = selectedChoiceIndex
        });
    }

    private void RestoreKnowledgeArchive()
    {
        KnowledgeArchive archive = FindFirstObjectByType<KnowledgeArchive>();
        if (archive == null)
            return;

        archive.RestoreUnlockedIds(_data.unlockedKnowledgeIDs);
    }
}
