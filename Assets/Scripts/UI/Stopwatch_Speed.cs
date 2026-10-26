using UnityEngine;
using UnityEngine.UI;

public class Stopwatch_Speed : MonoBehaviour
{
    public Image stopwatch_Arrow;
    public Image stopwatch_Background;
    [SerializeField] private float maxMoveSpeed = 50f;
    [SerializeField] private Rigidbody2D playerRigidbody;
    private float rotationAmount;

    private void Awake()
    {
        if (stopwatch_Arrow == null)
        {
            Debug.LogError("Stopwatch_Speed requires the stopwatch arrow Image to be assigned.", this);
            enabled = false;
            return;
        }

        if (playerRigidbody == null)
        {
            Debug.LogError("Stopwatch_Speed requires the player's Rigidbody2D to be assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Debug.Log(playerRigidbody.linearVelocity.x);
        rotationAmount = Mathf.Abs(playerRigidbody.linearVelocity.x) / maxMoveSpeed;
        stopwatch_Arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, rotationAmount * -360);
    }
}
