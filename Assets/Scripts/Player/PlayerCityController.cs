using UnityEngine;
using System.Collections;

/// <summary>
/// Điều khiển player trong City Map.
/// A/D/S/W = xoay hướng (KHÔNG di chuyển).
/// Space/E  = xác nhận đi theo hướng đang nhìn.
/// Có thể bị chặn bởi TrafficLightBlocker.
/// </summary>
[RequireComponent(typeof(AudioListener))]
public class PlayerCityController : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector
    // -------------------------------------------------------

    [Header("Starting State")]
    [Tooltip("Node bắt đầu — nếu để trống sẽ lấy từ NodeGraph.startNode")]
    public AudioNode startNode;
    public NodeDirection startFacing = NodeDirection.North;

    [Header("Movement")]
    [Tooltip("Thời gian lerp di chuyển tới node kế tiếp (giây)")]
    public float moveTime = 0.15f;

    [Header("Footstep Sound")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float footstepVolume = 1f;

    [Header("Blocked Sound")]
    [Tooltip("Clip phát khi bị chặn (đèn đỏ, v.v.)")]
    [SerializeField] private AudioClip blockedClip;
    [Range(0f, 1f)] public float blockedVolume = 1f;

    // -------------------------------------------------------
    // State
    // -------------------------------------------------------

    private AudioNode     _currentNode;
    private NodeDirection _facing;
    private bool          _isMoving;
    private bool          _inputLocked;       // DialogueManager / ChoiceManager
    private bool          _movementBlocked;   // TrafficLightBlocker

    // -------------------------------------------------------
    // Events
    // -------------------------------------------------------

    /// <summary>Player đến node mới.</summary>
    public static event System.Action<AudioNode>     OnNodeChanged;

    /// <summary>Player xoay hướng.</summary>
    public static event System.Action<NodeDirection> OnFacingChanged;

    /// <summary>Player nhấn Interact nhưng KHÔNG bị chặn (dùng cho NPC, v.v.).</summary>
    public static event System.Action               OnInteract;

    /// <summary>Player cố đi nhưng bị chặn (đèn đỏ, v.v.).</summary>
    public static event System.Action               OnMovementBlocked;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 0f;
            footstepSource.playOnAwake  = false;
        }
    }

    private void Start()
    {
        if (startNode == null && NodeGraph.Instance != null)
            startNode = NodeGraph.Instance.startNode;

        if (startNode != null)
            TeleportToNode(startNode);
        else
            Debug.LogWarning("[PlayerCityController] Không tìm thấy startNode!");

        _facing = startFacing;
        ApplyFacingRotation();
    }

    private void Update()
    {
        if (_isMoving || _inputLocked) return;
        HandleInput();
    }

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    private void HandleInput()
    {
        // --- Xoay hướng (KHÔNG di chuyển) ---
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SetFacing(RotateLeft(_facing));
            return;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SetFacing(RotateRight(_facing));
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetFacing(_facing.Opposite());
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            // W = "nhìn lại hướng ban đầu" không cần xoay thêm,
            // nhưng nếu muốn reset về North thì dùng dòng dưới.
            // Hiện tại: W không làm gì thêm vì W/S đã đối xứng qua S.
            // → Giữ để mở rộng sau (ví dụ: look forward = về hướng North mặc định).
            return;
        }

        // --- Xác nhận di chuyển ---
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            TryMove(_facing);
        }
    }

    // -------------------------------------------------------
    // Movement
    // -------------------------------------------------------

    private void TryMove(NodeDirection direction)
    {
        if (_currentNode == null) return;

        if (_movementBlocked)
        {
            PlayBlockedSound();
            OnMovementBlocked?.Invoke();
            Debug.Log("[PlayerCity] Di chuyển bị chặn.");
            return;
        }

        AudioNode neighbor = _currentNode.GetNeighbor(direction);
        if (neighbor == null)
        {
            Debug.Log($"[PlayerCity] Không có đường hướng {direction}.");
            return;
        }

        StartCoroutine(MoveToNode(neighbor));
    }

    private IEnumerator MoveToNode(AudioNode targetNode)
    {
        _isMoving = true;
        PlayFootstep();

        Vector3 from    = transform.position;
        Vector3 to      = targetNode.transform.position;
        float   elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / moveTime);
            yield return null;
        }

        transform.position = to;
        _currentNode       = targetNode;
        _isMoving          = false;

        OnNodeChanged?.Invoke(_currentNode);
        Debug.Log($"[PlayerCity] Đến: {_currentNode.gameObject.name}");
    }

    private void TeleportToNode(AudioNode node)
    {
        _currentNode       = node;
        transform.position = node.transform.position;
        OnNodeChanged?.Invoke(_currentNode);
    }

    // -------------------------------------------------------
    // Facing
    // -------------------------------------------------------

    private void SetFacing(NodeDirection dir)
    {
        _facing = dir;
        ApplyFacingRotation();
        OnFacingChanged?.Invoke(_facing);
        Debug.Log($"[PlayerCity] Nhìn hướng: {_facing}");
    }

    private void ApplyFacingRotation()
    {
        transform.rotation = Quaternion.Euler(0f, FacingToAngle(_facing), 0f);
    }

    // -------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null) footstepSource.PlayOneShot(clip, footstepVolume);
    }

    private void PlayBlockedSound()
    {
        if (blockedClip == null) return;
        footstepSource.PlayOneShot(blockedClip, blockedVolume);
    }

    // -------------------------------------------------------
    // Public API — TrafficLightBlocker, DialogueManager, v.v.
    // -------------------------------------------------------

    /// <summary>Chặn di chuyển (gọi bởi TrafficLightBlocker khi đèn đỏ).</summary>
    public void BlockMovement()   => _movementBlocked = true;

    /// <summary>Mở di chuyển (gọi bởi TrafficLightBlocker khi đèn xanh).</summary>
    public void UnblockMovement() => _movementBlocked = false;

    /// <summary>Khoá input (gọi bởi DialogueManager).</summary>
    public void LockInput()       => _inputLocked = true;

    /// <summary>Mở input (gọi bởi DialogueManager).</summary>
    public void UnlockInput()     => _inputLocked = false;

    // Read-only state
    public AudioNode      CurrentNode      => _currentNode;
    public NodeDirection  Facing           => _facing;
    public bool           IsMoving         => _isMoving;
    public bool           IsMovementBlocked => _movementBlocked;

    // -------------------------------------------------------
    // Static helpers
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
}