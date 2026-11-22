using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Stopwatch_Speed : MonoBehaviour
{
    public Image stopwatch_Arrow;
    public Image stopwatch_Background;
   // [SerializeField] private float maxMoveSpeed = 50f;
    [SerializeField] private float timeRemaining = 1.7f;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [SerializeField] private MovementController MovementController;
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

    public IEnumerator UpdateStopWatch()//bool isWallJumping)
    {   
        /*
        if (isWallJumping)
        {
           ResetStopwatch();
           yield return null;
        }
        */
        timeRemaining = 1.7f;
        
        while (timeRemaining > 0f)
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            rotationAmount = timeRemaining / 1.7f;
            stopwatch_Arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, rotationAmount * -360);
            yield return null;
        }
        timeRemaining = 1.7f;
    }

    public void ResetStopwatch()
    {
        timeRemaining = 0f;
        rotationAmount = 0f;
        stopwatch_Arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, rotationAmount * -360);
    }

}
