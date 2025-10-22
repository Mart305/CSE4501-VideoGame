using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Tower Shooting Sounds")]
    [SerializeField] private AudioClip fireTowerShootSound;
    [SerializeField] private AudioClip iceTowerShootSound;
    [SerializeField] private AudioClip ballistaTowerShootSound;
    [SerializeField] private AudioClip lightningTowerShootSound;
    [SerializeField] private AudioClip voidTowerShootSound;
    [SerializeField] private AudioClip defaultTowerShootSound;

    [Header("Combat Sounds")]
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip enemyDeathSound;
    [SerializeField] private AudioClip towerDestroyedSound;
    [SerializeField] private AudioClip towerPlacedSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip waveStartSound;
    [SerializeField] private AudioClip waveCompleteSound;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;

    // Pool of audio sources for playing multiple sounds simultaneously
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private int poolSize = 10;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Play background music on loop
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void InitializeAudioSources()
    {
        // Create main audio sources if they don't exist
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.parent = transform;
            musicObj.layer = LayerMask.NameToLayer("Tower");
            musicSource = musicObj.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.parent = transform;
            sfxObj.layer = LayerMask.NameToLayer("Tower");
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        // Create audio source pool for simultaneous sounds
        for (int i = 0; i < poolSize; i++)
        {
            GameObject poolObj = new GameObject($"PooledAudioSource_{i}");
            poolObj.transform.parent = transform;
            poolObj.layer = LayerMask.NameToLayer("Tower");
            AudioSource source = poolObj.AddComponent<AudioSource>();
            audioSourcePool.Add(source);
        }
    }

    // Play sound effect using pooled audio sources
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        // Find available audio source from pool
        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource != null)
        {
            availableSource.clip = clip;
            availableSource.volume = sfxVolume * volumeMultiplier;
            availableSource.Play();
        }
    }

    // Play 3D positional sound
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource != null)
        {
            availableSource.transform.position = position;
            availableSource.clip = clip;
            availableSource.volume = sfxVolume * volumeMultiplier;
            availableSource.spatialBlend = 1f; // Make it 3D
            availableSource.maxDistance = 50f;
            availableSource.Play();

            // Reset to 2D after playing
            StartCoroutine(ResetSpatialBlendAfterPlay(availableSource, clip.length));
        }
    }

    private System.Collections.IEnumerator ResetSpatialBlendAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (source != null)
        {
            source.spatialBlend = 0f; // Reset to 2D
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        // Find first audio source that's not playing
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // If all sources are busy, use the first one anyway
        return audioSourcePool.Count > 0 ? audioSourcePool[0] : sfxSource;
    }

    // Tower-specific shooting sounds
    public void PlayTowerShootSound(string towerType, Vector3 position)
    {
        AudioClip clip = GetTowerShootClip(towerType);
        if (clip != null)
        {
            PlaySFXAtPosition(clip, position, 0.6f);
        }
        else if (defaultTowerShootSound != null)
        {
            PlaySFXAtPosition(defaultTowerShootSound, position, 0.6f);
        }
    }

    private AudioClip GetTowerShootClip(string towerType)
    {
        switch (towerType.ToLower())
        {
            case "firetower":
                return fireTowerShootSound;
            case "icetower":
                return iceTowerShootSound;
            case "ballistatower":
                return ballistaTowerShootSound;
            case "lightningtower":
                return lightningTowerShootSound;
            case "voidtower":
                return voidTowerShootSound;
            default:
                return defaultTowerShootSound;
        }
    }

    // Convenience methods for common sounds
    public void PlayEnemyHitSound(Vector3 position)
    {
        PlaySFXAtPosition(enemyHitSound, position, 0.5f);
    }

    public void PlayEnemyDeathSound(Vector3 position)
    {
        PlaySFXAtPosition(enemyDeathSound, position, 0.7f);
    }

    public void PlayTowerDestroyedSound(Vector3 position)
    {
        PlaySFXAtPosition(towerDestroyedSound, position, 0.8f);
    }

    public void PlayTowerPlacedSound(Vector3 position)
    {
        PlaySFXAtPosition(towerPlacedSound, position, 0.6f);
    }

    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickSound, 0.5f);
    }

    public void PlayWaveStartSound()
    {
        PlaySFX(waveStartSound, 0.8f);
    }

    public void PlayWaveCompleteSound()
    {
        PlaySFX(waveCompleteSound, 0.8f);
    }

    public void PlayVictorySound()
    {
        PlaySFX(waveCompleteSound, 1f); // Reuse wave complete sound for victory
    }

    public void PlayDefeatSound()
    {
        PlaySFX(towerDestroyedSound, 1f); // Reuse tower destroyed sound for defeat
    }

    // Volume control
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void ToggleMusic()
    {
        if (musicSource != null)
        {
            if (musicSource.isPlaying)
                musicSource.Pause();
            else
                musicSource.UnPause();
        }
    }

    public void StopAllSounds()
    {
        if (musicSource != null)
            musicSource.Stop();

        if (sfxSource != null)
            sfxSource.Stop();

        foreach (AudioSource source in audioSourcePool)
        {
            if (source != null && source.isPlaying)
                source.Stop();
        }
    }
    
    // Master volume control for options panel
    public void SetMasterVolume(float volume)
    {
        // Master volume is handled by AudioListener.volume in OptionsPanel
        // This method exists for consistency with the options panel interface
    }
}
