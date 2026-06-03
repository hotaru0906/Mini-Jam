using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// System [13] - Ambient Soundscape.
/// Quan ly cac layer ambient theo khu vuc va crossfade khi chuyen vung.
/// Co che ducking de ambient khong at voice/narrator.
/// </summary>
public class AmbientSoundscapeManager : MonoBehaviour
{
    public static AmbientSoundscapeManager Instance { get; private set; }

    public enum AmbientRegion
    {
        None = 0,
        Forest = 1,
        Sea = 2,
        City = 3
    }

    [System.Serializable]
    public class AmbientLayerConfig
    {
        public AmbientRegion region;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 0.7f;

        [Range(0.5f, 1.5f)]
        public float pitch = 1f;
    }

    [Header("Ambient Layers")]
    [Tooltip("Moi region gan 1 clip loop rieng")]
    public AmbientLayerConfig[] layers;

    [Header("Fade")]
    [Tooltip("Thoi gian crossfade giua cac region")]
    public float fadeDuration = 1.25f;

    [Header("Voice Ducking")]
    [Tooltip("Nguon voice dung de kich hoat ducking (optional)")]
    public AudioSource[] duckingSources;

    [Tooltip("Muc volume ambient khi narrator dang noi")]
    [Range(0.1f, 1f)]
    public float duckingMultiplier = 0.45f;

    [Tooltip("Toc do doi volume ducking")]
    public float duckingLerpSpeed = 6f;

    [Header("Debug")]
    public bool logTransitions;

    private readonly Dictionary<AmbientRegion, AudioSource> _sourceMap = new Dictionary<AmbientRegion, AudioSource>();
    private readonly Dictionary<AmbientRegion, float> _baseVolumeMap = new Dictionary<AmbientRegion, float>();

    private AmbientRegion _currentRegion = AmbientRegion.None;
    private float _currentDucking = 1f;
    private float _targetDucking = 1f;
    private Coroutine _transitionRoutine;
    private float _duckingRefreshTimer;

    public AmbientRegion CurrentRegion => _currentRegion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildLayerSources();
    }

    private void Update()
    {
        _duckingRefreshTimer += Time.unscaledDeltaTime;
        if (_duckingRefreshTimer >= 1f)
        {
            _duckingRefreshTimer = 0f;
            RefreshAutoDuckingSources();
        }

        UpdateDuckingTarget();

        _currentDucking = Mathf.Lerp(_currentDucking, _targetDucking, Time.unscaledDeltaTime * duckingLerpSpeed);
        ApplyDuckingToAll();
    }

    public void EnterRegion(AmbientRegion region)
    {
        if (region == AmbientRegion.None)
            return;

        TransitionTo(region);
    }

    public void LeaveRegion(AmbientRegion region)
    {
        if (_currentRegion == region)
            TransitionTo(AmbientRegion.None);
    }

    public void TransitionTo(AmbientRegion targetRegion)
    {
        if (_currentRegion == targetRegion)
            return;

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionRoutine(targetRegion));
    }

    private IEnumerator TransitionRoutine(AmbientRegion targetRegion)
    {
        if (logTransitions)
            Debug.Log($"[Ambient] Transition: {_currentRegion} -> {targetRegion}");

        float duration = Mathf.Max(0.01f, fadeDuration);

        var fromVolumes = new Dictionary<AmbientRegion, float>();
        foreach (var kv in _sourceMap)
        {
            AudioSource src = kv.Value;
            if (src == null) continue;
            fromVolumes[kv.Key] = src.volume;
        }

        if (targetRegion != AmbientRegion.None && _sourceMap.TryGetValue(targetRegion, out AudioSource targetSource) && targetSource != null)
        {
            if (!targetSource.isPlaying)
                targetSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            foreach (var kv in _sourceMap)
            {
                AmbientRegion region = kv.Key;
                AudioSource src = kv.Value;
                if (src == null) continue;

                float start = fromVolumes.TryGetValue(region, out float v) ? v : 0f;
                float target = 0f;

                if (region == targetRegion && _baseVolumeMap.TryGetValue(region, out float baseVolume))
                    target = baseVolume * _currentDucking;

                src.volume = Mathf.Lerp(start, target, t);
            }

            yield return null;
        }

        foreach (var kv in _sourceMap)
        {
            AmbientRegion region = kv.Key;
            AudioSource src = kv.Value;
            if (src == null) continue;

            if (region == targetRegion && _baseVolumeMap.TryGetValue(region, out float baseVolume))
            {
                src.volume = baseVolume * _currentDucking;
            }
            else
            {
                src.volume = 0f;
                if (src.isPlaying)
                    src.Stop();
            }
        }

        _currentRegion = targetRegion;
        _transitionRoutine = null;
    }

    private void BuildLayerSources()
    {
        _sourceMap.Clear();
        _baseVolumeMap.Clear();

        if (layers == null)
            return;

        for (int i = 0; i < layers.Length; i++)
        {
            AmbientLayerConfig config = layers[i];
            if (config == null || config.region == AmbientRegion.None || config.clip == null)
                continue;

            if (_sourceMap.ContainsKey(config.region))
                continue;

            GameObject child = new GameObject($"Ambient_{config.region}");
            child.transform.SetParent(transform, false);

            AudioSource src = child.AddComponent<AudioSource>();
            src.clip = config.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.pitch = config.pitch;
            src.volume = 0f;

            _sourceMap.Add(config.region, src);
            _baseVolumeMap.Add(config.region, Mathf.Clamp01(config.volume));
        }
    }

    private void RefreshAutoDuckingSources()
    {
        // Tu dong tim cac narrator source de giam ambient luc co voice.
        List<AudioSource> autoSources = new List<AudioSource>();

        if (DialogueManager.Instance != null && DialogueManager.Instance.narratorSource != null)
            autoSources.Add(DialogueManager.Instance.narratorSource);

        if (ChoiceManager.Instance != null && ChoiceManager.Instance.narratorSource != null)
            autoSources.Add(ChoiceManager.Instance.narratorSource);

        if (SceneLoader.Instance != null && SceneLoader.Instance.narratorSource != null)
            autoSources.Add(SceneLoader.Instance.narratorSource);

        if (duckingSources != null)
        {
            for (int i = 0; i < duckingSources.Length; i++)
            {
                AudioSource src = duckingSources[i];
                if (src == null) continue;
                if (!autoSources.Contains(src))
                    autoSources.Add(src);
            }
        }

        duckingSources = autoSources.ToArray();
    }

    private void UpdateDuckingTarget()
    {
        bool shouldDuck = false;

        if (duckingSources != null)
        {
            for (int i = 0; i < duckingSources.Length; i++)
            {
                AudioSource src = duckingSources[i];
                if (src != null && src.isPlaying)
                {
                    shouldDuck = true;
                    break;
                }
            }
        }

        _targetDucking = shouldDuck ? duckingMultiplier : 1f;
    }

    private void ApplyDuckingToAll()
    {
        foreach (var kv in _sourceMap)
        {
            AmbientRegion region = kv.Key;
            AudioSource src = kv.Value;
            if (src == null || !_baseVolumeMap.TryGetValue(region, out float baseVolume))
                continue;

            float target = (_currentRegion == region) ? (baseVolume * _currentDucking) : 0f;
            src.volume = Mathf.Lerp(src.volume, target, Time.unscaledDeltaTime * duckingLerpSpeed);
        }
    }
}
