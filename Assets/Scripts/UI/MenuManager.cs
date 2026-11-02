using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Needed for the Slider
using TMPro; // Needed for the Button Text
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MenuManager : MonoBehaviour
{
    private const string VolumePreferenceKey = "MasterVolume";
    private const int HighScoreDisplayLimit = 20;
    private const float ScoreColumnWidth = 250f;
    private const float ScoreColumnSpacing = 20f;

    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject creditsPanel;
    public GameObject settingsPanel;
    public GameObject highScoresPanel;

    [Tooltip("Add each level's Unity scene name here to include it in the high scores page.")]
    [SerializeField] private string[] highScoreSceneNames =
        { "Level 1", "Level2", "Level 3" };

    public Slider volumeSlider;

    
    public TMP_Text leftButtonText;
    public TMP_Text rightButtonText;
    public TMP_Text jumpButtonText;
    public TMP_Text slideButtonText;
    public TMP_Text dashButtonText;

    private PlayerAction? pendingRebind;
    private TMP_Text pendingRebindText;
    private RectTransform scoreColumnsContent;
    private TMP_Text highScoresTitle;
    private GameObject confirmationPopup;
    private TMP_Text confirmationMessage;
    private string pendingClearSceneName;
    private bool pendingClearAll;

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
        InitializeHighScoresPanel();
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
        RefreshHighScores();
        mainMenuPanel.SetActive(false);
        highScoresPanel.SetActive(true);
    }

    public void CloseHighScores()
    {
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }
        highScoresPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void InitializeHighScoresPanel()
    {
        if (highScoresPanel == null)
        {
            Debug.LogError("Cannot initialize high scores because the panel is not assigned.");
            return;
        }

        Transform titleTransform = highScoresPanel.transform.Find("TitleText");
        highScoresTitle = titleTransform != null
            ? titleTransform.GetComponent<TMP_Text>()
            : null;
        if (highScoresTitle != null)
        {
            highScoresTitle.text = "High Scores";
            RectTransform titleRect = highScoresTitle.rectTransform;
            titleRect.anchoredPosition = new Vector2(0f, 410f);
            titleRect.sizeDelta = new Vector2(945f, 125f);
        }
        else
        {
            Debug.LogError("The high scores panel is missing its TitleText TMP component.");
        }

        GameObject viewportObject = new GameObject(
            "HighScoresViewport", typeof(RectTransform), typeof(Image), typeof(Mask),
            typeof(ScrollRect));
        viewportObject.layer = highScoresPanel.layer;
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(highScoresPanel.transform, false);
        viewport.anchorMin = new Vector2(0.07f, 0.20f);
        viewport.anchorMax = new Vector2(0.93f, 0.70f);
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("ScoreColumns", typeof(RectTransform));
        contentObject.layer = highScoresPanel.layer;
        scoreColumnsContent = contentObject.GetComponent<RectTransform>();
        scoreColumnsContent.SetParent(viewport, false);
        scoreColumnsContent.anchorMin = new Vector2(0f, 1f);
        scoreColumnsContent.anchorMax = new Vector2(0f, 1f);
        scoreColumnsContent.pivot = new Vector2(0f, 1f);

        ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = scoreColumnsContent;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;

        CreateUiButton(
            "ClearAllHighScoresButton", "Clear All Scores", highScoresPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(0f, 180f),
            new Vector2(300f, 42f), new Color(0.62f, 0.18f, 0.16f, 1f),
            ShowClearAllConfirmation);

        CreateConfirmationPopup();
    }

    private void RefreshHighScores()
    {
        if (scoreColumnsContent == null)
        {
            return;
        }

        for (int i = scoreColumnsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(scoreColumnsContent.GetChild(i).gameObject);
        }

        var sceneNames = new List<string>();
        var seenSceneNames = new HashSet<string>();
        if (highScoreSceneNames != null)
        {
            foreach (string sceneName in highScoreSceneNames)
            {
                if (!string.IsNullOrWhiteSpace(sceneName) && seenSceneNames.Add(sceneName))
                {
                    sceneNames.Add(sceneName);
                }
            }
        }

        float totalColumnsWidth = sceneNames.Count * ScoreColumnWidth +
            Mathf.Max(0, sceneNames.Count - 1) * ScoreColumnSpacing;
        float contentWidth = Mathf.Max(1650f, totalColumnsWidth + 20f);
        scoreColumnsContent.sizeDelta = new Vector2(contentWidth, 530f);
        float startX = Mathf.Max(10f, (contentWidth - totalColumnsWidth) * 0.5f);

        for (int i = 0; i < sceneNames.Count; i++)
        {
            CreateHighScoreColumn(sceneNames[i], startX + i *
                (ScoreColumnWidth + ScoreColumnSpacing));
        }

        ScrollRect scrollRect = scoreColumnsContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private void CreateHighScoreColumn(string sceneName, float xPosition)
    {
        GameObject columnObject = new GameObject(
            sceneName + "Scores", typeof(RectTransform), typeof(Image));
        columnObject.layer = scoreColumnsContent.gameObject.layer;
        RectTransform column = columnObject.GetComponent<RectTransform>();
        column.SetParent(scoreColumnsContent, false);
        column.anchorMin = new Vector2(0f, 1f);
        column.anchorMax = new Vector2(0f, 1f);
        column.pivot = new Vector2(0f, 1f);
        column.anchoredPosition = new Vector2(xPosition, -5f);
        column.sizeDelta = new Vector2(ScoreColumnWidth, 520f);
        columnObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);

        TMP_Text levelTitle = CreateText(
            "LevelTitle", sceneName, column, new Vector2(0f, 1f),
            new Vector2(1f, 1f), Vector2.zero, new Vector2(-16f, -40f),
            25f, TextAlignmentOptions.Center);
        levelTitle.fontStyle = FontStyles.Bold;
        levelTitle.enableAutoSizing = true;
        levelTitle.fontSizeMin = 14f;
        levelTitle.fontSizeMax = 25f;

        string[] scores = GameTimer.RetrieveHighScores(sceneName, HighScoreDisplayLimit);
        string scoreText = scores.Length == 0 ? "No scores yet" : FormatScoreLines(scores);
        CreateText(
            "ScoreList", scoreText, column, new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(12f, -48f),
            new Vector2(-12f, -465f), 17f, TextAlignmentOptions.TopLeft);

        CreateUiButton(
            "ClearLevelScoresButton", "Clear Scores", column,
            new Vector2(0.5f, 0f), new Vector2(0f, 10f),
            new Vector2(220f, 38f), new Color(0.58f, 0.2f, 0.18f, 1f),
            () => ShowClearSceneConfirmation(sceneName));
    }

    private static string FormatScoreLines(string[] scores)
    {
        var lines = new string[scores.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            lines[i] = $"{i + 1}. {scores[i]}";
        }
        return string.Join("\n", lines);
    }

    private TMP_Text CreateText(
        string objectName, string value, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = highScoresTitle != null ? highScoresTitle.font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateUiButton(
        string objectName, string label, Transform parent, Vector2 anchor,
        Vector2 anchoredPosition, Vector2 size, Color color, Action onClick)
    {
        GameObject buttonObject = new GameObject(
            objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick());

        TMP_Text buttonText = CreateText(
            "Label", label, rect, Vector2.zero, Vector2.one,
            new Vector2(6f, 2f), new Vector2(-6f, -2f),
            20f, TextAlignmentOptions.Center);
        buttonText.fontStyle = FontStyles.Bold;
        return button;
    }

    private void CreateConfirmationPopup()
    {
        confirmationPopup = new GameObject(
            "HighScoreConfirmationPopup", typeof(RectTransform), typeof(Image));
        confirmationPopup.layer = highScoresPanel.layer;
        RectTransform popupRect = confirmationPopup.GetComponent<RectTransform>();
        popupRect.SetParent(highScoresPanel.transform, false);
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        confirmationPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        GameObject dialogObject = new GameObject(
            "ConfirmationDialog", typeof(RectTransform), typeof(Image));
        dialogObject.layer = highScoresPanel.layer;
        RectTransform dialog = dialogObject.GetComponent<RectTransform>();
        dialog.SetParent(popupRect, false);
        dialog.anchorMin = new Vector2(0.5f, 0.5f);
        dialog.anchorMax = new Vector2(0.5f, 0.5f);
        dialog.sizeDelta = new Vector2(520f, 230f);
        dialogObject.GetComponent<Image>().color = new Color(0.16f, 0.19f, 0.22f, 1f);

        confirmationMessage = CreateText(
            "ConfirmationMessage", string.Empty, dialog, new Vector2(0.08f, 0.38f),
            new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero,
            25f, TextAlignmentOptions.Center);
        CreateUiButton(
            "ConfirmClearButton", "Clear Scores", dialog,
            new Vector2(0.32f, 0f), new Vector2(0f, 34f),
            new Vector2(190f, 44f), new Color(0.62f, 0.18f, 0.16f, 1f),
            ConfirmClearScores);
        CreateUiButton(
            "CancelClearButton", "Cancel", dialog,
            new Vector2(0.68f, 0f), new Vector2(0f, 34f),
            new Vector2(190f, 44f), new Color(0.3f, 0.34f, 0.38f, 1f),
            CancelClearScores);
        confirmationPopup.SetActive(false);
    }

    private void ShowClearAllConfirmation()
    {
        pendingClearAll = true;
        pendingClearSceneName = null;
        confirmationMessage.text = "Clear all high scores for every level?";
        confirmationPopup.SetActive(true);
    }

    private void ShowClearSceneConfirmation(string sceneName)
    {
        pendingClearAll = false;
        pendingClearSceneName = sceneName;
        confirmationMessage.text = $"Clear all high scores for {sceneName}?";
        confirmationPopup.SetActive(true);
    }

    private void ConfirmClearScores()
    {
        if (pendingClearAll)
        {
            GameTimer.ClearAllHighScores();
        }
        else if (!GameTimer.ClearHighScores(pendingClearSceneName))
        {
            return;
        }

        confirmationPopup.SetActive(false);
        RefreshHighScores();
    }

    private void CancelClearScores()
    {
        pendingClearAll = false;
        pendingClearSceneName = null;
        confirmationPopup.SetActive(false);
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