using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System [8] — Knowledge Archive.
/// - Lắng nghe ChoiceManager.OnChoiceConfirmed để unlock kiến thức.
/// - Mở archive bằng phím K.
/// - Duyệt bằng Up/Down, nghe nội dung bằng Enter/Space.
/// </summary>
public class KnowledgeArchive : MonoBehaviour
{
    public static KnowledgeArchive Instance { get; private set; }

    [Header("Knowledge Database")]
    [Tooltip("Danh sách toàn bộ kiến thức của game")]
    public KnowledgeEntry[] allEntries;

    [Header("Audio")]
    [Tooltip("AudioSource 2D để đọc tiêu đề/nội dung archive")]
    public AudioSource narratorSource;

    [Tooltip("Âm báo khi mở khóa kiến thức mới (optional)")]
    public AudioClip unlockAnnounceClip;

    [Header("Subtitle UI (optional)")]
    public Text subtitleText;

    [Header("Input")]
    public KeyCode openArchiveKey = KeyCode.K;

    [Header("Settings")]
    public float pauseBetweenClips = 0.2f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private Dictionary<string, KnowledgeEntry> _entryById = new Dictionary<string, KnowledgeEntry>();
    private HashSet<string> _unlockedIds = new HashSet<string>();

    private List<KnowledgeEntry> _unlockedList = new List<KnowledgeEntry>();

    private PlayerController _playerController;

    private bool _isArchiveOpen;
    private bool _isReadingContent;
    private int _selectedIndex;

    private Coroutine _voiceRoutine;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (narratorSource == null)
        {
            narratorSource = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;
            narratorSource.playOnAwake = false;
            narratorSource.volume = 1f;
        }

        BuildLookup();
    }

    private void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (subtitleText != null) subtitleText.text = string.Empty;
    }

    private void OnEnable()
    {
        ChoiceManager.OnChoiceConfirmed += OnChoiceConfirmed;
    }

    private void OnDisable()
    {
        ChoiceManager.OnChoiceConfirmed -= OnChoiceConfirmed;
    }

    private void Update()
    {
        if (Input.GetKeyDown(openArchiveKey))
        {
            if (_isArchiveOpen) CloseArchive();
            else OpenArchive();
            return;
        }

        if (!_isArchiveOpen || _isReadingContent)
            return;

        if (_unlockedList.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            ChangeSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            ChangeSelection(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ReadSelectedContent();
            return;
        }
    }

    // -------------------------------------------------------
    // Unlock flow
    // -------------------------------------------------------

    private void OnChoiceConfirmed(AudioEventData _, int __, string knowledgeID)
    {
        if (string.IsNullOrWhiteSpace(knowledgeID))
            return;

        Unlock(knowledgeID);
    }

    public void Unlock(string knowledgeID)
    {
        if (!_entryById.ContainsKey(knowledgeID))
        {
            Debug.LogWarning($"[KnowledgeArchive] Không tìm thấy knowledgeID: {knowledgeID}");
            return;
        }

        if (_unlockedIds.Add(knowledgeID))
        {
            KnowledgeEntry entry = _entryById[knowledgeID];
            _unlockedList.Add(entry);
            Debug.Log($"[KnowledgeArchive] Unlocked: {entry.title} ({entry.knowledgeID})");
            PlayUnlockAnnounce(entry);
        }
    }

    // -------------------------------------------------------
    // Archive open/close
    // -------------------------------------------------------

    public void OpenArchive()
    {
        _isArchiveOpen = true;
        _selectedIndex = 0;

        _playerController?.LockInput();

        if (_unlockedList.Count == 0)
        {
            SetSubtitle("[Archive] Chưa có kiến thức nào được mở khóa.");
            narratorSource.Stop();
            Debug.Log("[KnowledgeArchive] Chưa có mục nào.");
            return;
        }

        AnnounceSelectedEntry();
    }

    public void CloseArchive()
    {
        _isArchiveOpen = false;
        _isReadingContent = false;

        if (_voiceRoutine != null)
        {
            StopCoroutine(_voiceRoutine);
            _voiceRoutine = null;
        }

        narratorSource.Stop();
        SetSubtitle(string.Empty);

        _playerController?.UnlockInput();
    }

    // -------------------------------------------------------
    // Read/announce
    // -------------------------------------------------------

    private void AnnounceSelectedEntry()
    {
        if (_unlockedList.Count == 0) return;

        KnowledgeEntry entry = _unlockedList[_selectedIndex];

        narratorSource.Stop();
        SetSubtitle($"[{_selectedIndex + 1}/{_unlockedList.Count}] {entry.title} ({entry.category})");

        AudioClip previewClip = GetPreviewClip(entry);
        if (previewClip != null)
        {
            narratorSource.clip = previewClip;
            narratorSource.Play();
        }

        Debug.Log($"[Archive] {_selectedIndex + 1}/{_unlockedList.Count}: {entry.title} ({entry.category})");
    }

    private void ChangeSelection(int delta)
    {
        if (_unlockedList.Count <= 1)
            return;

        int nextIndex = (_selectedIndex + delta + _unlockedList.Count) % _unlockedList.Count;
        if (nextIndex == _selectedIndex)
            return;

        _selectedIndex = nextIndex;
        AnnounceSelectedEntry();
    }

    private void ReadSelectedContent()
    {
        if (_unlockedList.Count == 0) return;
        if (_isReadingContent) return;

        KnowledgeEntry entry = _unlockedList[_selectedIndex];
        _voiceRoutine = StartCoroutine(ReadEntryRoutine(entry));
    }

    private IEnumerator ReadEntryRoutine(KnowledgeEntry entry)
    {
        _isReadingContent = true;

        if (entry.contentAudio == null || entry.contentAudio.Length == 0)
        {
            Debug.Log($"[Archive] {entry.title}: chưa có contentAudio.");
            yield return new WaitForSeconds(0.2f);
            _isReadingContent = false;
            _voiceRoutine = null;
            yield break;
        }

        for (int i = 0; i < entry.contentAudio.Length; i++)
        {
            AudioClip clip = entry.contentAudio[i];
            if (clip == null) continue;

            narratorSource.clip = clip;
            narratorSource.Play();

            SetSubtitle($"{entry.title} [{i + 1}/{entry.contentAudio.Length}] - {clip.name}");

            while (narratorSource.isPlaying)
                yield return null;

            yield return new WaitForSeconds(pauseBetweenClips);
        }

        _isReadingContent = false;
        _voiceRoutine = null;

        // Khi đọc xong, quay lại announce title hiện tại
        AnnounceSelectedEntry();
    }

    private void PlayUnlockAnnounce(KnowledgeEntry entry)
    {
        narratorSource.Stop();
        if (unlockAnnounceClip != null)
            narratorSource.PlayOneShot(unlockAnnounceClip, 1f);

        SetSubtitle($"[Unlocked] {entry.title}");
        Debug.Log($"[KnowledgeArchive] Đã mở khóa kiến thức mới: {entry.title}");
    }

    private AudioClip GetPreviewClip(KnowledgeEntry entry)
    {
        if (entry == null) return null;
        if (entry.titleAudio != null) return entry.titleAudio;

        if (entry.contentAudio != null && entry.contentAudio.Length > 0)
            return entry.contentAudio[0];

        return null;
    }

    // -------------------------------------------------------
    // Utilities
    // -------------------------------------------------------

    private void BuildLookup()
    {
        _entryById.Clear();

        if (allEntries == null) return;

        for (int i = 0; i < allEntries.Length; i++)
        {
            KnowledgeEntry entry = allEntries[i];
            if (entry == null) continue;

            if (string.IsNullOrWhiteSpace(entry.knowledgeID))
            {
                Debug.LogWarning($"[KnowledgeArchive] Entry thiếu knowledgeID: {entry.name}");
                continue;
            }

            if (_entryById.ContainsKey(entry.knowledgeID))
            {
                Debug.LogWarning($"[KnowledgeArchive] Trùng knowledgeID: {entry.knowledgeID}");
                continue;
            }

            _entryById.Add(entry.knowledgeID, entry);
        }
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
            subtitleText.text = text;
    }

    // API cho save system phase sau
    public List<string> GetUnlockedIds()
    {
        return new List<string>(_unlockedIds);
    }

    public bool IsUnlocked(string knowledgeID)
    {
        return _unlockedIds.Contains(knowledgeID);
    }

    public void RestoreUnlockedIds(IEnumerable<string> knowledgeIDs)
    {
        if (knowledgeIDs == null)
            return;

        _unlockedIds.Clear();
        _unlockedList.Clear();

        foreach (string knowledgeID in knowledgeIDs)
        {
            if (string.IsNullOrWhiteSpace(knowledgeID))
                continue;

            if (!_entryById.TryGetValue(knowledgeID, out KnowledgeEntry entry))
                continue;

            if (_unlockedIds.Add(knowledgeID))
                _unlockedList.Add(entry);
        }
    }
}
