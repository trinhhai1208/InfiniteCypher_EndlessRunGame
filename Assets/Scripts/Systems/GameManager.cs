using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState { Loadout, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Loadout;
    public bool IsGameOver => State == GameState.GameOver;
    public bool IsPlaying => State == GameState.Playing;
    
    public int Distance  { get; private set; }
    public int BestDistance { get; private set; }
    public int CoinCount { get; private set; }
    public int TotalGold { get; private set; } // Tổng số vàng tích lũy

    private static bool _shouldAutoStart = false; // Biến static để giữ trạng thái qua scene mới

    public event Action<int> OnDistanceChanged;
    public event Action<int> OnCoinChanged;
    public event Action      OnGameOver;
    public event Action      OnGameStart; // Sự kiện khi nhấn Start Dash

    private void Awake()
    {
        // Singleton pattern: Dọn dẹp nếu có tham chiếu rác từ scene cũ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tối ưu cho WebGL: Ép FPS về 60 để mượt hơn
        Application.targetFrameRate = 60;
        
        Time.timeScale = 1f;
        State = GameState.Loadout;
        
        BestDistance = PlayerPrefs.GetInt("BestDistance", 0);
        TotalGold = PlayerPrefs.GetInt("TotalGold", 0);

        // Ép Unity cập nhật lại ánh sáng môi trường để tránh bị tối khi Reload Scene
        DynamicGI.UpdateEnvironment();

        // Đăng ký vào ServiceLocator để các module khác dùng thay FindObjectOfType
        ServiceLocator.Register<GameManager>(this);
    }

    private void Start()
    {
        // Nếu vừa nhấn Restart từ màn hình GameOver, nhảy vào chơi luôn
        if (_shouldAutoStart)
        {
            _shouldAutoStart = false; // Reset flag
            StartCoroutine(WaitAndStartGame());
        }
    }

    private IEnumerator WaitAndStartGame()
    {
        // H4: Dùng ServiceLocator thay FindObjectOfType (O(1) thay vì scan toàn Scene)
        TrackManager tm = ServiceLocator.Get<TrackManager>();
        if (tm == null) tm = FindObjectOfType<TrackManager>(); // fallback an toàn
        
        // Chờ đến khi Map sẵn sàng
        while (tm != null && !tm.IsReady)
        {
            yield return null;
        }

        StartGame();
    }

    public static void SetAutoStart(bool auto)
    {
        _shouldAutoStart = auto;
    }

    public void StartGame()
    {
        State = GameState.Playing;
        OnGameStart?.Invoke();
        EventBus.Publish(new GameStartEvent());
    }

    public void UpdateDistance(float zPosition)
    {
        if (IsGameOver) return;

        int newDistance = Mathf.FloorToInt(zPosition);
        if (newDistance > Distance)
        {
            Distance = newDistance;
            OnDistanceChanged?.Invoke(Distance);
            // Publish EventBus (H5)
            EventBus.Publish(new DistanceChangedEvent { Distance = Distance });

            if (Distance > BestDistance)
            {
                BestDistance = Distance;
                // P1: Chỉ SetInt trong bộ nhớ, KHÔNG Save() xuống đĩa mỗi frame.
                // Save() thực sự chỉ chạy 1 lần trong TriggerGameOver.
                PlayerPrefs.SetInt("BestDistance", BestDistance);
            }
        }
    }

    public void AddCoin()
    {
        if (IsGameOver) return;

        // Mặc định mỗi lần ăn 1 xu. Nếu có PowerUp Multiplier thì nhân đôi (ăn 1 thành 2).
        int increment = 1;
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsMultiplierActive())
        {
            increment = 2;
        }

        CoinCount += increment;
        OnCoinChanged?.Invoke(CoinCount);
        // Publish EventBus (H5)
        EventBus.Publish(new CoinCollectedEvent { Count = CoinCount });
    }

    public void TriggerGameOver()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;

        // Dừng nhạc nền khi thua cuộc
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Cộng dồn vàng vào tổng kho khi kết thúc ván
        TotalGold += CoinCount;
        PlayerPrefs.SetInt("TotalGold", TotalGold);
        // P1: Save() tất cả 1 lần duy nhất ở đây (bao gồm cả BestDistance)
        PlayerPrefs.Save();
        
        OnGameOver?.Invoke();
        // Publish EventBus (H5)
        EventBus.Publish(new GameOverEvent());
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<GameManager>();
            Instance = null;
        }
    }
}
