using UnityEngine;

public class EndLevelScreen : MonoBehaviour
{
    public static EndLevelScreen instance;

    [SerializeField] private GameObject endLevelScreen;

    private void Awake()
    {
        endLevelScreen.SetActive(false);
    }
    public void levelTransitionScreen()
    {
        endLevelScreen.SetActive(true);
    }
}
