using System.Collections;
using UnityEngine;

/// <summary>
/// Gan vao GameObject forest region selection trong Hub.
/// Goi OnForestSelected() khi player chon khu vuc rung:
///   1. Phat selectVoiceClip den het.
///   2. Bat forestIntroObject de bat dau ForestIntroFlowManager.
/// </summary>
public class ForestRegionTransition : MonoBehaviour
{
    [Tooltip("AudioSource de phat clip chuyen scene (co the de trong - script tu tao)")]
    public AudioSource audioSource;

    [Tooltip("Clip voice phat khi player chon rung")]
    public AudioClip selectVoiceClip;

    [Tooltip("GameObject chua ForestIntroFlowManager, tat truoc khi can")]
    public GameObject forestIntroObject;

    private bool _triggered;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.volume = 1f;
        }
    }

    /// <summary>
    /// Goi ham nay tu Button.onClick, AudioNode trigger, hoac bat ky su kien chon rung nao.
    /// </summary>
    public void OnForestSelected()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(PlayThenOpenForestIntro());
    }

    private IEnumerator PlayThenOpenForestIntro()
    {
        if (selectVoiceClip != null)
        {
            audioSource.clip = selectVoiceClip;
            audioSource.Play();
            while (audioSource.isPlaying)
                yield return null;
        }

        if (forestIntroObject != null)
            forestIntroObject.SetActive(true);
    }
}
