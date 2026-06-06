using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên GameObject xe.
/// Quản lý 2 loại âm thanh: tiếng máy (loop) và còi xe (định kỳ).
/// </summary>
public class VehicleAudioController : MonoBehaviour
{
    [Header("Engine Sound (loop)")]
    public AudioClip engineClip;
    [Range(0f, 1f)] public float engineVolume = 0.7f;

    [Header("Horn Sound (định kỳ)")]
    public AudioClip hornClip;
    [Range(0f, 1f)] public float hornVolume   = 1f;
    [Tooltip("Cứ mỗi N giây sẽ bấm còi 1 lần")]
    public float hornInterval = 3f;
    [Tooltip("Delay ngẫu nhiên thêm vào interval (tránh các xe còi cùng lúc)")]
    public float hornRandomOffset = 0.5f;

    private AudioSource _engineSource;
    private AudioSource _hornSource;
    private Coroutine   _hornCoroutine;

    private void Awake()
    {
        // Engine source — loop
        _engineSource             = gameObject.AddComponent<AudioSource>();
        _engineSource.clip        = engineClip;
        _engineSource.loop        = true;
        _engineSource.volume      = engineVolume;
        _engineSource.spatialBlend = 1f;   // 3D
        _engineSource.playOnAwake = false;

        // Horn source — one shot
        _hornSource             = gameObject.AddComponent<AudioSource>();
        _hornSource.loop        = false;
        _hornSource.volume      = hornVolume;
        _hornSource.spatialBlend = 1f;
        _hornSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        // Bật engine
        if (engineClip != null)
            _engineSource.Play();

        // Bắt đầu chu kỳ còi
        if (hornClip != null)
            _hornCoroutine = StartCoroutine(HornLoop());
    }

    private void OnDisable()
    {
        _engineSource.Stop();
        _hornSource.Stop();

        if (_hornCoroutine != null)
        {
            StopCoroutine(_hornCoroutine);
            _hornCoroutine = null;
        }
    }

    private IEnumerator HornLoop()
    {
        while (true)
        {
            float wait = hornInterval + Random.Range(0f, hornRandomOffset);
            yield return new WaitForSeconds(wait);
            _hornSource.PlayOneShot(hornClip, hornVolume);
        }
    }
}