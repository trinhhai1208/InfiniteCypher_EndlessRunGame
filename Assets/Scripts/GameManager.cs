using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }
    public bool IsPlaying { get; set; } = true;
    
    public int Distance  { get; private set; }
    public int BestDistance { get; private set; }
    public int CoinCount { get; private set; }

    public event Action<int> OnDistanceChanged;
    public event Action<int> OnCoinChanged;
    public event Action      OnGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Time.timeScale = 1f;
        IsGameOver = false;
        
        BestDistance = PlayerPrefs.GetInt("BestDistance", 0);
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
                PlayerPrefs.SetInt("BestDistance", BestDistance);
                PlayerPrefs.Save();
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
        
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
