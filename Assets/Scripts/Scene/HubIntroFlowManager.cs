using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dieu khien flow HUB mo dau theo node N1 -> N2 -> N3.
/// - Trigger narrator khi player den node dung thu tu.
/// - Bat 3 tieng vong tai N3.
/// - Mo khoa cua/chon khu vuc sau khi ket thuc intro N3.
/// </summary>
public class HubIntroFlowManager : MonoBehaviour
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

    [Header("Narrator")]
    public AudioSource narratorSource;
    public Text subtitleText;

    [Tooltip("Delay truoc cau dau tien cua N1")]
    public float n1StartDelay = 1.5f;

    [Header("N1 - Thuc tinh")]
    public NarrationLine[] n1Lines;

    [Header("N2 - Gioi thieu Hub")]
    public NarrationLine[] n2Lines;

    [Header("N3 - Gioi thieu 3 tieng vong")]
    public NarrationLine[] n3Lines;

    [Tooltip("Thoi gian lang nghe sau cau 'Hay lang nghe'")]
    public float n3ListenDelay = 2f;

    [Header("3 Tieng Vong (N4 - N6)")]
    [Tooltip("Node N4 (vd: rung). SpatialAudioSource dat o object con cua node")]
    public AudioNode nodeN4;

    [Tooltip("Node N5 (vd: bien). SpatialAudioSource dat o object con cua node")]
    public AudioNode nodeN5;

    [Tooltip("Node N6 (vd: thanh pho). SpatialAudioSource dat o object con cua node")]
    public AudioNode nodeN6;

    public float echoFadeInDuration = 1.1f;

    [Header("Region Selection")]
    [Tooltip("An cac doi tuong nay luc dau, bat lai sau N3")]
    public bool lockRegionSelectionUntilN3 = true;
    public GameObject[] regionSelectionObjects;

    private int _stage; // 0: cho N1, 1: cho N2, 2: cho N3, 3: xong
    private bool _isRunning;
    private PlayerController _playerController;
    private bool _inputLockedByFlow;
    private bool _regionSelectionUnlocked;

    private void Reset()
    {
        ApplyDefaultHubScript();
    }

    [ContextMenu("Apply Default HUB Script")]
    public void ApplyDefaultHubScript()
    {
        n1Lines = new[]
        {
            NewLine("Ban da tinh roi a?", 0.8f, 1.1f),
            NewLine("Ban co nghe thay toi khong?", 0.7f, 1.1f),
            NewLine("Neu co, hay thu buoc ve phia truoc.", 0.4f, 1.2f)
        };

        n2Lines = new[]
        {
            NewLine("Tot lam.", 0.35f, 0.7f),
            NewLine("Ban dang o mot noi dac biet.", 0.7f, 1.2f),
            NewLine("Noi day luu giu nhung am thanh tu nhieu noi khac nhau.", 0.7f, 1.4f),
            NewLine("Moi am thanh deu dan den mot trai nghiem rieng.", 0.7f, 1.3f),
            NewLine("Hay tien them mot chut nua.", 0.4f, 1.1f)
        };

        n3Lines = new[]
        {
            NewLine("Truoc mat ban la ba tieng vong.", 0.7f, 1.2f),
            NewLine("Moi noi mang theo nhung am thanh va cau chuyen khac nhau.", 0.7f, 1.5f),
            NewLine("Hay lang nghe.", 0.2f, 0.8f),
            NewLine("Va chon noi ban muon cam nhan nhat.", 0.3f, 1.3f)
        };

        n1StartDelay = 1.5f;
        n3ListenDelay = 2f;
    }

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

        PrepareEchoNodes();
        PrepareRegionSelection();
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
        {
            StartCoroutine(RunN1Routine());
            return;
        }

        if (_stage == 1 && enteredNode == nodeN2)
        {
            StartCoroutine(RunN2Routine());
            return;
        }

        if (_stage == 2 && enteredNode == nodeN3)
        {
            StartCoroutine(RunN3Routine());
        }
    }

    private IEnumerator RunN1Routine()
    {
        BeginNarrationStage();

        yield return new WaitForSeconds(n1StartDelay);
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

        if (n3Lines != null && n3Lines.Length > 0)
        {
            // N3 line 1
            yield return PlaySingleLine(n3Lines[0]);

            // N3 line 2
            if (n3Lines.Length >= 2)
                yield return PlaySingleLine(n3Lines[1]);

            // N3 line 3: bat N4-N6 + fade in tieng vong, nhung van khoa input
            if (n3Lines.Length >= 3)
            {
                yield return PlaySingleLine(n3Lines[2]);
                EnsureEchoPlayingFromNode(nodeN4);
                EnsureEchoPlayingFromNode(nodeN5);
                EnsureEchoPlayingFromNode(nodeN6);
                yield return new WaitForSeconds(n3ListenDelay);
            }

            // N3 line 4: ket thuc moi mo input
            if (n3Lines.Length >= 4)
                yield return PlaySingleLine(n3Lines[3]);

            // Neu co them line ngoai kịch ban 4 dong, van phat tiep cho an toan
            for (int i = 4; i < n3Lines.Length; i++)
                yield return PlaySingleLine(n3Lines[i]);
        }

        _stage = 3;
        UnlockRegionSelection();
        EndNarrationStage();
    }

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

    private void PrepareEchoNodes()
    {
        // Theo yeu cau hien tai, khong reset hoac restart audio echo luc vao scene.
        // N4-N6 active tu dau va giu nguyen trang thai phat.
    }

    private static NarrationLine NewLine(string subtitle, float pauseAfter, float fallbackDuration)
    {
        return new NarrationLine
        {
            subtitle = subtitle,
            pauseAfter = pauseAfter,
            fallbackDuration = fallbackDuration
        };
    }

    private static void EnsureEchoPlayingFromNode(AudioNode node)
    {
        if (node == null)
            return;

        var echoes = node.GetComponentsInChildren<SpatialAudioSource>(true);
        for (int i = 0; i < echoes.Length; i++)
        {
            if (echoes[i] != null && !echoes[i].IsPlaying)
                echoes[i].PlayAmbient();
        }
    }

    private void PrepareRegionSelection()
    {
        _regionSelectionUnlocked = !lockRegionSelectionUntilN3;
        SetRegionSelectionActive(_regionSelectionUnlocked);
    }

    private void UnlockRegionSelection()
    {
        if (_regionSelectionUnlocked)
            return;

        _regionSelectionUnlocked = true;
        SetRegionSelectionActive(true);
    }

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

    public bool IsRegionSelectionUnlocked => _regionSelectionUnlocked;
}
