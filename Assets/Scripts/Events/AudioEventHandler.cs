using UnityEngine;

/// <summary>
/// Gắn script này lên bất kỳ GameObject nào trong scene để tạo ra 1 Audio Event.
/// Khi player đến gần + quay đúng hướng + nhấn E → event kích hoạt.
/// AudioEventHandler tự quản lý SpatialAudioSource trên cùng GameObject.
/// </summary>
[RequireComponent(typeof(SpatialAudioSource))]
public class AudioEventHandler : MonoBehaviour
{
    [Header("Event Data")]
    public AudioEventData eventData;

    // -------------------------------------------------------
    // Events — DialogueManager và ChoiceManager lắng nghe
    // -------------------------------------------------------

    /// <summary>Phát ra khi player trigger event này. Truyền toàn bộ AudioEventData.</summary>
    public static event System.Action<AudioEventData> OnEventTriggered;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private SpatialAudioSource _audioSource;
    private Transform          _player;
    private bool               _playerInRange  = false;
    private bool               _playerFacing   = false;
    private bool               _isTriggered    = false; // đã trigger trong session này
    private bool               _isCompleted    = false; // hoàn thành trong session này

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        _audioSource = GetComponent<SpatialAudioSource>();
    }

    private void OnEnable()
    {
        // Reset runtime state mỗi lần object enable (ổn định khi bật/tắt Play nhiều lần)
        _playerInRange = false;
        _playerFacing = false;
        _isTriggered = false;
        _isCompleted = false;

        PlayerController.OnInteract += OnPlayerInteract;
        PlayerController.OnNodeChanged += OnNodeChanged;
        PlayerController.OnFacingChanged += OnFacingChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnInteract     -= OnPlayerInteract;
        PlayerController.OnNodeChanged  -= OnNodeChanged;
        PlayerController.OnFacingChanged -= OnFacingChanged;
    }

    private void Start()
    {
        // Tìm player qua tag
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
        else
            Debug.LogWarning($"[AudioEventHandler:{name}] Không tìm thấy GameObject tag 'Player'.");

        // Setup ambient sound từ eventData
        if (eventData != null && eventData.ambientSound != null)
        {
            _audioSource.ambientClip        = eventData.ambientSound;
            _audioSource.playAmbientOnStart = true;
        }

        if (eventData != null && SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventData.eventID))
        {
            MarkCompleted();
        }

    }

    // -------------------------------------------------------
    // Range & Facing checks — gọi lại khi player đổi node/hướng
    // -------------------------------------------------------

    private void OnNodeChanged(AudioNode _)       => CheckProximityAndFacing();
    private void OnFacingChanged(NodeDirection _) => CheckProximityAndFacing();

    private void CheckProximityAndFacing()
    {
        if (_player == null || eventData == null || _isCompleted) return;

        float distance = Vector3.Distance(_player.position, transform.position);
        _playerInRange = distance <= eventData.interactRange;

        if (_playerInRange)
        {
            Vector3 toEvent = transform.position - _player.position;
            float   angle   = Vector3.Angle(_player.forward, toEvent);
            _playerFacing   = angle <= eventData.interactAngle;
        }
        else
        {
            _playerFacing = false;
        }

        // Log trạng thái để debug (sẽ bỏ sau)
        if (_playerInRange && _playerFacing)
            Debug.Log($"[{eventData.eventID}] Trong tầm + đúng hướng — nhấn E để tương tác");
    }

    // -------------------------------------------------------
    // Interact
    // -------------------------------------------------------

    private void OnPlayerInteract()
    {
        if (!_playerInRange || !_playerFacing) return;
        if (eventData == null || _isCompleted || _isTriggered) return;

        _isTriggered = true;

        // Phát trigger sound (1 lần)
        if (eventData.triggerSound != null)
            _audioSource.PlayOneShot(eventData.triggerSound);

        // Dừng ambient (event đã được trigger, không loop nữa)
        StopAmbient();

        // Thông báo cho DialogueManager / ChoiceManager xử lý tiếp
        Debug.Log($"[AudioEventHandler] Triggered: {eventData.eventID}");
        OnEventTriggered?.Invoke(eventData);
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private void StopAmbient()
    {
        _audioSource.FadeOut(0.5f);
    }

    /// <summary>
    /// Gọi từ bên ngoài (DialogueManager) khi dialogue + choice kết thúc hoàn toàn.
    /// </summary>
    public void MarkCompleted()
    {
        _isCompleted = true;
        _isTriggered = true;
        StopAmbient();
    }

    // -------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (eventData == null) return;

        // Vòng tròn interact range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, eventData.interactRange);

        // Label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (eventData.interactRange + 0.3f),
            $"{eventData.eventID}\nRange: {eventData.interactRange}m | Angle: {eventData.interactAngle}°"
        );
    }

    private void OnDrawGizmos()
    {
        if (eventData == null || _isCompleted) return;

        // Chấm vàng nhỏ để nhìn thấy event trong scene
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
#endif
}
