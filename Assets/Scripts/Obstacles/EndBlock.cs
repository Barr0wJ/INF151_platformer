using UnityEngine;

public class EndBlock : MonoBehaviour
{
    private bool ended = false;
    [SerializeField] private string nextLevelName;

    private void OnTriggerEnter2D(Collider2D other) {
        if (ended) return;
        ended = true;
        GameTimer.Instance.EndGame(true);
        gameManager.instance.changeLevelTo(nextLevelName);
    }
}
