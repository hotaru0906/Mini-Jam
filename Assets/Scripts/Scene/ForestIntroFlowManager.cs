using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flow rung: N1 -> N2 -> N3+N4 -> N5 (QTE, chua lam) -> N6 (ket thuc, mo 2 lua chon).
/// </summary>
public class ForestIntroFlowManager : MonoBehaviour
{
    [System.Serializable]
    public class NarrationLine
    {
        [TextArea]
        public string subtitle;

        [Tooltip("Neu co clip, script se doi clip phat xong roi pause")]
        public AudioClip voiceClip;

        [Tooltip("Pause them sau cau nay")]
        public float pauseAfter = 0.35f;

        [Tooltip("Neu khong co voiceClip, cho tam theo thoi gian nay")]
        public float fallbackDuration = 1.2f;
    }

    [Header("Node Setup")]
    public AudioNode nodeN1;
    public AudioNode nodeN2;
    public AudioNode nodeN3;
    public AudioNode nodeN4; // Duoc kich hoat cung N3
    public AudioNode nodeN5; // Quick Time Event (chua lam)
    public AudioNode nodeN6; // Ket thuc + mo 2 lua chon

    [Header("Narrator")]
    public AudioSource narratorSource;
    public Text subtitleText;

    [Header("N1")]
    public NarrationLine[] n1Lines;

    [Header("N2")]
    public NarrationLine[] n2Lines;

    [Header("N3")]
    public NarrationLine[] n3Lines;

    [Header("N6 - Ket thuc (2 lua chon)")]
    public NarrationLine[] n6Lines;

    [Header("Region Selection (chi 2 object)")]
    public bool lockRegionSelectionUntilN6 = true;
    public GameObject[] regionSelectionObjects;

    // 0: cho N1 | 1: cho N2 | 2: cho N3+N4 | 3: cho N5 (QTE) | 4: cho N6 | 5: xong
    private int _stage;
    private bool _isRunning;
    private PlayerController _playerController;
    private bool _inputLockedByFlow;
    private bool _regionSelectionUnlocked;

    private void Awake()
    {
        if (narratorSource == null)
        {
            narratorSource = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;
            narratorSource.playOnAwake = false;
            narratorSource.volume = 1f;
        }

        if (subtitleText != null)
            subtitleText.text = string.Empty;

        _regionSelectionUnlocked = !lockRegionSelectionUntilN6;
        SetRegionSelectionActive(_regionSelectionUnlocked);
    }

    private void OnEnable()
    {
        PlayerController.OnNodeChanged += OnNodeChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnNodeChanged -= OnNodeChanged;
        UnlockPlayerInput();
    }

    private void Start()
    {
        EnsurePlayerReference();
        if (_playerController != null && _playerController.CurrentNode != null)
            OnNodeChanged(_playerController.CurrentNode);
    }

    private void OnNodeChanged(AudioNode enteredNode)
    {
        if (_isRunning || enteredNode == null)
            return;

        if (_stage == 0 && enteredNode == nodeN1)
            StartCoroutine(RunN1Routine());
        else if (_stage == 1 && enteredNode == nodeN2)
            StartCoroutine(RunN2Routine());
        else if (_stage == 2 && enteredNode == nodeN3)
            StartCoroutine(RunN3Routine());
        else if (_stage == 3 && enteredNode == nodeN5)
            StartCoroutine(RunN5Routine());
        else if (_stage == 4 && enteredNode == nodeN6)
            StartCoroutine(RunN6Routine());
    }

    private IEnumerator RunN1Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n1Lines);
        _stage = 1;
        EndNarrationStage();
    }

    private IEnumerator RunN2Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n2Lines);
        _stage = 2;
        EndNarrationStage();
    }

    private IEnumerator RunN3Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n3Lines);

        // Bat N4 cung luc ket thuc N3
        if (nodeN4 != null)
            nodeN4.gameObject.SetActive(true);

        _stage = 3;
        EndNarrationStage();
    }

    private IEnumerator RunN5Routine()
    {
        // TODO: Quick Time Event
        _stage = 4;
        yield break;
    }

    private IEnumerator RunN6Routine()
    {
        BeginNarrationStage();

        // Phat tung line va mo tung lua chon tuong ung
        if (n6Lines != null)
        {
            for (int i = 0; i < n6Lines.Length; i++)
            {
                yield return PlaySingleLine(n6Lines[i]);

                if (i == 0)
                    RevealRegionObjectAtIndex(0);
                else if (i == 1)
                    RevealRegionObjectAtIndex(1);
            }
        }

        UnlockRegionSelection();
        _stage = 5;
        EndNarrationStage();
    }

    // -------------------------------------------------------
    // Narration helpers
    // -------------------------------------------------------

    private void BeginNarrationStage()
    {
        _isRunning = true;
        LockPlayerInput();
    }

    private void EndNarrationStage()
    {
        UnlockPlayerInput();
        _isRunning = false;
    }

    private IEnumerator PlayNarration(NarrationLine[] lines)
    {
        if (lines == null)
            yield break;

        for (int i = 0; i < lines.Length; i++)
            yield return PlaySingleLine(lines[i]);
    }

    private IEnumerator PlaySingleLine(NarrationLine line)
    {
        if (line == null)
            yield break;

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
    }

    // -------------------------------------------------------
    // Player input helpers
    // -------------------------------------------------------

    private void EnsurePlayerReference()
    {
        if (_playerController == null)
            _playerController = FindFirstObjectByType<PlayerController>();
    }

    private void LockPlayerInput()
    {
        EnsurePlayerReference();
        if (_playerController == null)
            return;

        _playerController.LockInput();
        _inputLockedByFlow = true;
    }

    private void UnlockPlayerInput()
    {
        if (!_inputLockedByFlow)
            return;

        EnsurePlayerReference();
        if (_playerController != null)
            _playerController.UnlockInput();

        _inputLockedByFlow = false;
    }

    // -------------------------------------------------------
    // Region selection helpers
    // -------------------------------------------------------

    private void SetRegionSelectionActive(bool value)
    {
        if (regionSelectionObjects == null)
            return;

        for (int i = 0; i < regionSelectionObjects.Length; i++)
        {
            if (regionSelectionObjects[i] != null)
                regionSelectionObjects[i].SetActive(value);
        }
    }

    private void RevealRegionObjectAtIndex(int index)
    {
        if (regionSelectionObjects == null)
            return;

        if (index < 0 || index >= regionSelectionObjects.Length)
            return;

        if (regionSelectionObjects[index] != null)
            regionSelectionObjects[index].SetActive(true);
    }

    private void UnlockRegionSelection()
    {
        if (_regionSelectionUnlocked)
            return;

        _regionSelectionUnlocked = true;
        SetRegionSelectionActive(true);
    }

    public bool IsRegionSelectionUnlocked => _regionSelectionUnlocked;
}
