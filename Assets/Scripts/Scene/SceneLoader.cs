using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Giai đoạn 2 — Scene Management.
/// Load scene bất đồng bộ, có thể phát voice loading và khóa input player trong lúc chuyển.
/// Gắn lên 1 GameObject duy nhất tên "SceneLoader" ở scene khởi đầu.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("AudioSource 2D để phát voice loading")]
    public AudioSource narratorSource;

    [Tooltip("Clip narrator đọc khi chuyển scene, ví dụ: 'Đang tải...' ")]
    public AudioClip loadingVoiceClip;

    [Header("UI (optional)")]
    [Tooltip("Text loading để hiện trạng thái khi chuyển scene")]
    public Text loadingText;

    [Header("Settings")]
    [Tooltip("Giữ màn hình loading tối thiểu trong từng này giây")]
    public float minimumLoadingTime = 0.35f;

    private bool _isLoading;
    private PlayerController _playerController;

    public static event System.Action<string> OnLoadStarted;
    public static event System.Action<string> OnLoadFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (narratorSource == null)
        {
            narratorSource = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;
            narratorSource.playOnAwake = false;
            narratorSource.volume = 1f;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshPlayerReference();
        SetLoadingText(string.Empty);
    }

    public bool IsLoading => _isLoading;

    public void LoadScene(string sceneName)
    {
        if (_isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Scene name rỗng.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;
        RefreshPlayerReference();
        _playerController?.LockInput();

        OnLoadStarted?.Invoke(sceneName);
        SetLoadingText($"Đang tải {sceneName}...");

        if (loadingVoiceClip != null)
            narratorSource.PlayOneShot(loadingVoiceClip, 1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] Không thể load scene: {sceneName}");
            _isLoading = false;
            _playerController?.UnlockInput();
            SetLoadingText(string.Empty);
            yield break;
        }

        operation.allowSceneActivation = true;

        float elapsed = 0f;
        while (!operation.isDone || elapsed < minimumLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _isLoading = false;
        SetLoadingText(string.Empty);
        OnLoadFinished?.Invoke(sceneName);

        RefreshPlayerReference();
        _playerController?.UnlockInput();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPlayerReference();
        Debug.Log($"[SceneLoader] Loaded scene: {scene.name}");
    }

    private void RefreshPlayerReference()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
    }

    private void SetLoadingText(string text)
    {
        if (loadingText != null)
            loadingText.text = text;
    }
}