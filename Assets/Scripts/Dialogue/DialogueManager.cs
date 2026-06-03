using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System [6] — Dialogue Manager (Singleton).
/// Phát voice clip lần lượt, chờ hết clip mới phát câu tiếp theo.
/// Tự động bắt đầu khi AudioEventHandler trigger.
/// Gắn script này lên 1 GameObject duy nhất tên "DialogueManager" trong scene.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("AudioSource riêng cho narrator — tách biệt với 3D audio")]
    public AudioSource narratorSource;

    [Header("Subtitle UI (tuỳ chọn — để trống nếu chưa có UI)")]
    public Text subtitleText;   // Legacy Text, đổi sang TMP_Text nếu dùng TextMeshPro

    [Header("Settings")]
    [Tooltip("Khoảng lặng giữa 2 câu thoại (giây)")]
    public float pauseBetweenLines = 0.3f;

    // -------------------------------------------------------
    // Events
    // -------------------------------------------------------

    /// <summary>Phát ra sau khi toàn bộ dialogue + delay kết thúc, truyền event data.</summary>
    public static event System.Action<AudioEventData> OnDialogueEnd;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private Coroutine          _playRoutine;
    private AudioEventData     _currentEventData;
    private PlayerController   _playerController;
    private bool               _skipRequested;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Tạo narrator AudioSource nếu chưa gán
        if (narratorSource == null)
        {
            narratorSource              = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;   // 2D — narrator không có vị trí không gian
            narratorSource.playOnAwake  = false;
            narratorSource.volume       = 1f;
        }
    }

    private void OnEnable()
    {
        AudioEventHandler.OnEventTriggered += OnEventTriggered;
    }

    private void OnDisable()
    {
        AudioEventHandler.OnEventTriggered -= OnEventTriggered;
    }

    private void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (subtitleText != null) subtitleText.text = "";
    }

    // -------------------------------------------------------
    // Entry point — gọi tự động từ AudioEventHandler
    // -------------------------------------------------------

    private void OnEventTriggered(AudioEventData data)
    {
        _currentEventData = data;

        if (data.narratorLines == null || data.narratorLines.Length == 0)
        {
            // Không có dialogue → chuyển thẳng sang Choice
            OnDialogueEnd?.Invoke(data);
            return;
        }

        Play(data.narratorLines, data);
    }

    /// <summary>Phát mảng clip lần lượt, kết thúc gọi OnDialogueEnd.</summary>
    public void Play(AudioClip[] lines, AudioEventData eventData = null)
    {
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlayRoutine(lines, eventData));
    }

    // -------------------------------------------------------
    // Core coroutine
    // -------------------------------------------------------

    private IEnumerator PlayRoutine(AudioClip[] lines, AudioEventData eventData)
    {
        // Lock input player trong suốt dialogue
        _playerController?.LockInput();

        for (int i = 0; i < lines.Length; i++)
        {
            AudioClip clip = lines[i];
            if (clip == null) continue;

            // Phát clip
            narratorSource.clip = clip;
            narratorSource.Play();

            // Subtitle placeholder (sẽ replace bằng text thật khi có voice)
            if (subtitleText != null)
                subtitleText.text = $"[{clip.name}]";

            Debug.Log($"[Dialogue] Phát: {clip.name} ({clip.length:F1}s)");

            // Chờ tới khi clip phát xong hoặc người chơi bấm skip.
            while (narratorSource.isPlaying && !_skipRequested)
            {
                yield return null;
            }

            // Nếu skip thì chuyển câu kế tiếp ngay, không chèn khoảng nghỉ.
            bool wasSkipped = _skipRequested;
            _skipRequested = false;
            if (!wasSkipped)
                yield return new WaitForSeconds(pauseBetweenLines);
        }

        // Xóa subtitle
        if (subtitleText != null) subtitleText.text = "";

        // Mở input lại
        _playerController?.UnlockInput();

        _playRoutine = null;

        Debug.Log($"[Dialogue] Kết thúc — event: {eventData?.eventID}");
        OnDialogueEnd?.Invoke(eventData);
    }

    // -------------------------------------------------------
    // Skip — nhấn Space trong lúc dialogue (optional)
    // -------------------------------------------------------

    private void Update()
    {
        // Skip câu hiện tại khi nhấn Space trong lúc dialogue đang chạy
        if (_playRoutine != null && Input.GetKeyDown(KeyCode.Space))
        {
            SkipCurrent();
        }
    }

    /// <summary>Cắt ngắn clip hiện tại, chuyển sang câu tiếp theo ngay.</summary>
    public void SkipCurrent()
    {
        if (narratorSource.isPlaying)
        {
            _skipRequested = true;
            narratorSource.Stop();
        }
    }

    /// <summary>Dừng toàn bộ dialogue ngay lập tức và mở input.</summary>
    public void StopAll()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
        _skipRequested = false;
        narratorSource.Stop();
        if (subtitleText != null) subtitleText.text = "";
        _playerController?.UnlockInput();
    }

    // -------------------------------------------------------
    // Tiện ích — phát 1 clip đơn lẻ không liên quan event
    // -------------------------------------------------------

    public void PlaySingle(AudioClip clip, System.Action onComplete = null)
    {
        if (clip == null) { onComplete?.Invoke(); return; }
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlaySingleRoutine(clip, onComplete));
    }

    private IEnumerator PlaySingleRoutine(AudioClip clip, System.Action onComplete)
    {
        narratorSource.clip = clip;
        narratorSource.Play();
        yield return new WaitForSeconds(clip.length + pauseBetweenLines);
        _playRoutine = null;
        onComplete?.Invoke();
    }
}
