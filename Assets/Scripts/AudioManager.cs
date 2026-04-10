using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private AudioClip _coinAltSound; // Coin_1
    [SerializeField] private AudioClip _maleDeathSound;
    [SerializeField] private AudioClip _femaleDeathSound;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tự động tạo AudioSource nếu chưa có
        SetupAudioSources();

        // Đăng ký sự kiện chuyển cảnh
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void SetupAudioSources()
    {
        // Tạo Music Source
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;

        // Tạo SFX Source
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
        // Chạy lần đầu khi khởi tạo
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
        
        // Nếu cùng 1 clip mà đang phát rồi thì bỏ qua
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, volume);
    }

    // --- Helper Methods cho từng loại âm thanh cụ thể ---

    public void PlayJump() => PlaySFX(_jumpSound);
    public void PlaySlide() => PlaySFX(_slideSound);
    
    public void PlayCoin()
    {
        // Ngẫu nhiên chọn giữa 2 tiếng xu để cho sinh động
        AudioClip clip = Random.value > 0.5f ? _coinSound : _coinAltSound;
        PlaySFX(clip, 0.7f);
    }

    public void PlayDeath(bool isFemale = false)
    {
        PlaySFX(isFemale ? _femaleDeathSound : _maleDeathSound);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }
}
