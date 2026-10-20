using System;
using Random = UnityEngine.Random;

[Serializable]
public class Range
{
    public float lowerBound;
    public float upperBound;

    public Range(float lowerBound, float upperBound)
    {
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
    }

    public int GetRandomInt()
    {
        return (int) GetRandomFloat();
    }

    public float GetRandomFloat()
    {
        if (lowerBound == upperBound) return lowerBound;
        return Random.Range(lowerBound, upperBound);
    }
}
