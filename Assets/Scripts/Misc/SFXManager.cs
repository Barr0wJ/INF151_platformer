using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour 
{
    public static SFXManager instance;

    [SerializeField] private AudioSource soundFXOjbect;

    private void Awake()
    {
        if (instance == null) {
            instance = this;
        }
    }


    public void PlaySFX(AudioClip clip, Transform spawnTransform, float volume, float pitch){

        AudioSource audiosource = Instantiate(soundFXOjbect, spawnTransform.position, Quaternion.identity);

        audiosource.clip = clip;

        audiosource.volume = volume;

        audiosource.pitch = pitch;

        audiosource.Play();

        float clipLength = audiosource.clip.length;

        Destroy(audiosource.gameObject, clipLength);
    }
}
