using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý chu kỳ giao thông: safe ↔ traffic.
/// Hoạt động đúng khi bật/tắt qua SetActive.
/// Nhiều instance có thể tồn tại trong 1 scene.
/// </summary>
public class TrafficTimerManager : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Thời gian player được đi (giây)")]
    public float safeDuration    = 5f;

    [Tooltip("Thời gian xe chạy — player bị block (giây)")]
    public float trafficDuration = 5f;

    [Tooltip("Delay trước khi chu kỳ đầu tiên bắt đầu")]
    public float initialDelay    = 0f;

    [Header("References")]
    public TrafficVehicleSpawner vehicleSpawner;

    [Header("Debug")]
    public bool logState = true;

    // -------------------------------------------------------
    // Events (static — bất kỳ manager nào fire đều notify)
    // -------------------------------------------------------

    public static event System.Action OnTrafficPhaseStart;
    public static event System.Action OnSafePhaseStart;

    // -------------------------------------------------------
    // State
    // -------------------------------------------------------

    public enum Phase { Safe, Traffic }
    public Phase CurrentPhase { get; private set; } = Phase.Safe;

    private PlayerController _player;
    private Coroutine        _cycleCoroutine;

    // -------------------------------------------------------
    // Lifecycle — dùng OnEnable/OnDisable để SetActive hoạt động
    // -------------------------------------------------------

    private void OnEnable()
    {
        _player = FindFirstObjectByType<PlayerController>();
        _cycleCoroutine = StartCoroutine(RunCycle());

        if (logState) Debug.Log($"[TrafficTimer:{name}] Bắt đầu chu kỳ.");
    }

    private void OnDisable()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }

        // Khi bị tắt, đảm bảo player không bị kẹt block
        if (_player != null) _player.UnlockInput();
        if (vehicleSpawner != null) vehicleSpawner.StopSpawning();

        if (logState) Debug.Log($"[TrafficTimer:{name}] Dừng chu kỳ.");
    }

    // -------------------------------------------------------
    // Cycle
    // -------------------------------------------------------

    private IEnumerator RunCycle()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            EnterSafePhase();
            yield return new WaitForSeconds(safeDuration);

            EnterTrafficPhase();
            yield return new WaitForSeconds(trafficDuration);
        }
    }

    private void EnterSafePhase()
    {
        CurrentPhase = Phase.Safe;

        if (_player != null)     _player.UnlockInput();
        if (vehicleSpawner != null) vehicleSpawner.StopSpawning();

        OnSafePhaseStart?.Invoke();

        if (logState) Debug.Log($"[TrafficTimer:{name}] ✅ AN TOÀN.");
    }

    private void EnterTrafficPhase()
    {
        CurrentPhase = Phase.Traffic;

        if (_player != null)     _player.LockInput();
        if (vehicleSpawner != null) vehicleSpawner.StartSpawning();

        OnTrafficPhaseStart?.Invoke();

        if (logState) Debug.Log($"[TrafficTimer:{name}] 🚗 XE CHẠY.");
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    public void ForceSafePhase()
    {
        if (_cycleCoroutine != null) StopCoroutine(_cycleCoroutine);
        EnterSafePhase();
    }

    public void ForceTrafficPhase()
    {
        if (_cycleCoroutine != null) StopCoroutine(_cycleCoroutine);
        EnterTrafficPhase();
    }

    public void RestartCycle()
    {
        if (_cycleCoroutine != null) StopCoroutine(_cycleCoroutine);
        _cycleCoroutine = StartCoroutine(RunCycle());
    }
}