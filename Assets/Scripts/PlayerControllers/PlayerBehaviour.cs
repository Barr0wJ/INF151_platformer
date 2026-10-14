using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{

    [SerializeField] private bool enableAdminKeybinds = true;

    [SerializeField] private TypingSkillCheck skillCheck;

    private CameraController cameraController;
    private MovementController movementController;
    private GameTimer gameTimer;
    
    private void StartSkillCheck()
    {
        if (skillCheck != null && !skillCheck.GetIsActive())
        {
            movementController.movementEnabled = false;
            cameraController.ZoomIn();
            skillCheck.StartSkillCheck();
        }
    }

    // called by TypingSkillCheck when finished
    public void EndSkillCheck()
    {
        movementController.movementEnabled = true;
        cameraController.ZoomOut();
    }

    
    private IEnumerator WaitToRestart(float waitTime)
    {
        yield return new WaitForSecondsRealtime(waitTime);
        GameTimer.Instance.RestartGame();
    }

    public void RestartGame(float waitTime = 0f)
    {
        StartCoroutine(WaitToRestart(waitTime));
    }

    // for testing
    private void ProcessAdminControls()
    {
        if (!enableAdminKeybinds) return;

        // Typing Skill Check ; key = "I"
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            StartSkillCheck();
        }
        else if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            RestartGame();
        }

    }

    private void ProcessHotkeys()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            gameTimer.HandlePause();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        movementController = GetComponent<MovementController>();
        gameTimer = GameTimer.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessAdminControls();
        ProcessHotkeys();
    }
}
