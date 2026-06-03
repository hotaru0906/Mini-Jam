using System.Collections;
using UnityEngine;

/// <summary>
/// Wrapper cho AudioSource 3D.
/// Gắn script này vào bất kỳ GameObject nào cần phát âm thanh trong không gian.
/// AudioListener phải nằm trên Player (đã setup ở System [2]).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpatialAudioSource : MonoBehaviour
{
    [Header("Audio Clip")]
    public AudioClip ambientClip;       // Âm thanh loop liên tục (tiếng chim, sóng...)
    public AudioClip triggerClip;       // Âm thanh phát 1 lần khi event trigger

    [Header("3D Settings")]
    [Tooltip("Khoảng cách bắt đầu giảm âm lượng (player phải vào gần hơn mức này để nghe rõ)")]
    public float minDistance = 2f;

    [Tooltip("Khoảng cách không còn nghe thấy nữa")]
    public float maxDistance = 20f;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float baseVolume = 1f;

    [Header("Auto Play")]
    [Tooltip("Tự động phát ambientClip khi scene start")]
    public bool playAmbientOnStart = true;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private AudioSource _source;

    private bool EnsureSource()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        return _source != null;
    }

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        if (!EnsureSource())
            return;

        ApplySettings();
    }

    private void Start()
    {
        if (playAmbientOnStart && ambientClip != null)
            PlayAmbient();
    }

    // -------------------------------------------------------
    // Apply 3D settings lên AudioSource
    // -------------------------------------------------------

    private void ApplySettings()
    {
        _source.spatialBlend       = 1f;    // Luôn 3D hoàn toàn, không serialize tránh vô tình đổi
        _source.minDistance        = minDistance;
        _source.maxDistance        = maxDistance;
        _source.volume             = baseVolume;
        _source.rolloffMode        = AudioRolloffMode.Linear; // Linear: về đúng 0 tại maxDistance
        _source.dopplerLevel       = 0f;    // Tắt doppler — game này không cần
        _source.spread             = 0f;    // Âm thanh điểm, không trải rộng
        _source.playOnAwake        = false; // Tắt, ta tự control
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>Phát ambientClip theo vòng lặp.</summary>
    public void PlayAmbient()
    {
        if (!EnsureSource()) return;
        if (ambientClip == null) return;
        _source.clip   = ambientClip;
        _source.loop   = true;
        _source.volume = baseVolume;
        _source.Play();
    }

    /// <summary>Phát triggerClip 1 lần (không ảnh hưởng ambient đang loop).</summary>
    public void PlayTrigger()
    {
        if (!EnsureSource()) return;
        if (triggerClip == null) return;
        _source.PlayOneShot(triggerClip, baseVolume);
    }

    /// <summary>Phát bất kỳ clip nào 1 lần.</summary>
    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (!EnsureSource()) return;
        if (clip == null) return;
        _source.PlayOneShot(clip, baseVolume * volumeScale);
    }

    public void Stop()
    {
        if (!EnsureSource()) return;
        _source.Stop();
    }

    /// <summary>Fade âm lượng về 0 rồi stop.</summary>
    public void FadeOut(float duration = 1f)
    {
        if (!EnsureSource()) return;
        StartCoroutine(FadeRoutine(baseVolume, 0f, duration, stopAfter: true));
    }

    /// <summary>Bắt đầu phát ambient rồi fade in lên baseVolume.</summary>
    public void FadeIn(float duration = 1f)
    {
        if (!EnsureSource()) return;
        if (ambientClip == null) return;
        _source.clip   = ambientClip;
        _source.loop   = true;
        _source.volume = 0f;
        _source.Play();
        StartCoroutine(FadeRoutine(0f, baseVolume, duration, stopAfter: false));
    }

    // -------------------------------------------------------
    // Coroutine
    // -------------------------------------------------------

    private IEnumerator FadeRoutine(float from, float to, float duration, bool stopAfter)
    {
        if (!EnsureSource())
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            _source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _source.volume = to;
        if (stopAfter) _source.Stop();
    }

    // -------------------------------------------------------
    // Properties
    // -------------------------------------------------------

    public bool IsPlaying => _source != null && _source.isPlaying;

    // -------------------------------------------------------
    // Gizmos — hiện vòng tròn MinDistance và MaxDistance trong Scene View
    // -------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Vòng xanh = Min Distance (nghe rõ nhất bên trong)
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, minDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Vòng đỏ = Max Distance (không nghe nữa bên ngoài)
        Gizmos.color = new Color(1f, 0f, 0f, 0.05f);
        Gizmos.DrawSphere(transform.position, maxDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (maxDistance + 0.5f),
            $"Min: {minDistance}m | Max: {maxDistance}m"
        );
    }
#endif
}
