using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;

public class videoManager : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button skipButton;
    
    [Header("Canvas Management")]
    [SerializeField] private List<GameObject> canvasesToToggle = new List<GameObject>();
    
    private const string FirstVideoKey = "HasSeenIntroVideo";
    private bool videoCompleted = false;
    private List<bool> originalAudioSourceStates = new List<bool>();
    private List<bool> originalAudioSourcePlayingStates = new List<bool>();

    void Start()
    {
        // Check if this is the first time loading the game scene
        bool hasSeenVideo = PlayerPrefs.GetInt(FirstVideoKey, 0) == 1;
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Skip video entirely for WebGL builds
            Debug.Log("WebGL build detected - skipping intro video");
            OnVideoEnd();
        #else
            // Normal video behavior for other platforms
            if (!hasSeenVideo)
            {
                PlayIntroVideo();
            }
            else
            {
                OnVideoEnd();
            }
        #endif
    }

    private void PlayIntroVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer component not assigned!");
            OnVideoEnd();
            return;
        }

        // Disable all AudioController audio sources during video
        DisableAllAudioSources();
        
        // Turn off all canvases during video
        SetCanvasesActive(false);
        
        // Show skip button
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.onClick.AddListener(SkipVideo);
        }

        // Subscribe to video events
        videoPlayer.loopPointReached += OnVideoCompleted;
        
        // Start playing the video
        videoPlayer.Play();
        
        Debug.Log("Playing intro video for first time");
    }

    public void SkipVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        Debug.Log("Video skipped by player");
        OnVideoEnd();
    }

    private void OnVideoCompleted(VideoPlayer vp)
    {
        Debug.Log("Video completed naturally");
        OnVideoEnd();
    }

    private void OnVideoEnd()
    {
        // Mark that the video has been seen
        PlayerPrefs.SetInt(FirstVideoKey, 1);
        PlayerPrefs.Save();
        
        // Hide skip button
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        
        // Turn canvases back on
        SetCanvasesActive(true);
        
        // Re-enable all AudioController audio sources
        EnableAllAudioSources();
        
        // Clean up video player events
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoCompleted;
        }
        
        videoCompleted = true;
    }

    private void DisableAllAudioSources()
    {
        originalAudioSourceStates.Clear();
        originalAudioSourcePlayingStates.Clear();
        
        if (AudioController.Instance != null)
        {
            // Store and disable all sound audio sources
            foreach (var sound in AudioController.Instance.sounds)
            {
                if (sound.source != null)
                {
                    originalAudioSourceStates.Add(sound.source.enabled);
                    originalAudioSourcePlayingStates.Add(sound.source.isPlaying);
                    
                    if (sound.source.isPlaying)
                    {
                        sound.source.Stop();
                    }
                    sound.source.enabled = false;
                }
            }
            
            // Store and disable all music audio sources
            foreach (var music in AudioController.Instance.musics)
            {
                if (music.source != null)
                {
                    originalAudioSourceStates.Add(music.source.enabled);
                    originalAudioSourcePlayingStates.Add(music.source.isPlaying);
                    
                    if (music.source.isPlaying)
                    {
                        music.source.Stop();
                    }
                    music.source.enabled = false;
                }
            }
            
            Debug.Log("All AudioController audio sources disabled during video");
        }
    }

    private void EnableAllAudioSources()
    {
        if (AudioController.Instance != null)
        {
            int stateIndex = 0;
            
            // Restore sound audio sources
            foreach (var sound in AudioController.Instance.sounds)
            {
                if (sound.source != null && stateIndex < originalAudioSourceStates.Count)
                {
                    sound.source.enabled = originalAudioSourceStates[stateIndex];
                    
                    // Resume playing if it was playing before
                    if (originalAudioSourcePlayingStates[stateIndex] && sound.source.enabled)
                    {
                        sound.source.Play();
                    }
                    
                    stateIndex++;
                }
            }
            
            // Restore music audio sources
            foreach (var music in AudioController.Instance.musics)
            {
                if (music.source != null && stateIndex < originalAudioSourceStates.Count)
                {
                    music.source.enabled = originalAudioSourceStates[stateIndex];
                    
                    // Resume playing if it was playing before
                    if (originalAudioSourcePlayingStates[stateIndex] && music.source.enabled)
                    {
                        music.source.Play();
                    }
                    
                    stateIndex++;
                }
            }
            
            Debug.Log("All AudioController audio sources re-enabled after video");
        }
        
        originalAudioSourceStates.Clear();
        originalAudioSourcePlayingStates.Clear();
    }

    private void SetCanvasesActive(bool active)
    {
        foreach (GameObject canvas in canvasesToToggle)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(active);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up events to prevent memory leaks
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoCompleted;
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipVideo);
        }
        
        // Make sure to re-enable all audio sources if script is destroyed
        EnableAllAudioSources();
    }
}