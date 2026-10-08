using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// handles the typing skill check
// presumably called by PlayerBehaviour
public class TypingSkillCheck : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI targetWordLabel;
    [SerializeField] private TextMeshProUGUI inputLabel;

    [SerializeField] private float timeLimit = 3.0f;
    [SerializeField] private float wrongKeyDecrease = 0.1f;
    [SerializeField] private float timeSlowFactor = 0.2f;

    private GameTimer gameTimer;
    private GameObject playerObject;
    private PlayerBehaviour playerBehaviour;

    private string targetWord = "";
    private string currentInput = "";
    private float timeLeft = 0f;
    private bool isActive = false;
    private float originalTimeScale; // probably 1 but just in case it isn't

    public bool GetIsActive()
    {
        return isActive;
    }

    public void StartSkillCheck(string word = "job")
    {
        targetWord = word.ToUpper();
        currentInput = "";
        
        timeLeft = timeLimit;
        isActive = true;

        originalTimeScale = Time.timeScale;
        Time.timeScale = timeSlowFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // default is 0.02

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

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
        
        playerBehaviour.EndSkillCheck();

        if (success)
        {
            Debug.Log("passed");
            // add remaining time to the gametimer
            gameTimer.IncreaseTime(timeLeft);
        }
        else
        {
            Debug.Log("failure");
            // failed skill check to an unheard degree (the negative value makes the round timer go negative?)
            // probably restart round or smth
        }
    }

    // ---- MONOBEHAVIOUR -- \\

    void Start()
    {
        gameTimer = GameTimer.Instance;
        playerObject = GameObject.FindWithTag("Player");
        playerBehaviour = playerObject.GetComponent<PlayerBehaviour>();
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