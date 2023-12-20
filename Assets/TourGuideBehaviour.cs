using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TourGuideBehaviour : MonoBehaviour
{

    private AudioClip audioClip;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetAudioClip(AudioClip audio)
    {
        audioClip = audio;
    }
}
