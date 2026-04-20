using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý âm thanh toàn cục (BGM & SFX). 
/// Đã tối ưu cho WebGL: Điều khiển trực tiếp volume để giảm overhead.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Background Music (BGM)")]
    [SerializeField] private AudioClip _menuMusic;
    [SerializeField] private AudioClip _inGameMusic;

    [Header("Sound Effects (SFX)")]
    [SerializeField] private AudioClip _jumpSound;
    [SerializeField] private AudioClip _slideSound;
    [SerializeField] private AudioClip _coinSound;
    [SerializeField] private AudioClip _coinAltSound; 
    [SerializeField] private AudioClip _maleDeathSound;
    [SerializeField] private AudioClip _femaleDeathSound;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    // --- Cài đặt âm lượng ---
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;

    public float MusicVolume => _musicVolume;
    public float SFXVolume => _sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadVolumeSettings(); // Tải cài đặt ngay khi khởi tạo

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void SetupAudioSources()
    {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMusicByScene(scene.name);
    }

    private void Start()
    {
        UpdateMusicByScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateMusicByScene(string sceneName)
    {
        if (sceneName == "MainMenuScene" || sceneName == "LoadingScene")
        {
            PlayMusic(_menuMusic);
        }
        else if (sceneName == "GameScene")
        {
            PlayMusic(_inGameMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.volume = _musicVolume; // Áp dụng volume hiện tại
        _musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        // SFX thực tế = volume truyền vào * volume tổng hệ thống
        _sfxSource.PlayOneShot(clip, volume * _sfxVolume);
    }

    // --- Volume Control API ---

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        // SFX Source volume gốc luôn là 1, ta điều khiển qua PlayOneShot
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
    }

    private void LoadVolumeSettings()
    {
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }

    // --- Helper Methods ---

    public void PlayJump() => PlaySFX(_jumpSound);
    public void PlaySlide() => PlaySFX(_slideSound);
    
    public void PlayCoin()
    {
        AudioClip clip = Random.value > 0.5f ? _coinSound : _coinAltSound;
        PlaySFX(clip, 0.7f);
    }

    public void PlayDeath(bool isFemale = false)
    {
        PlaySFX(isFemale ? _femaleDeathSound : _maleDeathSound);
    }

    public void StopMusic() => _musicSource.Stop();
}
