using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }
    public bool IsPlaying { get; set; } = false; // Mặc định là false để dừng ở Menu
    
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tối ưu cho WebGL: Ép FPS về 60 để mượt hơn
        Application.targetFrameRate = 60;
        
        Time.timeScale = 1f;
        IsGameOver = false;
        IsPlaying = false; 
        
        BestDistance = PlayerPrefs.GetInt("BestDistance", 0);
        TotalGold = PlayerPrefs.GetInt("TotalGold", 0);

        // Ép Unity cập nhật lại ánh sáng môi trường để tránh bị tối khi Reload Scene
        DynamicGI.UpdateEnvironment();
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
        TrackManager tm = FindObjectOfType<TrackManager>();
        
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
        IsPlaying = true;
        OnGameStart?.Invoke();
    }

    public void UpdateDistance(float zPosition)
    {
        if (IsGameOver) return;

        int newDistance = Mathf.FloorToInt(zPosition);
        if (newDistance > Distance)
        {
            Distance = newDistance;
            OnDistanceChanged?.Invoke(Distance);

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
        CoinCount++;
        OnCoinChanged?.Invoke(CoinCount);
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        IsPlaying = false;

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
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
