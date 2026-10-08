using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; } // allows GameTimer.Instance
    
    [SerializeField] private float time = 100; // seconds


    public float GetTime()
    {
        return time;
    }

    // Returns in format: mm:ss:ms. Will this be used? idk
    public string GetClockTime()
    {
        return "";
    }

    public void IncreaseTime(float value)
    {
        time -= value;
    }

    public void DecreaseTime(float value)
    {
        time -= value;
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // the timer for how long you get to type a word would 
        // probably decrease by unscaledDeltaTime, but this is for
        // the round timer

        time -= Time.deltaTime; 
    }
}
