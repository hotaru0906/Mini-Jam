using UnityEngine;
using System.Collections;

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
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 0f;
            footstepSource.playOnAwake = false;
        }
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

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    private void HandleInput()
    {
        // Xoay trái
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TurnLeft();
            return;
        }

        // Xoay phải
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TurnRight();
            return;
        }

        // Di chuyển tới (theo hướng đang nhìn)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryMove(_facing);
            return;
        }

        // Di chuyển lùi (hướng ngược lại)
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            TryMove(_facing.Opposite());
            return;
        }

        // Tương tác
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
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
            Debug.Log($"[Player] Không có đường hướng {direction}.");
            return;
        }

        StartCoroutine(MoveToNode(neighbor));
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) return;
        footstepSource.PlayOneShot(clip, footstepVolume);
    }

    private IEnumerator MoveToNode(AudioNode targetNode)
    {
        _isMoving = true;
        PlayFootstep();

        Vector3 from = transform.position;
        Vector3 to   = targetNode.transform.position;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / moveTime);
            yield return null;
        }

        transform.position = to;
        _currentNode = targetNode;
        _isMoving = false;

        OnNodeChanged?.Invoke(_currentNode);
        Debug.Log($"[Player] Đến: {_currentNode.gameObject.name}");
    }

    private void TeleportToNode(AudioNode node)
    {
        _currentNode = node;
        transform.position = node.transform.position;
        OnNodeChanged?.Invoke(_currentNode);
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

    public void LockInput()   => _inputLocked = true;
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
