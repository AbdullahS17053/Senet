using UnityEngine;
using UnityEngine.UI;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume = 1f;
        public bool loop = false;
        [HideInInspector] public AudioSource source;
    }

    public Sound[] sounds;
    private float globalVolume = 1f;

    private Slider volumeSlider;
    private const string FirstTimeKey = "HasPlayedBefore";
    private const string VolumeKey = "GlobalVolume";

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeVolume();
        SetupSounds();
    }

    private void Start()
    {
        TryFindSlider(); // Initial attempt to find the slider
    }

    private void InitializeVolume()
    {
        if (!PlayerPrefs.HasKey(FirstTimeKey))
        {
            PlayerPrefs.SetFloat(VolumeKey, 1f);
            PlayerPrefs.SetInt(FirstTimeKey, 1);
        }

        globalVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    private void SetupSounds()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;
            s.source.playOnAwake = false;
        }

        ApplyVolume();
    }

    private void Update()
    {
        ApplyVolume();

        // If the slider hasn't been assigned yet, keep trying
        if (volumeSlider == null)
            TryFindSlider();
    }

    private void TryFindSlider()
    {
        GameObject sliderObj = GameObject.FindGameObjectWithTag("VolumeSlider");
        if (sliderObj != null)
        {
            volumeSlider = sliderObj.GetComponent<Slider>();
            if (volumeSlider != null)
            {
                volumeSlider.value = globalVolume;
                volumeSlider.onValueChanged.AddListener(SetGlobalVolume);
            }
        }
    }

    private void ApplyVolume()
    {
        foreach (Sound s in sounds)
        {
            if (s.source != null)
                s.source.volume = s.baseVolume * globalVolume;
        }
    }

    public void SetGlobalVolume(float value)
    {
        globalVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, globalVolume);
        PlayerPrefs.Save();
    }

    public void Play(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null && s.source != null)
        {
            s.source.Play();
        }
    }

    public void Stop(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null && s.source != null)
        {
            s.source.Stop();
        }
    }
}
