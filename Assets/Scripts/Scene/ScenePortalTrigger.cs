using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này lên door / portal trong scene.
/// Khi player đến gần và quay đúng hướng, script tự phát audio rồi chuyển scene.
/// </summary>
public class ScenePortalTrigger : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Tên scene đích trong Build Settings")]
    public string targetSceneName;

    [Header("Interact")]
    public float interactRange = 4f;

    [Range(10f, 90f)]
    public float interactAngle = 45f;

    [Header("Transition Audio")]
    [Tooltip("Âm thanh phát trước khi chuyển scene")]
    public AudioClip portalTransitionClip;

    [Tooltip("Chờ thêm sau khi audio kết thúc rồi mới load scene")]
    public float delayAfterAudio = 1f;

    private Transform _player;
    private PlayerController _playerController;
    private HubIntroFlowManager _hubIntroFlowManager;
    private AudioSource _audioSource;
    private bool _playerInRange;
    private bool _playerFacing;
    private bool _isTransitioning;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.playOnAwake = false;
            _audioSource.minDistance = 1f;
            _audioSource.maxDistance = 10f;
        }
    }

    private void OnEnable()
    {
        PlayerController.OnNodeChanged += OnPlayerMoved;
        PlayerController.OnFacingChanged += OnPlayerRotated;
    }

    private void OnDisable()
    {
        PlayerController.OnNodeChanged -= OnPlayerMoved;
        PlayerController.OnFacingChanged -= OnPlayerRotated;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            _player = playerObject.transform;

        _playerController = FindFirstObjectByType<PlayerController>();
        _hubIntroFlowManager = FindFirstObjectByType<HubIntroFlowManager>();
    }

    private void OnPlayerMoved(AudioNode _)
    {
        UpdateState();
    }

    private void OnPlayerRotated(NodeDirection _)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        if (_player == null)
            return;

        float distance = Vector3.Distance(_player.position, transform.position);
        _playerInRange = distance <= interactRange;

        if (_playerInRange)
        {
            Vector3 toPortal = transform.position - _player.position;
            float angle = Vector3.Angle(_player.forward, toPortal);
            _playerFacing = angle <= interactAngle;
        }
        else
        {
            _playerFacing = false;
        }

        if (_playerInRange && _playerFacing && IsPortalUnlocked())
        {
            if (!_isTransitioning)
                StartCoroutine(TransitionRoutine());
        }
    }

    private bool IsPortalUnlocked()
    {
        if (_hubIntroFlowManager == null)
            return true;

        if (!_hubIntroFlowManager.lockRegionSelectionUntilN3)
            return true;

        return _hubIntroFlowManager.IsRegionSelectionUnlocked;
    }

    private IEnumerator TransitionRoutine()
    {
        _isTransitioning = true;
        _playerController?.LockInput();

        if (portalTransitionClip != null)
        {
            _audioSource.Stop();
            _audioSource.clip = portalTransitionClip;
            _audioSource.loop = false;
            _audioSource.Play();

            while (_audioSource.isPlaying)
                yield return null;
        }

        if (delayAfterAudio > 0f)
            yield return new WaitForSeconds(delayAfterAudio);

        if (SceneLoader.Instance == null)
        {
            Debug.LogWarning("[ScenePortal] Không tìm thấy SceneLoader trong scene.");
            _playerController?.UnlockInput();
            _isTransitioning = false;
            yield break;
        }

        SceneLoader.Instance.LoadScene(targetSceneName);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (interactRange + 0.3f),
            $"Portal -> {targetSceneName}\nRange: {interactRange}m | Angle: {interactAngle}°"
        );
    }
#endif
}