using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 20f;

    private CameraController cameraController;
    private MovementController movementController;
    private bool isActive;
    private bool movementWasEnabled;
    private bool cameraControllerWasEnabled;

    public static void ToggleFreeCam()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Cannot toggle free camera because there is no MainCamera.");
            return;
        }

        FreeCamera freeCamera = mainCamera.GetComponent<FreeCamera>();
        if (freeCamera == null)
        {
            freeCamera = mainCamera.gameObject.AddComponent<FreeCamera>();
        }

        freeCamera.Toggle();
    }

    private void Toggle()
    {
        if (isActive)
        {
            Deactivate();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        movementController = player != null ? player.GetComponent<MovementController>() : null;
        if (movementController == null)
        {
            Debug.LogError("Cannot enable free camera because the Player has no MovementController.");
            return;
        }

        cameraController = GetComponent<CameraController>();
        movementWasEnabled = movementController.movementEnabled;
        cameraControllerWasEnabled = cameraController != null && cameraController.enabled;

        movementController.movementEnabled = false;
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        isActive = true;
    }

    private void Deactivate()
    {
        movementController.movementEnabled = movementWasEnabled;
        if (cameraController != null)
        {
            cameraController.enabled = cameraControllerWasEnabled;
        }

        isActive = false;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveLeft, PlayerKeybinds.MoveLeftAlternate))
        {
            input.x -= 1f;
        }

        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveRight, PlayerKeybinds.MoveRightAlternate))
        {
            input.x += 1f;
        }

        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveUp, PlayerKeybinds.MoveUpAlternate))
        {
            input.y += 1f;
        }

        if (PlayerKeybinds.IsPressed(PlayerKeybinds.MoveDown, PlayerKeybinds.MoveDownAlternate))
        {
            input.y -= 1f;
        }

        transform.position += (Vector3)(input.normalized * moveSpeed * Time.unscaledDeltaTime);
    }
}
