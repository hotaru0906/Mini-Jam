using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flow manager cho City Map.
/// N1 → có narration, sau khi xong unlock player di chuyển.
/// N2 trở đi → không có narration, player tự do.
/// </summary>
public class CityIntroFlowManager : MonoBehaviour
{
    [System.Serializable]
    public class NarrationLine
    {
        [TextArea]
        public string subtitle;
        public AudioClip voiceClip;
        public float pauseAfter      = 0.35f;
        public float fallbackDuration = 1.2f;

        [Tooltip("Bật các GameObject này trước khi line bắt đầu phát")]
        public GameObject[] objectsToActivate;

        [Tooltip("Tắt objectsToActivate sau N giây (0 = không tắt tự động)")]
        public float deactivateAfter = 0f;
    }

    // -------------------------------------------------------
    // Inspector
    // -------------------------------------------------------

    [Header("Node N1 (duy nhất có narration)")]
    public AudioNode nodeN1;

    [Header("Narrator")]
    public AudioSource narratorSource;
    public Text        subtitleText;

    [Header("N1 Lines")]
    public NarrationLine[] n1Lines;

    [Tooltip("Delay trước câu đầu tiên của N1")]
    public float n1StartDelay = 1.5f;

    // -------------------------------------------------------
    // State
    // -------------------------------------------------------

    private bool                    _isRunning;
    private bool                    _inputLockedByFlow;
    private bool                    _n1Done;
    private PlayerCityController    _player;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (narratorSource == null)
        {
            narratorSource             = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;
            narratorSource.playOnAwake  = false;
            narratorSource.volume       = 1f;
        }

        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    private void OnEnable()  => PlayerCityController.OnNodeChanged += OnNodeChanged;
    private void OnDisable()
    {
        PlayerCityController.OnNodeChanged -= OnNodeChanged;
        UnlockPlayerInput();
    }

    private void Start()
    {
        EnsurePlayerReference();

        // Khoá player cho đến khi N1 xong
        LockPlayerInput();

        if (_player != null && _player.CurrentNode != null)
            OnNodeChanged(_player.CurrentNode);
    }

    // -------------------------------------------------------
    // Node event
    // -------------------------------------------------------

    private void OnNodeChanged(AudioNode enteredNode)
    {
        if (enteredNode == null) return;

        // N1 chỉ chạy 1 lần
        if (!_n1Done && enteredNode == nodeN1)
        {
            StartCoroutine(RunN1Routine());
            return;
        }

        // N2 trở đi: không làm gì, player tự do
    }

    // -------------------------------------------------------
    // N1 Routine
    // -------------------------------------------------------

    private IEnumerator RunN1Routine()
    {
        _n1Done    = true;
        _isRunning = true;

        yield return new WaitForSeconds(n1StartDelay);
        yield return PlayNarration(n1Lines);

        if (subtitleText != null)
            subtitleText.text = string.Empty;

        _isRunning = false;
        UnlockPlayerInput();   // ← Player được phép di chuyển từ đây
    }

    // -------------------------------------------------------
    // Narration helpers
    // -------------------------------------------------------

    private IEnumerator PlayNarration(NarrationLine[] lines)
    {
        if (lines == null) yield break;
        foreach (var line in lines)
            yield return PlaySingleLine(line);
    }

    private IEnumerator PlaySingleLine(NarrationLine line)
    {
        if (line == null) yield break;

        if (line.objectsToActivate != null)
            foreach (var obj in line.objectsToActivate)
                if (obj != null) obj.SetActive(true);

        if (subtitleText != null)
            subtitleText.text = line.subtitle;

        if (line.voiceClip != null)
        {
            narratorSource.Stop();
            narratorSource.clip = line.voiceClip;
            narratorSource.Play();
            while (narratorSource.isPlaying)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(line.fallbackDuration);
        }

        if (line.pauseAfter > 0f)
            yield return new WaitForSeconds(line.pauseAfter);

        if (line.deactivateAfter > 0f && line.objectsToActivate != null)
        {
            yield return new WaitForSeconds(line.deactivateAfter);
            foreach (var obj in line.objectsToActivate)
                if (obj != null) obj.SetActive(false);
        }
    }

    // -------------------------------------------------------
    // Player helpers
    // -------------------------------------------------------

    private void EnsurePlayerReference()
    {
        if (_player == null)
            _player = FindFirstObjectByType<PlayerCityController>();
    }

    private void LockPlayerInput()
    {
        EnsurePlayerReference();
        if (_player == null) return;
        _player.LockInput();
        _inputLockedByFlow = true;
    }

    private void UnlockPlayerInput()
    {
        if (!_inputLockedByFlow) return;
        EnsurePlayerReference();
        if (_player != null) _player.UnlockInput();
        _inputLockedByFlow = false;
    }
}