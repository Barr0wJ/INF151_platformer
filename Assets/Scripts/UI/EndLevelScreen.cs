using UnityEngine;

public class EndLevelScreen : MonoBehaviour
{
    public static EndLevelScreen instance;

    [SerializeField] private GameObject endLevelScreen;

    [SerializeField] private ImageTransparencyFader ImageTransparencyFader;

    [SerializeField] private Color newTargetColor;
    [SerializeField] private float duration;

    private void Awake()
    {
        endLevelScreen.SetActive(false);
    }
    public void levelTransitionScreen()
    {
        endLevelScreen.SetActive(true);
        //ImageTransparencyFader.StartFade(newTargetColor, duration);
    }
}
