using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pause menu cho gameplay scene.
/// Mở/tắt: Escape hoặc Start/Menu trên gamepad.
/// Điều hướng: W/S, Up/Down, DPad, LeftStick.
/// Xác nhận: Enter/Space hoặc gamepad A.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Text[] optionTexts;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Scene Flow")]
    public string hubSceneName = "SampleScene";
    public string mainMenuSceneName = "MainMenu";

    [Header("Voice (optional)")]
    public AudioSource narratorSource;
    public AudioClip[] optionVoiceClips;

    [Header("Controller Haptics")]
    public bool enableControllerRumble = true;
    [Range(0f, 1f)] public float navigationRumble = 0.08f;
    [Range(0f, 1f)] public float submitRumble = 0.2f;
    public float rumbleDuration = 0.06f;

    [Header("Input Repeat")]
    public float axisRepeatDelay = 0.18f;

    private int _selectedIndex;
    private bool _isPaused;
    private float _nextAxisInputTime;
    private PlayerController _playerController;

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
        _playerController = FindFirstObjectByType<PlayerController>();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (PressedPauseToggle())
        {
            TogglePause();
            return;
        }

        if (!_isPaused)
            return;

        int move = ReadMoveInput();
        if (move != 0)
            MoveSelection(move);

        if (PressedSubmit())
            ConfirmSelection();

        if (PressedCancel())
            Resume();
    }

    private static bool PressedPauseToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            return true;
#endif

        return false;
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

    private static bool PressedCancel()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            return true;
#endif
        return false;
    }

    private void TogglePause()
    {
        if (_isPaused) Resume();
        else OpenPause();
    }

    private void OpenPause()
    {
        _isPaused = true;
        _selectedIndex = 0;

        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        _playerController?.LockInput();
        RefreshVisuals();
        AnnounceSelection();
        PulseRumble(navigationRumble);
    }

    private void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        _playerController?.UnlockInput();
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
        // 0 = Resume
        // 1 = Restart Scene
        // 2 = Return Hub
        // 3 = Main Menu
        if (_selectedIndex == 0)
        {
            Resume();
            return;
        }

        if (_selectedIndex == 1)
        {
            ReloadCurrentScene();
            return;
        }

        if (_selectedIndex == 2)
        {
            LoadScene(hubSceneName);
            return;
        }

        if (_selectedIndex == 3)
        {
            LoadScene(mainMenuSceneName);
        }
    }

    private void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        Time.timeScale = 1f;
        _isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

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