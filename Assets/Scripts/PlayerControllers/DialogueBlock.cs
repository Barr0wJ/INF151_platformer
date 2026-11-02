using System;
using UnityEngine;

public class DialogueBlock : MonoBehaviour
{
    [SerializeField] private string lines = "";
    [SerializeField] private float endLag = 5;
    
    public string GetLines()
    {
        return lines;
    }

    public float GetEndlag()
    {
        return endLag;
    }
}
