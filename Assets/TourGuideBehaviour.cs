using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TourGuideBehaviour : MonoBehaviour
{
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetAudioClip(AudioClip audio)
    {
        audioSource.clip = audio;
    }

    public void PlayAudio()
    {
        if (audioSource == null || audioSource.clip == null || audioSource.isPlaying)
        {
            return;
        }
        
        audioSource.PlayDelayed(0.5f);
    }
}
