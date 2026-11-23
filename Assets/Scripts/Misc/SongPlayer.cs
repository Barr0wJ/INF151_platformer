using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] songs;
    private AudioSource audioSource;
    private int currentIndex = 0;

    public void PlayNext()
    {
        if (songs.Length == 0) return; 

        currentIndex++;
        float savedTime = audioSource.time;

        if (currentIndex >= songs.Length)
        {
            currentIndex = 0;
        }

        audioSource.Stop();
        audioSource.clip = songs[currentIndex];

        if (savedTime < audioSource.clip.length)
        {
            audioSource.time = savedTime;
        }
        else
        {
            audioSource.time = 0f;
        }

        audioSource.Play();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
}
