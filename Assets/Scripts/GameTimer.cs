using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; } // allows GameTimer.Instance
    
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private float time = 100; // seconds

    private bool gameRunning = true;
    private float timeScale = 1; // way to keep track during pause menu

    public bool IsPaused()
    {
        return gameRunning;
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

        time -= Time.deltaTime;
        timerLabel.text = GetClockTime();

        if (time <= 0)
        {
            EndGame();
        }
    }

    public void EndGame(bool passed = false)
    {
        gameRunning = false;

        if (passed)
        {
            Debug.Log("Passed!");
            timerLabel.text = "You win! Final Time: " + GetClockTime();
        } 
        else
        {
            Debug.Log("Ran out of time");
            timerLabel.text = "Ran out of time";
        }
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
