using UnityEngine;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// System [4] — Direction Feedback.
/// Mỗi frame tính góc giữa hướng player và hướng tới từng SpatialAudioSource.
/// Nếu player quay đúng hướng → boost volume nguồn âm đó.
/// Nếu quay sai → giảm volume.
/// Gắn script này lên Player GameObject (cùng chỗ với PlayerController).
/// </summary>
public class DirectionFeedback : MonoBehaviour
{
    [Header("Angle Thresholds")]
    [Tooltip("Góc tính từ thẳng trước — nhỏ hơn ngưỡng này coi là 'đúng hướng'")]
    [Range(5f, 60f)]
    public float correctAngle = 30f;

    [Tooltip("Góc lớn hơn ngưỡng này coi là 'sai hướng hoàn toàn'")]
    [Range(60f, 180f)]
    public float wrongAngle = 100f;

    [Header("Volume Multiplier")]
    [Tooltip("Boost volume khi đúng hướng (1 = không boost, 1.3 = +30%)")]
    [Range(1f, 2f)]
    public float correctBoost = 1.25f;

    [Tooltip("Giảm volume khi sai hướng (1 = không giảm, 0.4 = còn 40%)")]
    [Range(0f, 1f)]
    public float wrongPenalty = 0.5f;

    [Header("Gamepad Vibration (tuỳ chọn)")]
    [Tooltip("Rung gamepad khi đúng hướng và đủ gần")]
    public bool useVibration = true;

    [Tooltip("Khoảng cách tối đa để kích hoạt rung (tính theo unit)")]
    public float vibrationMaxDistance = 10f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    // Volume multiplier hiện tại của từng nguồn âm
    // Key: SpatialAudioSource, Value: target multiplier
    private readonly Dictionary<SpatialAudioSource, float> _multipliers
        = new Dictionary<SpatialAudioSource, float>();

    private List<SpatialAudioSource> _allSources = new List<SpatialAudioSource>();

    private float _currentVibration = 0f;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void OnEnable()
    {
        PlayerController.OnNodeChanged   += OnNodeChanged;
        PlayerController.OnFacingChanged += OnFacingChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnNodeChanged   -= OnNodeChanged;
        PlayerController.OnFacingChanged -= OnFacingChanged;
        StopVibration();
    }

    private void Start()
    {
        RefreshSourceList();
        UpdateAllFeedback();
    }

    // -------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------

    private void OnNodeChanged(AudioNode _)   => UpdateAllFeedback();
    private void OnFacingChanged(NodeDirection _) => UpdateAllFeedback();

    // -------------------------------------------------------
    // Core logic
    // -------------------------------------------------------

    private void UpdateAllFeedback()
    {
        // Refresh danh sách nguồn âm (có thể thêm/bỏ khi event kết thúc)
        RefreshSourceList();

        float strongestAlignment = 0f; // 0–1, dùng cho vibration
        float closestDistance    = float.MaxValue;

        foreach (var src in _allSources)
        {
            if (src == null || !src.IsPlaying) continue;

            Vector3 toSource   = src.transform.position - transform.position;
            float   distance   = toSource.magnitude;
            float   angle      = Vector3.Angle(transform.forward, toSource);

            // Tính multiplier từ góc
            float multiplier = CalculateMultiplier(angle);

            // Lưu để apply
            _multipliers[src] = multiplier;
            ApplyMultiplier(src, multiplier);

            // Theo dõi nguồn âm gần + thẳng nhất để vibrate
            float alignment = 1f - Mathf.Clamp01(angle / 180f); // 1 = thẳng, 0 = sau lưng
            if (alignment > strongestAlignment && distance < vibrationMaxDistance)
            {
                strongestAlignment = alignment;
                closestDistance    = distance;
            }
        }

        // Vibration dựa trên nguồn âm thẳng + gần nhất
        if (useVibration)
        {
            float distanceFactor  = closestDistance < vibrationMaxDistance
                ? 1f - (closestDistance / vibrationMaxDistance)
                : 0f;
            float vibrationAmount = strongestAlignment * distanceFactor;
            SetVibration(vibrationAmount);
        }
    }

    /// <summary>
    /// Tính volume multiplier dựa trên góc.
    /// 0°–correctAngle → correctBoost (đúng hướng)
    /// correctAngle–wrongAngle → lerp từ boost xuống penalty
    /// wrongAngle–180° → wrongPenalty (sai hướng)
    /// </summary>
    private float CalculateMultiplier(float angle)
    {
        if (angle <= correctAngle)
            return correctBoost;

        if (angle >= wrongAngle)
            return wrongPenalty;

        // Lerp giữa 2 vùng
        float t = Mathf.InverseLerp(correctAngle, wrongAngle, angle);
        return Mathf.Lerp(correctBoost, wrongPenalty, t);
    }

    private void ApplyMultiplier(SpatialAudioSource src, float multiplier)
    {
        // Lấy AudioSource component trực tiếp để chỉnh volume
        var audioSource = src.GetComponent<AudioSource>();
        if (audioSource == null) return;

        // Target volume = baseVolume * multiplier, clamp 0–1
        float targetVolume = Mathf.Clamp01(src.baseVolume * multiplier);

        // Smooth volume một chút tránh pop đột ngột
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * 8f);
    }

    // -------------------------------------------------------
    // Vibration — chỉ hoạt động nếu có gamepad
    // -------------------------------------------------------

    private void SetVibration(float amount)
    {
#if ENABLE_INPUT_SYSTEM
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // amount: 0–1 → motor speed 0–0.5 (không rung quá mạnh)
        float motorSpeed = amount * 0.5f;
        gamepad.SetMotorSpeeds(motorSpeed, motorSpeed);
        _currentVibration = amount;
#endif
    }

    private void StopVibration()
    {
#if ENABLE_INPUT_SYSTEM
        var gamepad = Gamepad.current;
        if (gamepad != null)
            gamepad.SetMotorSpeeds(0f, 0f);
#endif
        _currentVibration = 0f;
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private void RefreshSourceList()
    {
        _allSources.Clear();
        var found = FindObjectsByType<SpatialAudioSource>(FindObjectsSortMode.None);
        _allSources.AddRange(found);
    }

    // -------------------------------------------------------
    // Gizmos — hiện đường + góc trong Scene View khi selected
    // -------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_allSources == null) return;

        foreach (var src in _allSources)
        {
            if (src == null) continue;

            Vector3 toSource = src.transform.position - transform.position;
            float   angle    = Vector3.Angle(transform.forward, toSource);
            float   mult     = CalculateMultiplier(angle);

            // Màu thay đổi theo mức feedback
            Gizmos.color = Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(wrongPenalty, correctBoost, mult));
            Gizmos.DrawLine(transform.position, src.transform.position);

            UnityEditor.Handles.Label(
                (transform.position + src.transform.position) * 0.5f,
                $"{angle:F0}°  x{mult:F2}"
            );
        }

        // Vẽ cung góc correctAngle và wrongAngle phía trước player
        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.2f);
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up,
            Quaternion.Euler(0, -correctAngle, 0) * transform.forward, correctAngle * 2f, 3f);

        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.1f);
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up,
            Quaternion.Euler(0, -wrongAngle, 0) * transform.forward, wrongAngle * 2f, 5f);
    }
#endif
}
