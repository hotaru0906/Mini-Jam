using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// System [7] — Choice System.
/// Lắng nghe DialogueManager.OnDialogueEnd, sau đó mở lựa chọn cho player.
/// Điều khiển: Left/Right hoặc A/D để đổi lựa chọn, Enter/Space để confirm,
/// hoặc nhấn số 1/2/3... để chọn trực tiếp.
/// </summary>
public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("AudioSource 2D để đọc lựa chọn + kết quả")]
    public AudioSource narratorSource;

    [Header("Subtitle UI (optional)")]
    public Text subtitleText;

    [Header("Settings")]
    [Tooltip("Khoảng nghỉ giữa các clip")]
    public float pauseBetweenClips = 0.2f;

    // -------------------------------------------------------
    // Events
    // -------------------------------------------------------

    /// <summary>
    /// Gọi khi người chơi đã confirm lựa chọn.
    /// int = choice index, string = knowledgeID
    /// </summary>
    public static event System.Action<AudioEventData, int, string> OnChoiceConfirmed;

    // -------------------------------------------------------
    // Internal state
    // -------------------------------------------------------

    private PlayerController _playerController;

    private AudioEventData _currentEvent;
    private ChoiceData[] _currentChoices;

    private bool _isChoosing;
    private bool _isProcessing;
    private int _selectedIndex;

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
    }

    private void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (subtitleText != null) subtitleText.text = "";
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueEnd += OnDialogueEnd;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEnd -= OnDialogueEnd;
    }

    // -------------------------------------------------------
    // Flow entry
    // -------------------------------------------------------

    private void OnDialogueEnd(AudioEventData eventData)
    {
        if (eventData == null) return;
        if (_isProcessing) return;

        _currentEvent = eventData;
        _currentChoices = eventData.choices;

        // Không có lựa chọn -> complete thẳng
        if (_currentChoices == null || _currentChoices.Length == 0)
        {
            CompleteEventWithoutChoice(eventData);
            return;
        }

        StartCoroutine(ChoiceFlowRoutine());
    }

    // -------------------------------------------------------
    // Choice flow
    // -------------------------------------------------------

    private IEnumerator ChoiceFlowRoutine()
    {
        _isProcessing = true;
        _isChoosing = true;
        _selectedIndex = 0;

        _playerController?.LockInput();

        // Đọc toàn bộ lựa chọn một lượt để người chơi nắm được tất cả option.
        yield return AnnounceAllChoicesOnce();
        Debug.Log("[Choice] Dùng Left/Right hoặc A/D để đổi, Enter/Space để xác nhận, hoặc phím số để chọn nhanh.");

        // Chờ người chơi confirm
        while (_isChoosing)
            yield return null;

        int confirmedIndex = Mathf.Clamp(_selectedIndex, 0, _currentChoices.Length - 1);
        ChoiceData chosen = _currentChoices[confirmedIndex];

        Debug.Log($"[Choice] Confirmed: {confirmedIndex + 1}. {chosen.choiceText}");

        // Phát kết quả sau khi chọn (nếu có)
        if (chosen.resultLines != null && chosen.resultLines.Length > 0)
            yield return PlayLinesSequential(chosen.resultLines);

        // Bắn event cho KnowledgeArchive / SaveSystem ở các phase sau
        OnChoiceConfirmed?.Invoke(_currentEvent, confirmedIndex, chosen.knowledgeID);

        // Hiển thị subtitle ngắn (nếu có)
        if (subtitleText != null)
            subtitleText.text = "";

        _playerController?.UnlockInput();

        _isProcessing = false;
        _currentEvent = null;
        _currentChoices = null;
    }

    private void CompleteEventWithoutChoice(AudioEventData eventData)
    {
        OnChoiceConfirmed?.Invoke(eventData, -1, string.Empty);
        Debug.Log($"[Choice] Event complete không có choice: {eventData.eventID}");
    }

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    private void Update()
    {
        if (!_isChoosing || _currentChoices == null) return;

        // Previous
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            _selectedIndex = (_selectedIndex - 1 + _currentChoices.Length) % _currentChoices.Length;
            StartCoroutine(PlayChoicePreview(_selectedIndex));
            return;
        }

        // Next
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            _selectedIndex = (_selectedIndex + 1) % _currentChoices.Length;
            StartCoroutine(PlayChoicePreview(_selectedIndex));
            return;
        }

        // Confirm
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            _isChoosing = false;
            return;
        }

        // Direct choose by number key
        int direct = ReadDirectChoiceIndex();
        if (direct >= 0 && direct < _currentChoices.Length)
        {
            _selectedIndex = direct;
            _isChoosing = false;
        }
    }

    private int ReadDirectChoiceIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 4;
        return -1;
    }

    // -------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------

    private IEnumerator PlayChoicePreview(int index)
    {
        if (_currentChoices == null || index < 0 || index >= _currentChoices.Length)
            yield break;

        ChoiceData choice = _currentChoices[index];

        // Nếu đang phát preview cũ, dừng để đọc preview mới
        if (narratorSource.isPlaying)
            narratorSource.Stop();

        if (choice.choiceVoice != null)
        {
            narratorSource.clip = choice.choiceVoice;
            narratorSource.Play();

            if (subtitleText != null)
                subtitleText.text = $"[{index + 1}] {choice.choiceText}";

            while (narratorSource.isPlaying)
                yield return null;
        }
        else
        {
            // Fallback: chưa có voice, log text để test flow
            Debug.Log($"[Choice Preview] {index + 1}. {choice.choiceText}");
            if (subtitleText != null)
                subtitleText.text = $"[{index + 1}] {choice.choiceText}";
        }
    }

    private IEnumerator AnnounceAllChoicesOnce()
    {
        if (_currentChoices == null || _currentChoices.Length == 0)
            yield break;

        for (int i = 0; i < _currentChoices.Length; i++)
        {
            ChoiceData choice = _currentChoices[i];

            if (choice.choiceVoice != null)
            {
                narratorSource.clip = choice.choiceVoice;
                narratorSource.Play();

                if (subtitleText != null)
                    subtitleText.text = $"[{i + 1}] {choice.choiceText}";

                while (narratorSource.isPlaying)
                    yield return null;
            }
            else
            {
                Debug.Log($"[Choice Option] {i + 1}. {choice.choiceText}");
                if (subtitleText != null)
                    subtitleText.text = $"[{i + 1}] {choice.choiceText}";
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Sau khi announce xong, focus về lựa chọn đầu để bắt đầu navigate.
        _selectedIndex = 0;
        if (subtitleText != null)
            subtitleText.text = $"[1] {_currentChoices[0].choiceText}";
    }

    private IEnumerator PlayLinesSequential(AudioClip[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            AudioClip clip = lines[i];
            if (clip == null) continue;

            narratorSource.clip = clip;
            narratorSource.Play();

            if (subtitleText != null)
                subtitleText.text = $"[{clip.name}]";

            while (narratorSource.isPlaying)
                yield return null;

            yield return new WaitForSeconds(pauseBetweenClips);
        }
    }
}
