using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class VideoControls : MonoBehaviour
{
    public GameObject overlay;
    //public GameObject btnContainer;
    public GameObject playButton;
    public GameObject pauseButton;
    public GameObject replayButton;
    public GameObject changeOrientationButton;
    public Image screenParent;
    public RawImage screen;
    
    [Range(0.1f, 2f)] 
    public float fadeTime = 1f;
    
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private ScreenControl screenControl;
    private ViewMode currentViewMode;
    private DeviceOrientation currentOrientation;
    private float controlTimer;
    private bool isPlaying;
    private bool isControlsActive;
    private bool isFullscreen;
    
    // Start is called before the first frame update
    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
        screenControl = screen.GetComponent<ScreenControl>();
        
        videoPlayer.prepareCompleted += IsPrepared;
        videoPlayer.loopPointReached += EndOfVideoReached;
        screenControl.OnScreenClicked += TriggerVideoOverlay;
    }

    private void Update()
    {
       AutoDisableControls();
       //CheckPhoneOrientation();
    }

    private void OnDisable()
    {
        Reset();
    }
    

    private void AutoDisableControls()
    {
        if (!isPlaying)
            return;
        
        if (controlTimer > 0)
        {
            controlTimer -= Time.deltaTime;
        }
        else
        {
            overlay.SetActive(false);
            isControlsActive = false;
        }
    }

    private void CheckPhoneOrientation()
    {
        if (!isFullscreen)
            return;

        if (Input.deviceOrientation == currentOrientation)
            return;
        
        switch (Input.deviceOrientation)
        {
            case DeviceOrientation.LandscapeLeft:
            {
                var rotation = new Vector3(0, 0, 270);
                screen.rectTransform.rotation = Quaternion.Euler(rotation);
                break;
            }
            case DeviceOrientation.LandscapeRight:
            {
                var rotation = new Vector3(0, 0, 90);
                screen.rectTransform.rotation = Quaternion.Euler(rotation);
                break;
            }
        }
    }
    
    private void TriggerVideoOverlay()
    {
        if (isControlsActive)
        {
            overlay.SetActive(false);
        }
        else
        {
            overlay.SetActive(true);
            controlTimer = fadeTime;
        }

        isControlsActive = !isControlsActive;
    }

    public void SetupVideo(string url)
    {
        try
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack (0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.url = url;
            
            videoPlayer.isLooping = true;
            videoPlayer.Prepare();
        }
        catch (Exception ex)
        {
            Debug.Log("An exception occured: " + ex.Message);
        }
        
        Pause();
    }

    private void SetupImage()
    {
        if (GetCurrentAspectRatio() > 1.0f)
        {
            SetVideoOrientation(ViewMode.LandScapeVertical);
        }
        else
        {
            SetVideoOrientation(ViewMode.Portrait);
        }
    }

    private void SetVideoOrientation(ViewMode viewMode)
    {
        var screenRect = screen.rectTransform;
        var parentRect = screenParent.rectTransform.rect;
        switch (viewMode)
        {
            case ViewMode.Portrait:
                screenRect.sizeDelta = new Vector2(screenRect.sizeDelta.y * GetCurrentAspectRatio(), screenRect.sizeDelta.y);
                changeOrientationButton.SetActive(false);
                break;
            case ViewMode.LandScapeFullscreen:
                var rotation = new Vector3(0, 0, 270);
                screen.rectTransform.rotation = Quaternion.Euler(rotation);
                screenRect.sizeDelta = new Vector2(parentRect.width * GetCurrentAspectRatio(), parentRect.width);
                break;
            case ViewMode.LandScapeVertical:
                screenRect.rotation = Quaternion.Euler(Vector3.zero);
                screenRect.sizeDelta = new Vector2(parentRect.width, parentRect.width / GetCurrentAspectRatio());
                break;
        }

        currentViewMode = viewMode;
    }

    private void IsPrepared(VideoPlayer player)
    {
        playButton.SetActive(true);
        SetupImage();
    }

    private void EndOfVideoReached(VideoPlayer player)
    {
        isPlaying = false;
        videoPlayer.playbackSpeed = 0;
        overlay.SetActive(true);
        playButton.SetActive(false);
        pauseButton.SetActive(false);
        replayButton.SetActive(true);
    }

    public void Play()
    {
        playButton.SetActive(false);
        pauseButton.SetActive(true);

        videoPlayer.playbackSpeed = 1;
        isPlaying = true;
    }

    public void Pause()
    {
        playButton.SetActive(true);
        pauseButton.SetActive(false);

        videoPlayer.playbackSpeed = 0;
        isPlaying = false;
    }
    
    public void Replay()
    {
        replayButton.SetActive(false);
        videoPlayer.frame = 0;
        Play();
    }

    public void ToggleFullscreen()
    {
        switch (currentViewMode)
        {
            case ViewMode.LandScapeVertical:
                SetVideoOrientation(ViewMode.LandScapeFullscreen);
                isFullscreen = true;
                break;
            case ViewMode.LandScapeFullscreen:
                SetVideoOrientation(ViewMode.LandScapeVertical);
                isFullscreen = false;
                break;
        }
    }
    
    private float GetCurrentAspectRatio()
    {
        // Check video format and adjust RawImage size
        float videoWidth = videoPlayer.width;
        float videoHeight = videoPlayer.height;

        // Calculate the aspect ratio of the video
        return videoWidth / videoHeight;
    }

 
    
    private void Reset()
    {
        videoPlayer.frame = 0;
        overlay.SetActive(true);
        Pause();
    }

    private enum ViewMode
    {
        Portrait,
        LandScapeFullscreen,
        LandScapeVertical
    }
}


