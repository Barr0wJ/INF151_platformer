using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
    public bool enablePausing = true;
    private float maxTime;

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
        if (time + value <= maxTime)
        {
            time += value;
        } else
        {
            time = maxTime;
        }
    }

    public void DecreaseTime(float value)
    {
        time -= value;
    }


    // ---- game menu ---- \\

    public void HandlePause()
    {
        if (!enablePausing) return;
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
        string sceneName = SceneManager.GetActiveScene().name;
        string[] previousHighScores = RetrieveHighScores(sceneName);
        bool isNewHighScore = passed &&
            (previousHighScores.Length == 0 ||
             (TryParseClockTime(previousHighScores[0], out long previousBest) &&
              TryParseClockTime(GetClockTime(), out long finalTime) &&
              finalTime > previousBest));

        UpdateHighScores(passed, sceneName);
        string[] highScores = RetrieveHighScores(sceneName);
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

    public static string[] RetrieveHighScores(string sceneName = null, int limit = 3)
    {
        sceneName = sceneName ?? SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return new string[0];
        }

        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        if (!TryReadHighScoreColumns(path, out List<HighScoreColumn> columns))
        {
            return new string[0];
        }

        HighScoreColumn column = columns.Find(candidate => candidate.SceneName == sceneName);
        if (column == null)
        {
            return new string[0];
        }

        int scoreCount = Mathf.Min(Mathf.Clamp(limit, 0, HighScoreLimit), column.Scores.Count);
        var topScores = new string[scoreCount];
        for (int i = 0; i < scoreCount; i++)
        {
            topScores[i] = column.Scores[i].ClockTime;
        }

        return topScores;
    }

    public static bool ClearHighScores(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Cannot clear high scores because the scene name is empty.");
            return false;
        }

        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        Directory.CreateDirectory(Application.persistentDataPath);
        if (!TryReadHighScoreColumns(path, out List<HighScoreColumn> columns))
        {
            return false;
        }

        HighScoreColumn column = columns.Find(candidate => candidate.SceneName == sceneName);
        if (column == null)
        {
            columns.Add(new HighScoreColumn(sceneName));
        }
        else
        {
            column.Scores.Clear();
        }

        WriteHighScoreColumns(path, columns);
        return true;
    }

    public static void ClearAllHighScores()
    {
        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        Directory.CreateDirectory(Application.persistentDataPath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.WriteAllText(path, string.Empty);
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

    private void UpdateHighScores(bool passed, string sceneName = null)
    {
        sceneName = sceneName ?? SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Cannot save high scores because the active scene has no name.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, HighScoresFileName);
        Directory.CreateDirectory(Application.persistentDataPath);

        if (!TryReadHighScoreColumns(path, out List<HighScoreColumn> columns))
        {
            return;
        }

        HighScoreColumn column = columns.Find(candidate => candidate.SceneName == sceneName);
        if (column == null)
        {
            column = new HighScoreColumn(sceneName);
            columns.Add(column);
        }

        if (passed)
        {
            string currentTime = GetClockTime();
            if (!TryParseClockTime(currentTime, out long currentCentiseconds))
            {
                Debug.LogError($"Could not parse current high score '{currentTime}'");
                return;
            }

            column.Scores.Add(new HighScore(currentTime, currentCentiseconds));
        }

        foreach (HighScoreColumn scoreColumn in columns)
        {
            scoreColumn.Scores.Sort((left, right) =>
                right.Centiseconds.CompareTo(left.Centiseconds));
            if (scoreColumn.Scores.Count > HighScoreLimit)
            {
                scoreColumn.Scores.RemoveRange(
                    HighScoreLimit, scoreColumn.Scores.Count - HighScoreLimit);
            }
        }

        WriteHighScoreColumns(path, columns);
    }

    private static void WriteHighScoreColumns(
        string path, List<HighScoreColumn> columns)
    {
        var output = new StringBuilder();
        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            if (columnIndex > 0)
            {
                output.Append(',');
            }

            AppendCsvField(output, columns[columnIndex].SceneName);
        }
        output.AppendLine();

        int rowCount = 0;
        foreach (HighScoreColumn scoreColumn in columns)
        {
            rowCount = Mathf.Max(rowCount, scoreColumn.Scores.Count);
        }

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                if (columnIndex > 0)
                {
                    output.Append(',');
                }

                List<HighScore> scores = columns[columnIndex].Scores;
                if (rowIndex < scores.Count)
                {
                    AppendCsvField(output, scores[rowIndex].ClockTime);
                }
            }
            output.AppendLine();
        }

        File.WriteAllText(path, output.ToString());
    }

    private static bool TryReadHighScoreColumns(
        string path, out List<HighScoreColumn> columns)
    {
        columns = new List<HighScoreColumn>();
        if (!File.Exists(path))
        {
            return true;
        }

        if (!TryParseCsv(File.ReadAllText(path), out List<List<string>> rows))
        {
            Debug.LogError($"Could not read high scores because {path} is not valid CSV.");
            return false;
        }

        if (rows.Count == 0)
        {
            return true;
        }

        foreach (string sceneName in rows[0])
        {
            HighScoreColumn column = columns.Find(
                candidate => candidate.SceneName == sceneName);
            if (column == null)
            {
                columns.Add(new HighScoreColumn(sceneName));
            }
        }

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                if (columnIndex >= rows[0].Count)
                {
                    if (!string.IsNullOrWhiteSpace(row[columnIndex]))
                    {
                        Debug.LogWarning(
                            $"Skipping high score in an unnamed column in {path}");
                    }
                    continue;
                }

                string clockTime = row[columnIndex].Trim();
                if (clockTime.Length == 0)
                {
                    continue;
                }

                if (!TryParseClockTime(clockTime, out long centiseconds))
                {
                    Debug.LogWarning($"Skipping invalid high score '{clockTime}' in {path}");
                    continue;
                }

                string sceneName = rows[0][columnIndex];
                HighScoreColumn column = columns.Find(
                    candidate => candidate.SceneName == sceneName);
                column.Scores.Add(new HighScore(clockTime, centiseconds));
            }
        }

        foreach (HighScoreColumn column in columns)
        {
            column.Scores.Sort((left, right) =>
                right.Centiseconds.CompareTo(left.Centiseconds));
            if (column.Scores.Count > HighScoreLimit)
            {
                column.Scores.RemoveRange(
                    HighScoreLimit, column.Scores.Count - HighScoreLimit);
            }
        }

        return true;
    }

    private static bool TryParseCsv(string text, out List<List<string>> rows)
    {
        rows = new List<List<string>>();
        if (text.Length == 0)
        {
            return true;
        }

        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        bool afterQuote = false;
        bool recordStarted = false;

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        afterQuote = true;
                    }
                }
                else
                {
                    field.Append(character);
                }
                continue;
            }

            if (afterQuote && character != ',' && character != '\r' && character != '\n')
            {
                return false;
            }

            if (character == '"')
            {
                if (field.Length > 0 || afterQuote)
                {
                    return false;
                }
                inQuotes = true;
                recordStarted = true;
            }
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Clear();
                afterQuote = false;
                recordStarted = true;
            }
            else if (character == '\r' || character == '\n')
            {
                row.Add(field.ToString());
                rows.Add(row);
                row = new List<string>();
                field.Clear();
                afterQuote = false;
                recordStarted = false;
                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                field.Append(character);
                recordStarted = true;
            }
        }

        if (inQuotes)
        {
            return false;
        }
        if (recordStarted || field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return true;
    }

    private static void AppendCsvField(StringBuilder output, string value)
    {
        bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        if (!needsQuotes)
        {
            output.Append(value);
            return;
        }

        output.Append('"');
        output.Append(value.Replace("\"", "\"\""));
        output.Append('"');
    }

    private sealed class HighScoreColumn
    {
        public readonly string SceneName;
        public readonly List<HighScore> Scores = new List<HighScore>();

        public HighScoreColumn(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    private sealed class HighScore
    {
        public readonly string ClockTime;
        public readonly long Centiseconds;

        public HighScore(string clockTime, long centiseconds)
        {
            ClockTime = clockTime;
            Centiseconds = centiseconds;
        }
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
        maxTime = time;
        Debug.Log($"High scores file: {Path.Combine(Application.persistentDataPath, HighScoresFileName)}");
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
