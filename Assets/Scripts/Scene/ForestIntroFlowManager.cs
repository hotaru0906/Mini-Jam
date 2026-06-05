using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flow rung: moi AudioNode tu phat narration khi player buoc vao, khong phu thuoc thu tu.
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

        [Tooltip("Bat cac GameObject nay truoc khi line nay bat dau phat")]
        public GameObject[] objectsToActivate;

        [Tooltip("Sau khi line phat xong, cho them thoi gian nay roi tat objectsToActivate (0 = khong tat tu dong)")]
        public float deactivateAfter = 0f;
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
    [Header("N4")]
    public NarrationLine[] n4Lines;
    [Header("N5")]
    public NarrationLine[] n5Lines;

    [Header("N6 - Ket thuc (2 lua chon)")]
    public NarrationLine[] n6Lines;

    [Header("Region Selection (chi 2 object)")]
    public bool lockRegionSelectionUntilN6 = true;
    public GameObject[] regionSelectionObjects;

    [Tooltip("Delay truoc cau dau tien cua N1")]
    public float n1StartDelay = 1.5f;

    private bool _isRunning;
    private PlayerController _playerController;
    private bool _inputLockedByFlow;
    private bool _regionSelectionUnlocked;
    private readonly System.Collections.Generic.HashSet<AudioNode> _visited =
        new System.Collections.Generic.HashSet<AudioNode>();

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
        if (_isRunning || enteredNode == null) return;
        if (_visited.Contains(enteredNode)) return;

        if (enteredNode == nodeN1) StartCoroutine(RunN1Routine());
        else if (enteredNode == nodeN2) StartCoroutine(RunNRoutine(n2Lines));
        else if (enteredNode == nodeN3) StartCoroutine(RunNRoutine(n3Lines));
        else if (enteredNode == nodeN4) StartCoroutine(RunNRoutine(n4Lines));
        else if (enteredNode == nodeN5) StartCoroutine(RunNRoutine(n5Lines));
        else if (enteredNode == nodeN6) StartCoroutine(RunN6Routine());
    }

    private IEnumerator RunNRoutine(NarrationLine[] lines)
    {
        MarkVisited(GetCurrentNode());
        BeginNarrationStage();
        yield return PlayNarration(lines);
        EndNarrationStage();
    }

    private IEnumerator RunN1Routine()
    {
        MarkVisited(nodeN1);
        BeginNarrationStage();
        yield return new WaitForSeconds(n1StartDelay);
        yield return PlayNarration(n1Lines);
        EndNarrationStage();
    }

    private IEnumerator RunN2Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n2Lines);
        EndNarrationStage();
    }

    private IEnumerator RunN3Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n3Lines);
        EndNarrationStage();
    }

    private IEnumerator RunN4Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n4Lines);
        EndNarrationStage();
    }
    private IEnumerator RunN5Routine()
    {
        BeginNarrationStage();
        yield return PlayNarration(n5Lines);
        EndNarrationStage();
    }

    private IEnumerator RunN6Routine()
    {
        MarkVisited(nodeN6);
        BeginNarrationStage();

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

    private void MarkVisited(AudioNode node)
    {
        if (node != null) _visited.Add(node);
    }

    private AudioNode GetCurrentNode()
    {
        EnsurePlayerReference();
        return _playerController != null ? _playerController.CurrentNode : null;
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

        // Bat cac GameObject duoc cau hinh cho line nay
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

        // Tat cac object neu co cau hinh deactivateAfter
        if (line.deactivateAfter > 0f && line.objectsToActivate != null)
        {
            yield return new WaitForSeconds(line.deactivateAfter);
            foreach (var obj in line.objectsToActivate)
                if (obj != null) obj.SetActive(false);
        }
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
