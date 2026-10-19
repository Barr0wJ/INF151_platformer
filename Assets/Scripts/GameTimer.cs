using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    private const int HighScoreLimit = 20;
    private const string HighScoresFileName = "highscores.csv";

    public static GameTimer Instance { get; private set; } // allows GameTimer.Instance
    
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private float time = 100; // seconds

    private bool gameRunning = true;
    private bool gameEnd = false; // we are sooooo inefficient
    private float timeScale = 1; // way to keep track during pause menu

    public bool IsPaused()
    {
        return !gameRunning;
    }

    public float GetTime()
    {
        return time;
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // default is 0.02
        timeScale = scale;
    }

    // Returns in format: ss:ms
    public string GetClockTime()
    {
        float abs = Mathf.Abs(time);
        int seconds = Mathf.FloorToInt(abs);
        int ms = Mathf.FloorToInt((abs - seconds) * 100f);

        string formattedTime = $"{seconds:D2}:{ms:D2}";
        return time < 0f ? $"-{formattedTime}" : formattedTime;
    }

    public void IncreaseTime(float value)
    {
        time += value;
    }

    public void DecreaseTime(float value)
    {
        time -= value;
    }


    // ---- game menu ---- \\

    public void HandlePause()
    {

        if (!gameRunning)
        {
            // disable pause ui
            pauseMenu.SetActive(false);

            // countdown probably
            SetTimeScale(timeScale);
        } else
        {
            // enable pause ui
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
            Time.fixedDeltaTime = 0;
        }

        gameRunning = !gameRunning;
    }

    private void ProcessRoundTimer()
    {
        if (!gameRunning) return;
        if (gameEnd) return;

        time -= Time.deltaTime;
        timerLabel.text = GetClockTime();

        if (time <= 0)
        {
            EndGame();
        }
    }

    public void EndGame(bool passed = false)
    {
        gameEnd = true;
        string[] previousHighScores = RetrieveHighScores();
        bool isNewHighScore = passed &&
            (previousHighScores.Length == 0 ||
             (TryParseClockTime(previousHighScores[0], out long previousBest) &&
              TryParseClockTime(GetClockTime(), out long finalTime) &&
              finalTime > previousBest));

        UpdateHighScores(passed);
        string[] highScores = RetrieveHighScores();
        string highScoresText = FormatHighScores(highScores);

        if (passed)
        {
            Debug.Log("Passed!");
            timerLabel.text = "You win! Final Time: " + GetClockTime() +
                (isNewHighScore ? "\nNew High Score!" : string.Empty) +
                "\nTop Scores:\n" + highScoresText;
        } 
        else
        {
            Debug.Log("Ran out of time");
            timerLabel.text = "Ran out of time\nTop Scores:\n" + highScoresText;
        }

    }

    public string[] RetrieveHighScores()
    {
        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        if (!File.Exists(path))
        {
            return new string[0];
        }

        var highScores = new List<KeyValuePair<string, long>>();

        foreach (string line in File.ReadAllLines(path))
        {
            string clockTime = line.Trim();
            if (clockTime.Length == 0 || clockTime == "Time")
            {
                continue;
            }

            if (TryParseClockTime(clockTime, out long centiseconds))
            {
                highScores.Add(new KeyValuePair<string, long>(clockTime, centiseconds));
            }
            else
            {
                Debug.LogWarning($"Skipping invalid high score '{clockTime}' in {path}");
            }
        }

        highScores.Sort((left, right) => right.Value.CompareTo(left.Value));
        int scoreCount = Mathf.Min(3, highScores.Count);
        var topScores = new string[scoreCount];
        for (int i = 0; i < scoreCount; i++)
        {
            topScores[i] = highScores[i].Key;
        }

        return topScores;
    }

    private static string FormatHighScores(string[] highScores)
    {
        if (highScores.Length == 0)
        {
            return "No high scores yet";
        }

        var lines = new string[highScores.Length];
        for (int i = 0; i < highScores.Length; i++)
        {
            lines[i] = $"{i + 1}. {highScores[i]}";
        }

        return string.Join("\n", lines);
    }

    private void UpdateHighScores(bool passed, int level = -1)
    {
        // eventually add different high scores for each level and one overall, 
        // but for now just one high score list

        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        Directory.CreateDirectory(Application.persistentDataPath);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Time" + System.Environment.NewLine);
        }

        if (!passed)
        {
            return;
        }

        var highScores = new List<KeyValuePair<string, long>>();
        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            string clockTime = line.Trim();
            if (clockTime.Length == 0 || clockTime == "Time")
            {
                continue;
            }

            if (TryParseClockTime(clockTime, out long centiseconds))
            {
                highScores.Add(new KeyValuePair<string, long>(clockTime, centiseconds));
            }
            else
            {
                Debug.LogWarning($"Skipping invalid high score '{clockTime}' in {path}");
            }
        }

        string currentTime = GetClockTime();
        if (!TryParseClockTime(currentTime, out long currentCentiseconds))
        {
            Debug.LogError($"Could not parse current high score '{currentTime}'");
            return;
        }

        highScores.Add(new KeyValuePair<string, long>(currentTime, currentCentiseconds));
        highScores.Sort((left, right) => right.Value.CompareTo(left.Value));

        if (highScores.Count > HighScoreLimit)
        {
            highScores.RemoveRange(HighScoreLimit, highScores.Count - HighScoreLimit);
        }

        var outputLines = new List<string> { "Time" };
        foreach (KeyValuePair<string, long> highScore in highScores)
        {
            outputLines.Add(highScore.Key);
        }

        File.WriteAllLines(path, outputLines);
    }

    private static bool TryParseClockTime(string clockTime, out long centiseconds)
    {
        centiseconds = 0;
        int separator = clockTime.IndexOf(':');
        if (separator < 0 || separator == clockTime.Length - 1)
        {
            return false;
        }

        bool isNegative = clockTime[0] == '-';
        string secondsText = isNegative ? clockTime.Substring(1, separator - 1) : clockTime.Substring(0, separator);
        string hundredthsText = clockTime.Substring(separator + 1);

        if (!long.TryParse(secondsText, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds) ||
            !long.TryParse(hundredthsText, NumberStyles.None, CultureInfo.InvariantCulture, out long hundredths) ||
            hundredths > 99)
        {
            return false;
        }

        if (seconds > (long.MaxValue - hundredths) / 100)
        {
            return false;
        }

        centiseconds = seconds * 100 + hundredths;
        if (isNegative)
        {
            centiseconds = -centiseconds;
        }

        return true;
    }

    public void RestartGame()
    {
        timeScale = 1;
        SetTimeScale(timeScale);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---- MONOBEHAVIOUR ---- \\

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // the timer for how long you get to type a word would 
        // probably decrease by unscaledDeltaTime, but this is for
        // the round timer

        ProcessRoundTimer();
    }
}
