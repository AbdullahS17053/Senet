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

    [System.Serializable]
    public class Music
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume = 1f;
        public bool loop = true;
        [HideInInspector] public AudioSource source;
    }

    public Sound[] sounds;
    public Music[] musics;

    private float globalSoundVolume = 1f;
    private float globalMusicVolume = 1f;

    private Slider soundSlider;
    private Slider musicSlider;

    private const string FirstTimeKey = "HasPlayedBefore";
    private const string SoundVolumeKey = "GlobalSoundVolume";
    private const string MusicVolumeKey = "GlobalMusicVolume";

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

        InitializeVolumes();
        SetupSounds();
        SetupMusics();
    }

    private void Start()
    {
        TryFindSliders();
    }

    private void Update()
    {
        ApplyVolumes();

        // If sliders not found yet, keep trying
        if (soundSlider == null || musicSlider == null)
            TryFindSliders();
    }

    private void InitializeVolumes()
    {
        if (!PlayerPrefs.HasKey(FirstTimeKey))
        {
            PlayerPrefs.SetFloat(SoundVolumeKey, 1f);
            PlayerPrefs.SetFloat(MusicVolumeKey, 1f);
            PlayerPrefs.SetInt(FirstTimeKey, 1);
        }

        globalSoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        globalMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
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
    }

    private void SetupMusics()
    {
        foreach (Music m in musics)
        {
            m.source = gameObject.AddComponent<AudioSource>();
            m.source.clip = m.clip;
            m.source.loop = m.loop;
            m.source.playOnAwake = false;
        }
    }

    private void ApplyVolumes()
    {
        foreach (Sound s in sounds)
        {
            if (s.source != null)
                s.source.volume = s.baseVolume * globalSoundVolume;
        }

        foreach (Music m in musics)
        {
            if (m.source != null)
                m.source.volume = m.baseVolume * globalMusicVolume;
        }
    }

    private void TryFindSliders()
    {
        if (soundSlider == null)
        {
            GameObject soundSliderObj = GameObject.FindGameObjectWithTag("SoundSlider");
            if (soundSliderObj != null)
            {
                soundSlider = soundSliderObj.GetComponent<Slider>();
                soundSlider.value = globalSoundVolume;
                soundSlider.onValueChanged.AddListener(SetSoundVolume);
            }
        }

        if (musicSlider == null)
        {
            GameObject musicSliderObj = GameObject.FindGameObjectWithTag("MusicSlider");
            if (musicSliderObj != null)
            {
                musicSlider = musicSliderObj.GetComponent<Slider>();
                musicSlider.value = globalMusicVolume;
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }
        }
    }

    public void SetSoundVolume(float value)
    {
        globalSoundVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SoundVolumeKey, globalSoundVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        globalMusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, globalMusicVolume);
        PlayerPrefs.Save();
    }

    public void PlaySound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null && s.source != null)
        {
            s.source.Play();
        }
    }

    public void StopSound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s != null && s.source != null)
        {
            s.source.Stop();
        }
    }

    public void PlayMusic(string name)
    {
        Music m = System.Array.Find(musics, music => music.name == name);
        if (m != null && m.source != null)
        {
            m.source.Play();
        }
    }

    public void StopMusic(string name)
    {
        Music m = System.Array.Find(musics, music => music.name == name);
        if (m != null && m.source != null)
        {
            m.source.Stop();
        }
    }
}
