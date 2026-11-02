using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Needed for the Slider
using TMPro; // Needed for the Button Text
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MenuManager : MonoBehaviour
{
    private const string VolumePreferenceKey = "MasterVolume";

    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject creditsPanel;
    public GameObject settingsPanel;
    public GameObject highScoresPanel;


    public Slider volumeSlider;

    
    public TMP_Text leftButtonText;
    public TMP_Text rightButtonText;
    public TMP_Text jumpButtonText;
    public TMP_Text slideButtonText;
    public TMP_Text dashButtonText;

    private PlayerAction? pendingRebind;
    private TMP_Text pendingRebindText;

    private void Start()
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePreferenceKey, 1f));
        AudioListener.volume = savedVolume;
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }

        PlayerKeybinds.Load();
        UpdateKeybindLabels();
    }

    private void Update()
    {
        if (!pendingRebind.HasValue || Keyboard.current == null)
        {
            return;
        }

        foreach (KeyControl keyControl in Keyboard.current.allKeys)
        {
            if (keyControl.wasPressedThisFrame && keyControl.keyCode != Key.None)
            {
                PlayerKeybinds.SetBinding(pendingRebind.Value, keyControl.keyCode);
                pendingRebind = null;
                pendingRebindText = null;
                UpdateKeybindLabels();
                return;
            }
        }
    }

    public void StartGame()
    {
        // Loads the next scene in the build index
        SceneManager.LoadScene("Level2");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }

    // Turns off the Main Menu and turns on Level Select
    public void OpenLevelSelect()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    // Turns off Level Select and turns on the Main Menu
    public void CloseLevelSelect()
    {
        levelSelectPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // Turns off the Main Menu and turns on Credits
    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    // Turns off Credits and turns on the Main Menu
    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        CancelRebind();
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenHighScores()
    {
        mainMenuPanel.SetActive(false);
        highScoresPanel.SetActive(true);
    }

    public void CloseHighScores()
    {
        highScoresPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void SetVolume()
    {
        if (volumeSlider == null)
        {
            Debug.LogError("Cannot set volume because the volume slider is not assigned.");
            return;
        }

        // AudioListener controls the master volume of the entire game
        float volume = Mathf.Clamp01(volumeSlider.value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePreferenceKey, volume);
        PlayerPrefs.Save();
    }

    public void StartRebindJump()
    {
        BeginRebind(PlayerAction.Jump, jumpButtonText);
    }

    public void StartRebindRight()
    {
        BeginRebind(PlayerAction.MoveRight, rightButtonText);
    }

    public void StartRebindLeft()
    {
        BeginRebind(PlayerAction.MoveLeft, leftButtonText);
    }

    public void StartRebindDash()
    {
        BeginRebind(PlayerAction.Dash, dashButtonText);
    }

    public void StartRebindSlide()
    {
        BeginRebind(PlayerAction.Slide, slideButtonText);
    }

    private void BeginRebind(PlayerAction action, TMP_Text buttonText)
    {
        CancelRebind();
        pendingRebind = action;
        pendingRebindText = buttonText;
        if (pendingRebindText != null)
        {
            pendingRebindText.text = "Press any key...";
        }
    }

    private void CancelRebind()
    {
        if (pendingRebind.HasValue)
        {
            pendingRebind = null;
            pendingRebindText = null;
            UpdateKeybindLabels();
        }
    }

    private void UpdateKeybindLabels()
    {
        SetKeybindLabel(jumpButtonText, PlayerKeybinds.FormatBinding(PlayerKeybinds.Jump));
        SetKeybindLabel(rightButtonText, PlayerKeybinds.FormatBinding(
            PlayerKeybinds.MoveRight, PlayerKeybinds.MoveRightAlternate));
        SetKeybindLabel(leftButtonText, PlayerKeybinds.FormatBinding(
            PlayerKeybinds.MoveLeft, PlayerKeybinds.MoveLeftAlternate));
        SetKeybindLabel(dashButtonText, PlayerKeybinds.FormatBinding(PlayerKeybinds.Dash));
        SetKeybindLabel(slideButtonText, PlayerKeybinds.FormatBinding(PlayerKeybinds.Slide));
    }

    private static void SetKeybindLabel(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }
}