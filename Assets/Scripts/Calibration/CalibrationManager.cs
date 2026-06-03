using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Giai đoạn 2 — Audio Calibration.
/// Dùng trong CalibrationScene để kiểm tra tai nghe trái/phải.
/// Trả lời bằng A/LeftArrow = trái, D/RightArrow = phải.
/// </summary>
public class CalibrationManager : MonoBehaviour
{
    private enum CalibrationStep
    {
        Idle,
        Intro,
        WaitLeftAnswer,
        WaitRightAnswer,
        Passed,
        Failed
    }

    [Header("Audio")]
    [Tooltip("AudioSource 2D dùng để phát voice và tín hiệu test")]
    public AudioSource narratorSource;

    [Tooltip("Voice mở đầu: Chúng tôi sẽ kiểm tra tai nghe của bạn")]
    public AudioClip introClip;

    [Tooltip("Voice hỏi sau khi phát test trái")]
    public AudioClip askLeftClip;

    [Tooltip("Voice hỏi sau khi phát test phải")]
    public AudioClip askRightClip;

    [Tooltip("Âm test ngắn để phát ở tai trái/phải")]
    public AudioClip testToneClip;

    [Tooltip("Voice khi calibration thành công")]
    public AudioClip successClip;

    [Tooltip("Voice khi calibration thất bại")]
    public AudioClip failureClip;

    [Header("UI (optional)")]
    public Text subtitleText;

    [Header("Flow")]
    [Tooltip("Scene sẽ load sau khi calibration pass. Để trống nếu chưa muốn tự load.")]
    public string nextSceneName = "Hub World";

    [Tooltip("Tự động load scene tiếp theo sau khi pass")]
    public bool autoLoadNextScene = true;

    [Tooltip("Thời gian chờ trước khi load scene tiếp theo")]
    public float delayBeforeNextScene = 0.75f;

    private CalibrationStep _step = CalibrationStep.Idle;
    private bool _isBusy;

    private const string CalibrationPassedKey = "AudioCalibrationPassed";

    private void Awake()
    {
        if (narratorSource == null)
        {
            narratorSource = gameObject.AddComponent<AudioSource>();
            narratorSource.spatialBlend = 0f;
            narratorSource.playOnAwake = false;
            narratorSource.volume = 1f;
        }
    }

    private void Start()
    {
        if (subtitleText != null)
            subtitleText.text = string.Empty;

        StartCoroutine(CalibrationRoutine());
    }

    private void Update()
    {
        if (_isBusy)
            return;

        if (_step == CalibrationStep.WaitLeftAnswer)
        {
            if (PressedLeft())
            {
                StartCoroutine(HandleAnswer(isCorrect: true));
                return;
            }

            if (PressedRight())
            {
                StartCoroutine(HandleAnswer(isCorrect: false));
                return;
            }
        }

        if (_step == CalibrationStep.WaitRightAnswer)
        {
            if (PressedRight())
            {
                StartCoroutine(HandleAnswer(isCorrect: true));
                return;
            }

            if (PressedLeft())
            {
                StartCoroutine(HandleAnswer(isCorrect: false));
                return;
            }
        }

        if (_step == CalibrationStep.Passed && !autoLoadNextScene)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                LoadNextScene();
        }

        if (_step == CalibrationStep.Failed)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                StartCoroutine(CalibrationRoutine());
        }
    }

    private IEnumerator CalibrationRoutine()
    {
        _isBusy = true;
        _step = CalibrationStep.Intro;
        SetSubtitle("Kiểm tra tai nghe trái và phải");

        yield return PlayVoice(introClip, "Chúng tôi sẽ kiểm tra tai nghe của bạn.");

        yield return PlayTestTone(-1f);
        yield return PlayVoice(askLeftClip, "Bạn nghe thấy âm thanh bên nào? Nhấn A cho trái, D cho phải.");

        _step = CalibrationStep.WaitLeftAnswer;
        _isBusy = false;
    }

    private IEnumerator HandleAnswer(bool isCorrect)
    {
        _isBusy = true;

        if (!isCorrect)
        {
            yield return FailCalibration();
            yield break;
        }

        if (_step == CalibrationStep.WaitLeftAnswer)
        {
            yield return PlayTestTone(1f);
            yield return PlayVoice(askRightClip, "Tiếp theo, bạn nghe thấy âm thanh bên nào? Nhấn A cho trái, D cho phải.");

            _step = CalibrationStep.WaitRightAnswer;
            _isBusy = false;
            yield break;
        }

        if (_step == CalibrationStep.WaitRightAnswer)
        {
            _step = CalibrationStep.Passed;
            PlayerPrefs.SetInt(CalibrationPassedKey, 1);
            PlayerPrefs.Save();

            yield return PlayVoice(successClip, "Kiểm tra tai nghe thành công.");

            if (autoLoadNextScene)
            {
                yield return new WaitForSeconds(delayBeforeNextScene);
                LoadNextScene();
            }
            else
            {
                SetSubtitle("Kiểm tra tai nghe thành công. Nhấn Enter để tiếp tục.");
            }
        }

        _isBusy = false;
    }

    private IEnumerator FailCalibration()
    {
        _step = CalibrationStep.Failed;
        PlayerPrefs.SetInt(CalibrationPassedKey, 0);
        PlayerPrefs.Save();

        yield return PlayVoice(failureClip, "Kết quả chưa đúng. Hãy kiểm tra lại tai nghe và nhấn Enter để thử lại.");
        SetSubtitle("Kết quả chưa đúng. Nhấn Enter để thử lại.");
        _isBusy = false;
    }

    private IEnumerator PlayTestTone(float panStereo)
    {
        if (testToneClip == null)
            yield break;

        narratorSource.Stop();
        narratorSource.panStereo = panStereo;
        narratorSource.clip = testToneClip;
        narratorSource.Play();

        while (narratorSource.isPlaying)
            yield return null;

        narratorSource.panStereo = 0f;
    }

    private IEnumerator PlayVoice(AudioClip clip, string fallbackSubtitle)
    {
        SetSubtitle(fallbackSubtitle);

        if (clip == null)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        narratorSource.Stop();
        narratorSource.panStereo = 0f;
        narratorSource.clip = clip;
        narratorSource.Play();

        while (narratorSource.isPlaying)
            yield return null;
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
            return;

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(nextSceneName);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
            subtitleText.text = text;
    }

    private static bool PressedLeft()
    {
        return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
    }

    private static bool PressedRight()
    {
        return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    public static bool HasPassedCalibration()
    {
        return PlayerPrefs.GetInt(CalibrationPassedKey, 0) == 1;
    }
}