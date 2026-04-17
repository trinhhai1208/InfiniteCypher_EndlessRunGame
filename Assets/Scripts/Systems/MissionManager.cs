using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionStatus
{
    Active,
    Completed // Done but not claimed yet. After claim -> Active (next tier)
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private List<MissionConfigSO> _configs;

    private Dictionary<MissionType, MissionConfigSO> _configDict;

    private void Awake()
    {
        // Singleton pattern: Nếu đã có Instance cũ (từ scene trước chưa được dọn sạch)
        // mà Instance đó trỏ đến một object đã bị hủy (null in Unity), thì reset nó.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        ServiceLocator.Register<MissionManager>(this);

        _configDict = new Dictionary<MissionType, MissionConfigSO>();
        foreach (var c in _configs)
        {
            if (c != null && !_configDict.ContainsKey(c.Type))
                _configDict.Add(c.Type, c);
        }
    }

    private void OnDestroy()
    {
        // Quan trọng: Dọn dẹp ServiceLocator khi object bị hủy (đổi scene)
        if (Instance == this)
        {
            ServiceLocator.Unregister<MissionManager>();
            Instance = null;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStartEvent>(OnGameStart);
        EventBus.Subscribe<DistanceChangedEvent>(OnDistance);
        EventBus.Subscribe<CoinCollectedEvent>(OnCoin);
        EventBus.Subscribe<PlayerBarrierRollEvent>(OnRoll);
        EventBus.Subscribe<PlayerObstacleJumpEvent>(OnJump);
        EventBus.Subscribe<PlayerVehicleRunEvent>(OnVehicle);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStartEvent>(OnGameStart);
        EventBus.Unsubscribe<DistanceChangedEvent>(OnDistance);
        EventBus.Unsubscribe<CoinCollectedEvent>(OnCoin);
        EventBus.Unsubscribe<PlayerBarrierRollEvent>(OnRoll);
        EventBus.Unsubscribe<PlayerObstacleJumpEvent>(OnJump);
        EventBus.Unsubscribe<PlayerVehicleRunEvent>(OnVehicle);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    // --- State Management ---
    private string GetKey(MissionType type, MissionScope scope, string prop)
    {
        return $"Mission_{type}_{scope}_{prop}";
    }

    public int GetTier(MissionType type, MissionScope scope)
    {
        return PlayerPrefs.GetInt(GetKey(type, scope, "Tier"), 1); // Default tier 1
    }

    public int GetProgress(MissionType type, MissionScope scope)
    {
        return PlayerPrefs.GetInt(GetKey(type, scope, "Progress"), 0);
    }

    public MissionStatus GetStatus(MissionType type, MissionScope scope)
    {
        return (MissionStatus)PlayerPrefs.GetInt(GetKey(type, scope, "Status"), 0);
    }

    private void SetTier(MissionType type, MissionScope scope, int val)
    {
        PlayerPrefs.SetInt(GetKey(type, scope, "Tier"), val);
        PlayerPrefs.Save();
    }

    private void SetProgress(MissionType type, MissionScope scope, int val)
    {
        PlayerPrefs.SetInt(GetKey(type, scope, "Progress"), val);
        // Ép lưu ngay lập tức để tránh mất tiến trình khi reload/crash
        PlayerPrefs.Save(); 
    }

    private void OnStatusChanged(MissionType type, MissionScope scope, MissionStatus val)
    {
        PlayerPrefs.SetInt(GetKey(type, scope, "Status"), (int)val);
        PlayerPrefs.Save();
    }

    public MissionConfigSO GetConfig(MissionType type)
    {
        if (_configDict != null && _configDict.TryGetValue(type, out var c)) return c;
        return null;
    }

    public List<MissionType> GetAllAvailableTypes()
    {
        return new List<MissionType>(_configDict.Keys);
    }

    // --- Progress Update Logic ---

    // Biến tạm để nạp tiến độ SingleRun
    private float _lastDistanceSent = 0;
    private int _lastCoinSent = 0;

    private void OnGameStart(GameStartEvent e)
    {
        _lastDistanceSent = 0;
        _lastCoinSent = 0;

        // Reset progress cho toàn bộ SingleRun nếu nó đang Active
        foreach (var type in _configDict.Keys)
        {
            if (GetStatus(type, MissionScope.SingleRun) == MissionStatus.Active)
            {
                SetProgress(type, MissionScope.SingleRun, 0);
            }
        }
        PlayerPrefs.Save();
    }

    private void OnDistance(DistanceChangedEvent e)
    {
        int delta = e.Distance - (int)_lastDistanceSent;
        if (delta > 0)
        {
            AddProgress(MissionType.Distance, MissionScope.Total, delta);
            SetProgressAbsolute(MissionType.Distance, MissionScope.SingleRun, e.Distance);
            _lastDistanceSent = e.Distance;
        }
    }

    private void OnCoin(CoinCollectedEvent e)
    {
        int delta = e.Count - _lastCoinSent;
        if (delta > 0)
        {
            AddProgress(MissionType.Coins, MissionScope.Total, delta);
            SetProgressAbsolute(MissionType.Coins, MissionScope.SingleRun, e.Count);
            _lastCoinSent = e.Count;
        }
    }

    private void OnRoll(PlayerBarrierRollEvent e)
    {
        AddProgress(MissionType.BarrierRoll, MissionScope.SingleRun, 1);
        AddProgress(MissionType.BarrierRoll, MissionScope.Total, 1);
    }

    private void OnJump(PlayerObstacleJumpEvent e)
    {
        AddProgress(MissionType.ObstacleJump, MissionScope.SingleRun, 1);
        AddProgress(MissionType.ObstacleJump, MissionScope.Total, 1);
    }

    private void OnVehicle(PlayerVehicleRunEvent e)
    {
        AddProgress(MissionType.VehicleRun, MissionScope.SingleRun, 1);
        AddProgress(MissionType.VehicleRun, MissionScope.Total, 1);
    }

    private void OnGameOver(GameOverEvent e)
    {
        PlayerPrefs.Save(); // Save everything
    }

    /// <summary>
    /// Cộng thêm tiến trình
    /// </summary>
    private void AddProgress(MissionType type, MissionScope scope, int amount)
    {
        if (GetStatus(type, scope) != MissionStatus.Active) return;
        
        int current = GetProgress(type, scope);
        SetProgressAbsolute(type, scope, current + amount);
    }

    /// <summary>
    /// Gán trực tiếp tiến trình
    /// </summary>
    private void SetProgressAbsolute(MissionType type, MissionScope scope, int absoluteAmount)
    {
        if (GetStatus(type, scope) != MissionStatus.Active) return;

        var config = GetConfig(type);
        if (config == null) return;

        int tier = GetTier(type, scope);
        int goal = config.GetGoal(scope, tier);

        if (absoluteAmount >= goal)
        {
            SetProgress(type, scope, goal);
            OnStatusChanged(type, scope, MissionStatus.Completed);
        }
        else
        {
            SetProgress(type, scope, absoluteAmount);
        }
    }

    // --- User Actions ---
    public bool ClaimReward(MissionType type, MissionScope scope)
    {
        if (GetStatus(type, scope) != MissionStatus.Completed) return false;

        var config = GetConfig(type);
        if (config == null) return false;

        int tier = GetTier(type, scope);
        int goal = config.GetGoal(scope, tier);
        int reward = config.GetReward(goal);

        // Add Gold
        int totalGold = PlayerPrefs.GetInt("TotalGold", 0);
        totalGold += reward;
        PlayerPrefs.SetInt("TotalGold", totalGold);

        // Reset for next tier
        SetTier(type, scope, tier + 1);
        SetProgress(type, scope, 0); // Reset tiến trình về 0
        OnStatusChanged(type, scope, MissionStatus.Active);

        PlayerPrefs.Save();
        return true;
    }
}
