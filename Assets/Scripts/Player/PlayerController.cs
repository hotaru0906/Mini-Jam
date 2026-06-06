using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class FootstepHapticCue
{
    [Min(0f)] public float delay;
    [FormerlySerializedAs("leftMotor")]
    [Range(0f, 1f)] public float leftMotor = 0.2f;
    [FormerlySerializedAs("rightMotor")]
    [Range(0f, 1f)] public float rightMotor = 0.45f;
    [Min(0f)] public float duration = 0.12f;
}

/// <summary>
/// Điều khiển player di chuyển giữa các AudioNode bằng keyboard.
/// Gắn script này lên Player GameObject cùng với AudioListener.
/// </summary>
[RequireComponent(typeof(AudioListener))]
public class PlayerController : MonoBehaviour
{
    [Header("Starting State")]
    [Tooltip("Node bắt đầu — nếu để trống sẽ lấy từ NodeGraph.startNode")]
    public AudioNode startNode;
    public NodeDirection startFacing = NodeDirection.North;

    [Header("Movement")]
    [Tooltip("Thời gian lerp di chuyển tới node kế tiếp (giây)")]
    public float moveTime = 0.15f;

    [Header("Controller Input")]
    [Tooltip("Độ trễ lặp lại khi giữ dpad / left stick")]
    [Min(0.05f)] public float axisRepeatDelay = 0.2f;

    [Header("Player Haptics")]
    public bool enablePlayerHaptics = true;
    [Tooltip("Danh sach nhịp rung cho mỗi footstep. Delay tính từ lúc footstep phát.")]
    public List<FootstepHapticCue> footstepHaptics = new List<FootstepHapticCue>
    {
        new FootstepHapticCue()
    };
    [Tooltip("Lap lai danh sach footstep haptics trong luc clip footstep dang phat")]
    public bool loopFootstepHaptics = true;
    [Tooltip("Cho footstep haptics ket thuc som hon thoi gian move bao nhieu giay")]
    [Min(0f)] public float footstepHapticsEndEarly = 0.1f;
    [Min(0f)] public float footstepHapticsLoopInterval = 0.05f;
    [Range(0f, 1f)] public float blockedHapticsLow = 0.1f;
    [Range(0f, 1f)] public float blockedHapticsHigh = 0.18f;
    [Min(0f)] public float blockedHapticsDuration = 0.08f;
    [Range(0f, 1f)] public float interactHapticsLow = 0.12f;
    [Range(0f, 1f)] public float interactHapticsHigh = 0.3f;
    [Min(0f)] public float interactHapticsDuration = 0.1f;

    [Header("Movement Sound")]
    [Tooltip("AudioSource phát âm thanh bước chân (để trống — script tự tạo)")]
    public AudioSource footstepSource;
    [Tooltip("Danh sách clip bước chân, mỗi bước sẽ chọn ngẫu nhiên")]
    public AudioClip[] footstepClips;
    [Range(0f, 1f)]
    public float footstepVolume = 1f;

    // -------------------------------------------------------
    // State
    // -------------------------------------------------------

    private AudioNode       _currentNode;
    private NodeDirection   _facing;
    private bool            _isMoving;
    private bool            _inputLocked;   // Lock khi đang dialogue/choice
    private float           _nextHorizontalInputTime;
    private float           _nextVerticalInputTime;
    private Coroutine       _footstepHapticsRoutine;
    private AudioSource     _footstepPlaybackSource;
    private bool            _horizontalStickConsumed;
    private bool            _verticalStickConsumed;

    // -------------------------------------------------------
    // Events — các system khác lắng nghe những sự kiện này
    // -------------------------------------------------------

    /// <summary>Phát ra khi player đến node mới.</summary>
    public static event System.Action<AudioNode> OnNodeChanged;

    /// <summary>Phát ra khi player xoay hướng.</summary>
    public static event System.Action<NodeDirection> OnFacingChanged;

    /// <summary>Phát ra khi player nhấn nút Interact (E / Space).</summary>
    public static event System.Action OnInteract;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        EnsureFootstepPlaybackSource();
        ConfigureFootstepSource(_footstepPlaybackSource);
    }

    private void Start()
    {
        // Lấy startNode từ NodeGraph nếu không được gán tay
        if (startNode == null && NodeGraph.Instance != null)
            startNode = NodeGraph.Instance.startNode;

        if (startNode != null)
            TeleportToNode(startNode);
        else
            Debug.LogWarning("[PlayerController] Không tìm thấy startNode!");

        _facing = startFacing;
        ApplyFacingRotation();
    }

    private void Update()
    {
        if (_isMoving || _inputLocked) return;
        HandleInput();
    }

    private void OnDisable()
    {
        CleanupMovementFeedback();
    }

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    private void HandleInput()
    {
        int horizontalInput = ReadHorizontalInput();
        if (horizontalInput < 0)
        {
            TurnLeft();
            return;
        }

        if (horizontalInput > 0)
        {
            TurnRight();
            return;
        }

        int verticalInput = ReadVerticalInput();
        if (verticalInput > 0)
        {
            TryMove(_facing);
            return;
        }

        if (verticalInput < 0)
        {
            TryMove(_facing.Opposite());
            return;
        }

        if (PressedInteract())
        {
            PlayHaptics(interactHapticsLow, interactHapticsHigh, interactHapticsDuration);
            OnInteract?.Invoke();
        }
    }

    // -------------------------------------------------------
    // Movement
    // -------------------------------------------------------

    private void TryMove(NodeDirection direction)
    {
        if (_currentNode == null) return;

        AudioNode neighbor = _currentNode.GetNeighbor(direction);
        if (neighbor == null)
        {
            PlayHaptics(blockedHapticsLow, blockedHapticsHigh, blockedHapticsDuration);
            Debug.Log($"[Player] Không có đường hướng {direction}.");
            return;
        }

        StartCoroutine(MoveToNode(neighbor));
    }

    private void PlayFootstep(float stepDuration)
    {
        if (_footstepPlaybackSource == null || footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) return;

        _footstepPlaybackSource.Stop();
        _footstepPlaybackSource.clip = clip;
        _footstepPlaybackSource.volume = footstepVolume;
        _footstepPlaybackSource.loop = clip.length > 0f && stepDuration > clip.length;
        _footstepPlaybackSource.Play();

        float hapticsDuration = Mathf.Max(0f, stepDuration - footstepHapticsEndEarly);
        ScheduleFootstepHaptics(hapticsDuration);
    }

    private void StopFootstep()
    {
        CleanupMovementFeedback();
    }

    private void CleanupMovementFeedback()
    {
        if (_footstepHapticsRoutine != null)
        {
            StopCoroutine(_footstepHapticsRoutine);
            _footstepHapticsRoutine = null;
        }

        if (_footstepPlaybackSource == null)
        {
            return;
        }

        _footstepPlaybackSource.Stop();
        _footstepPlaybackSource.clip = null;
        _footstepPlaybackSource.loop = false;
    }

    private void EnsureFootstepPlaybackSource()
    {
        if (_footstepPlaybackSource != null)
        {
            return;
        }

        GameObject footstepAudioObject = new GameObject("Footstep Audio");
        footstepAudioObject.transform.SetParent(transform, false);
        _footstepPlaybackSource = footstepAudioObject.AddComponent<AudioSource>();

        if (footstepSource != null)
        {
            _footstepPlaybackSource.outputAudioMixerGroup = footstepSource.outputAudioMixerGroup;
            _footstepPlaybackSource.priority = footstepSource.priority;
            _footstepPlaybackSource.panStereo = footstepSource.panStereo;
            _footstepPlaybackSource.reverbZoneMix = footstepSource.reverbZoneMix;
            _footstepPlaybackSource.rolloffMode = footstepSource.rolloffMode;
            _footstepPlaybackSource.minDistance = footstepSource.minDistance;
            _footstepPlaybackSource.maxDistance = footstepSource.maxDistance;
            _footstepPlaybackSource.dopplerLevel = footstepSource.dopplerLevel;
            _footstepPlaybackSource.spread = footstepSource.spread;
        }

        footstepSource = _footstepPlaybackSource;
    }

    private void ConfigureFootstepSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private IEnumerator MoveToNode(AudioNode targetNode)
    {
        _isMoving = true;
        float stepDuration = Mathf.Max(0f, moveTime);
        PlayFootstep(stepDuration);

        Vector3 from = transform.position;
        Vector3 to   = targetNode.transform.position;
        float elapsed = 0f;

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = stepDuration <= 0f ? 1f : elapsed / stepDuration;
            transform.position = Vector3.Lerp(from, to, normalizedTime);
            yield return null;
        }

        StopFootstep();
        transform.position = to;
        _currentNode = targetNode;
        _isMoving = false;
        _currentNode.TriggerEnterHaptics();

        OnNodeChanged?.Invoke(_currentNode);
        Debug.Log($"[Player] Đến: {_currentNode.gameObject.name}");
    }

    private void TeleportToNode(AudioNode node)
    {
        _currentNode = node;
        transform.position = node.transform.position;
        _currentNode.TriggerEnterHaptics();
        OnNodeChanged?.Invoke(_currentNode);
    }

    // -------------------------------------------------------
    // Input helpers
    // -------------------------------------------------------

    private int ReadHorizontalInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            return -1;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            return 1;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame)
                return -1;
            if (Gamepad.current.dpad.right.wasPressedThisFrame)
                return 1;

            float x = Gamepad.current.leftStick.ReadValue().x;
            if (Mathf.Abs(x) < 0.35f)
            {
                _horizontalStickConsumed = false;
                return 0;
            }

            if (_horizontalStickConsumed)
            {
                return 0;
            }

            if (x < -0.55f)
            {
                _horizontalStickConsumed = true;
                return -1;
            }

            if (x > 0.55f)
            {
                _horizontalStickConsumed = true;
                return 1;
            }
        }
#endif

        return 0;
    }

    private int ReadVerticalInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return 1;

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return -1;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame)
                return 1;
            if (Gamepad.current.dpad.down.wasPressedThisFrame)
                return -1;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                return 1;
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                return -1;

            float y = Gamepad.current.leftStick.ReadValue().y;
            if (Mathf.Abs(y) < 0.35f)
            {
                _verticalStickConsumed = false;
                return 0;
            }

            if (_verticalStickConsumed)
            {
                return 0;
            }

            if (y > 0.55f)
            {
                _verticalStickConsumed = true;
                return 1;
            }

            if (y < -0.55f)
            {
                _verticalStickConsumed = true;
                return -1;
            }
        }
#endif

        return 0;
    }

    private static bool PressedInteract()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && (Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.leftShoulder.wasPressedThisFrame))
            return true;
#endif

        return false;
    }

    private void PlayHaptics(float low, float high, float duration)
    {
        if (!enablePlayerHaptics)
        {
            return;
        }

        GlobalHaptics.Pulse(low, high, duration);
    }

    private void ScheduleFootstepHaptics(float clipLength)
    {
        if (!enablePlayerHaptics)
        {
            return;
        }

        if (_footstepHapticsRoutine != null)
        {
            StopCoroutine(_footstepHapticsRoutine);
        }

        _footstepHapticsRoutine = StartCoroutine(FootstepHapticsRoutine(Mathf.Max(0f, clipLength)));
    }

    private IEnumerator FootstepHapticsRoutine(float clipLength)
    {
        if (footstepHaptics == null || footstepHaptics.Count == 0)
        {
            _footstepHapticsRoutine = null;
            yield break;
        }

        List<FootstepHapticCue> sortedCues = new List<FootstepHapticCue>(footstepHaptics);
        sortedCues.Sort((left, right) => left.delay.CompareTo(right.delay));

        float clipElapsed = 0f;
        bool shouldLoop = loopFootstepHaptics && clipLength > 0f;

        do
        {
            float sequenceElapsed = 0f;
            for (int index = 0; index < sortedCues.Count; index++)
            {
                FootstepHapticCue cue = sortedCues[index];
                if (cue == null)
                {
                    continue;
                }

                float waitTime = Mathf.Max(0f, cue.delay - sequenceElapsed);
                if (waitTime > 0f)
                {
                    if (shouldLoop && clipElapsed + waitTime > clipLength)
                    {
                        _footstepHapticsRoutine = null;
                        yield break;
                    }

                    yield return new WaitForSeconds(waitTime);
                    sequenceElapsed += waitTime;
                    clipElapsed += waitTime;
                }

                PlayHaptics(cue.leftMotor, cue.rightMotor, cue.duration);
                sequenceElapsed = Mathf.Max(sequenceElapsed, cue.delay);
            }

            if (!shouldLoop)
            {
                break;
            }

            float loopWait = Mathf.Max(0f, footstepHapticsLoopInterval);
            if (loopWait <= 0f)
            {
                continue;
            }

            if (clipElapsed + loopWait > clipLength)
            {
                break;
            }

            yield return new WaitForSeconds(loopWait);
            clipElapsed += loopWait;
        }
        while (shouldLoop && clipElapsed < clipLength);

        _footstepHapticsRoutine = null;
    }

    // -------------------------------------------------------
    // Facing / Rotation
    // -------------------------------------------------------

    private void TurnLeft()
    {
        _facing = RotateLeft(_facing);
        ApplyFacingRotation();
        OnFacingChanged?.Invoke(_facing);
        Debug.Log($"[Player] Quay trái → {_facing}");
        TryMove(_facing);
    }

    private void TurnRight()
    {
        _facing = RotateRight(_facing);
        ApplyFacingRotation();
        OnFacingChanged?.Invoke(_facing);
        Debug.Log($"[Player] Quay phải → {_facing}");
        TryMove(_facing);
    }

    private void ApplyFacingRotation()
    {
        transform.rotation = Quaternion.Euler(0f, FacingToAngle(_facing), 0f);
    }

    // -------------------------------------------------------
    // Helpers — static, không cần instance
    // -------------------------------------------------------

    private static NodeDirection RotateLeft(NodeDirection dir)
    {
        switch (dir)
        {
            case NodeDirection.North: return NodeDirection.West;
            case NodeDirection.West:  return NodeDirection.South;
            case NodeDirection.South: return NodeDirection.East;
            case NodeDirection.East:  return NodeDirection.North;
            default:                  return NodeDirection.North;
        }
    }

    private static NodeDirection RotateRight(NodeDirection dir)
    {
        switch (dir)
        {
            case NodeDirection.North: return NodeDirection.East;
            case NodeDirection.East:  return NodeDirection.South;
            case NodeDirection.South: return NodeDirection.West;
            case NodeDirection.West:  return NodeDirection.North;
            default:                  return NodeDirection.North;
        }
    }

    private static float FacingToAngle(NodeDirection dir)
    {
        switch (dir)
        {
            case NodeDirection.North: return 0f;
            case NodeDirection.East:  return 90f;
            case NodeDirection.South: return 180f;
            case NodeDirection.West:  return 270f;
            default:                  return 0f;
        }
    }

    // -------------------------------------------------------
    // Public API — dùng bởi DialogueManager và ChoiceManager
    // -------------------------------------------------------

    public void LockInput()
    {
        _inputLocked = true;
        CleanupMovementFeedback();
    }

    public void UnlockInput() => _inputLocked = false;

    public void SetFacing(NodeDirection dir)
    {
        _facing = dir;
        ApplyFacingRotation();
        OnFacingChanged?.Invoke(_facing);
    }

    public AudioNode      CurrentNode => _currentNode;
    public NodeDirection  Facing      => _facing;
    public bool           IsMoving    => _isMoving;
}
