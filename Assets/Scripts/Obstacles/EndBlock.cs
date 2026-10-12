using UnityEngine;

public class EndBlock : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) {
        GameTimer.Instance.EndGame(true);
    }
}
