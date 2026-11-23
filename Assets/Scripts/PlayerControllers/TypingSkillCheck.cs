using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


// handles the typing skill check
// presumably called by PlayerBehaviour
public class TypingSkillCheck : MonoBehaviour
{
    private const string DifficultyPreferenceKey = "TypingSkillCheckDifficulty";
    private const string DefaultDifficulty = "Easy";
    private static readonly string[] DifficultyColumns =
    {
        "Beginner", "Easy", "Amateur", "Intermediate", "Advanced",
        "Hard", "Insane", "Extreme", "Impossible"
    };

    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI targetWordLabel;
    [SerializeField] private TextMeshProUGUI inputLabel;

    [SerializeField] private float timeLimit = 3.0f;
    [SerializeField] private float wrongKeyDecrease = 0.1f;
    [SerializeField] private float timeSlowFactor = 0.2f;

    [Header("SFX")]
    [SerializeField] private AudioClip beatMiniGameSFX;
    [SerializeField] private AudioClip failMiniGameSFX;

    public int lastFrame;
    private GameTimer gameTimer;
    private GameObject playerObject;
    private PlayerBehaviour playerBehaviour;

    private string targetWord = "";
    private string currentInput = "";
    private float timeLeft = 0f;
    private bool isActive = false;
    private float originalTimeScale; // probably 1 but just in case it isn't

    private string[] selectedWords = new string[0];

    public static string[] GetDifficultyOptions()
    {
        return (string[])DifficultyColumns.Clone();
    }

    public static string GetSelectedDifficulty()
    {
        string difficulty = PlayerPrefs.GetString(DifficultyPreferenceKey, DefaultDifficulty);
        if (System.Array.IndexOf(DifficultyColumns, difficulty) < 0)
        {
            Debug.LogWarning(
                $"Saved typing difficulty '{difficulty}' is not valid. Using {DefaultDifficulty}.");
            return DefaultDifficulty;
        }

        return difficulty;
    }

    public static bool SetSelectedDifficulty(string difficulty)
    {
        if (System.Array.IndexOf(DifficultyColumns, difficulty) < 0)
        {
            Debug.LogError($"Cannot set an unknown typing difficulty: '{difficulty}'.");
            return false;
        }

        PlayerPrefs.SetString(DifficultyPreferenceKey, difficulty);
        PlayerPrefs.Save();
        return true;
    }

    public bool GetIsActive()
    {
        return isActive;
    }

    public void StartSkillCheck(string word = null)
    {
        // word = "pop";
        if (string.IsNullOrWhiteSpace(word))
        {
            if (selectedWords.Length == 0)
            {
                Debug.LogError(
                    $"No words were found in the {GetSelectedDifficulty()} column of TypingSkillCheckWords.csv.",
                    this);
                return;
            }

            int randomInt = Random.Range(0, selectedWords.Length);
            targetWord = selectedWords[randomInt];
        }
        else
        {
            targetWord = word;
        }

        targetWord = targetWord.ToUpper();
        currentInput = "";
        
        timeLeft = timeLimit;
        isActive = true;

        originalTimeScale = Time.timeScale;
        gameTimer.SetTimeScale(timeSlowFactor);

        timeLabel.gameObject.SetActive(true);
        targetWordLabel.gameObject.SetActive(true);
        inputLabel.gameObject.SetActive(true);

        UpdateUI();

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
    }

    private void OnTextInput(char character)
    {
        if (!isActive) return;
        if (char.IsControl(character)) return; // backspace, enter, etc

        char typedChar = char.ToUpper(character);
        int nextIndex = currentInput.Length;

        // Check if typed char matches target char
        if (nextIndex < targetWord.Length && typedChar == targetWord[nextIndex])
        {
            currentInput += typedChar;
            UpdateUI();

            if (currentInput.Length >= targetWord.Length)
            {
                Complete();
            }
        }
        else // typed the wrong character
        {
            timeLeft -= wrongKeyDecrease;
        }
    }

    private void ProcessTimer()
    {
        if (gameTimer.IsPaused()) return;

        timeLeft -= Time.unscaledDeltaTime;

        float abs = Mathf.Abs(timeLeft);
        int seconds = Mathf.FloorToInt(abs);
        int ms = Mathf.FloorToInt((abs - seconds) * 100f);

        string formattedTime = $"{seconds:D2}:{ms:D2}s";

        if (timeLeft >= 0)
        {
            timeLabel.text = $"<color=green>+{formattedTime}</color>";
        }
        else
        {
            timeLabel.text = $"<color=red>-{formattedTime}</color>";

            if (timeLeft <= -10)
            {
                Complete(false);
            }
        }
    }

    private void UpdateUI()
    {
        if (targetWordLabel != null)
        {
            targetWordLabel.text = targetWord;
        }

        if (inputLabel != null)
        {
            inputLabel.text = currentInput;
        }

        ProcessTimer();
    }

    private void Complete(bool success = true)
    {
        isActive = false;
        lastFrame = Time.frameCount;

        timeLabel.gameObject.SetActive(false);
        targetWordLabel.gameObject.SetActive(false);
        inputLabel.gameObject.SetActive(false);

        gameTimer.SetTimeScale(originalTimeScale);

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
        
        playerBehaviour.EndSkillCheck();

        if (success)
        {
            Debug.Log("passed");
            SFXManager.instance.PlaySFX(beatMiniGameSFX, transform, 1f, UnityEngine.Random.Range(1, 1.5f));
            // add remaining time to the gametimer
            gameTimer.IncreaseTime(timeLeft);
        }
        else
        {
            Debug.Log("failure");
            SFXManager.instance.PlaySFX(failMiniGameSFX, transform, 1f, UnityEngine.Random.Range(1, 1.5f));
            // failed skill check to an unheard degree (the negative value makes the round timer go negative?)
            gameTimer.IncreaseTime(timeLeft);
        }
    }

    // ---- MONOBEHAVIOUR -- \\

    void Start()
    {
        gameTimer = GameTimer.Instance;
        playerObject = GameObject.FindWithTag("Player");
        playerBehaviour = playerObject.GetComponent<PlayerBehaviour>();
    }

    void Awake()
    {
        TextAsset wordsFile = Resources.Load<TextAsset>("TypingSkillCheckWords");
        if (wordsFile == null)
        {
            Debug.LogError("Could not load Resources/TypingSkillCheckWords.csv.", this);
            return;
        }

        string[] rows = wordsFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0)
        {
            Debug.LogError("TypingSkillCheckWords.csv is empty.", this);
            return;
        }

        List<string> headers = ParseCsvRow(rows[0]);
        string difficulty = GetSelectedDifficulty();
        int difficultyColumn = headers.FindIndex(header =>
            header.Trim().TrimStart('\uFEFF').Equals(
                difficulty, System.StringComparison.OrdinalIgnoreCase));
        if (difficultyColumn < 0)
        {
            Debug.LogError(
                $"TypingSkillCheckWords.csv does not contain a {difficulty} column.", this);
            return;
        }

        List<string> words = new List<string>();
        for (int rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            List<string> columns = ParseCsvRow(rows[rowIndex]);
            if (difficultyColumn < columns.Count)
            {
                string candidate = columns[difficultyColumn].Trim();
                if (!string.IsNullOrEmpty(candidate))
                {
                    words.Add(candidate);
                }
            }
        }

        selectedWords = words.ToArray();
        if (selectedWords.Length == 0)
        {
            Debug.LogError(
                $"TypingSkillCheckWords.csv contains no words in the {difficulty} column.", this);
        }
    }

    private static List<string> ParseCsvRow(string row)
    {
        List<string> columns = new List<string>();
        StringBuilder value = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < row.Length; i++)
        {
            char character = row[i];
            if (character == '"')
            {
                if (insideQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                columns.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        columns.Add(value.ToString());
        return columns;
    }

    void Update()
    {
        if (!isActive) return;

        ProcessTimer();
    }

    void OnDisable()
    {
        if (isActive)
        {
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            playerBehaviour.EndSkillCheck();
        }

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
    }
}