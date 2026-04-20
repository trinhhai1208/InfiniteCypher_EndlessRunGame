using UnityEngine;
using System;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    // Hợp nhất về GameManager: GameFlowManager giờ chỉ trỏ về GameManager để giữ tương thích (Legacy support)
    public GameState CurrentState => GameManager.Instance != null ? GameManager.Instance.State : GameState.Loadout;

    public event Action OnGameStart
    {
        add { if (GameManager.Instance != null) GameManager.Instance.OnGameStart += value; }
        remove { if (GameManager.Instance != null) GameManager.Instance.OnGameStart -= value; }
    }

    public event Action OnGameOver
    {
        add { if (GameManager.Instance != null) GameManager.Instance.OnGameOver += value; }
        remove { if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= value; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ServiceLocator.Register<GameFlowManager>(this);
    }

    public void StartGame() => GameManager.Instance?.StartGame();
    public void TriggerGameOver() => GameManager.Instance?.TriggerGameOver();

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<GameFlowManager>();
            Instance = null;
        }
    }
}