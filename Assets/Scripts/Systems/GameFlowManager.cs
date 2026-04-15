using UnityEngine;
using System;

public enum GameState { Loadout, Playing, Paused, GameOver }

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Loadout;

    // Events
    public event Action OnGameStart;
    public event Action OnGameOver;

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

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        OnGameStart?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        CurrentState = GameState.GameOver;
        OnGameOver?.Invoke();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<GameFlowManager>();
    }
}