using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{

    [SerializeField] private bool enableAdminKeybinds = true;

    [SerializeField] private TypingSkillCheck skillCheck;

    private CameraController cameraController;
    private MovementController movementController;
    
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

    // for testing
    private void ProcessAdminControls()
    {
        if (!enableAdminKeybinds) return;

        // Typing Skill Check ; key = "I"
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            StartSkillCheck();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        movementController = GetComponent<MovementController>();
    }

    // Update is called once per frame
    void Update()
    {
        ProcessAdminControls();
    }
}
