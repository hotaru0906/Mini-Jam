using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Main menu cho bản jam.
/// Điều hướng: W/S hoặc Up/Down hoặc DPad/LeftStick.
/// Xác nhận: Enter/Space hoặc gamepad A.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Flow")]
    public string gameplaySceneName = "Hub World";
    public string calibrationSceneName = "Calibration Scene";
    public bool startWithCalibration = true;

    [Header("UI")]
    [Tooltip("Danh sách text theo đúng thứ tự menu")]
    public Text[] optionTexts;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Voice (optional)")]
    public AudioSource narratorSource;
    [Tooltip("Voice đọc tên option theo index")]
    public AudioClip[] optionVoiceClips;

    [Header("Controller Haptics")]
    public bool enableControllerRumble = true;
    [Range(0f, 1f)] public float navigationRumble = 0.08f;
    [Range(0f, 1f)] public float submitRumble = 0.18f;
    public float rumbleDuration = 0.06f;

    [Header("Input Repeat")]
    public float axisRepeatDelay = 0.18f;

    private int _selectedIndex;
    private float _nextAxisInputTime;

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
        _selectedIndex = 0;
        RefreshVisuals();
        AnnounceSelection();
    }

    private void Update()
    {
        int move = ReadMoveInput();
        if (move != 0)
            MoveSelection(move);

        if (PressedSubmit())
            ConfirmSelection();
    }

    private int ReadMoveInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return -1;

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return 1;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame)
                return -1;
            if (Gamepad.current.dpad.down.wasPressedThisFrame)
                return 1;

            if (Time.unscaledTime >= _nextAxisInputTime)
            {
                float y = Gamepad.current.leftStick.ReadValue().y;
                if (y > 0.55f)
                {
                    _nextAxisInputTime = Time.unscaledTime + axisRepeatDelay;
                    return -1;
                }
                if (y < -0.55f)
                {
                    _nextAxisInputTime = Time.unscaledTime + axisRepeatDelay;
                    return 1;
                }
            }
        }
#endif

        return 0;
    }

    private static bool PressedSubmit()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;
#endif

        return false;
    }

    private void MoveSelection(int delta)
    {
        if (optionTexts == null || optionTexts.Length == 0)
            return;

        _selectedIndex = (_selectedIndex + delta + optionTexts.Length) % optionTexts.Length;
        RefreshVisuals();
        AnnounceSelection();
        PulseRumble(navigationRumble);
    }

    private void RefreshVisuals()
    {
        if (optionTexts == null)
            return;

        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] == null) continue;
            optionTexts[i].color = (i == _selectedIndex) ? selectedColor : normalColor;
        }
    }

    private void AnnounceSelection()
    {
        if (optionVoiceClips == null || _selectedIndex < 0 || _selectedIndex >= optionVoiceClips.Length)
            return;

        AudioClip clip = optionVoiceClips[_selectedIndex];
        if (clip == null)
            return;

        narratorSource.Stop();
        narratorSource.clip = clip;
        narratorSource.Play();
    }

    private void ConfirmSelection()
    {
        PulseRumble(submitRumble);

        // Index mặc định:
        // 0 = Start
        // 1 = Exit
        if (_selectedIndex == 0)
        {
            string targetScene = startWithCalibration ? calibrationSceneName : gameplaySceneName;
            LoadScene(targetScene);
            return;
        }

        if (_selectedIndex == 1)
        {
            Application.Quit();
        }
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void PulseRumble(float amount)
    {
#if ENABLE_INPUT_SYSTEM
        if (!enableControllerRumble || Gamepad.current == null)
            return;

        StartCoroutine(RumblePulseRoutine(amount, rumbleDuration));
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private IEnumerator RumblePulseRoutine(float amount, float duration)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
            yield break;

        gamepad.SetMotorSpeeds(amount, amount);
        yield return new WaitForSecondsRealtime(duration);

        if (Gamepad.current == gamepad)
            gamepad.SetMotorSpeeds(0f, 0f);
    }
#endif
}