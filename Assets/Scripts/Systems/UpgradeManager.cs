using System;
using UnityEngine;

/// <summary>
/// Quản lý dữ liệu mua sắm và cấp độ của các PowerUp.
/// Lưu trữ xuống PlayerPrefs để duy trì tiến trình (Persistence).
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Config References")]
    [Tooltip("Gắn các Config SO đã tạo vào đây (Magnet, Shield, Multiplier...)")]
    public UpgradeConfigSO[] Configs;

    public event Action<PowerUpType, int> OnUpgradeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ qua các Scene
        
        ServiceLocator.Register<UpgradeManager>(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<UpgradeManager>();
        }
    }

    /// <summary>
    /// Lấy Cấp độ hiện tại của một loại PowerUp (Mặc định 1, Tối đa 5).
    /// </summary>
    public int GetLevel(PowerUpType type)
    {
        return PlayerPrefs.GetInt("Upgrade_" + type.ToString(), 1);
    }

    /// <summary>
    /// Lấy Cấu hình (ConfigSO) của một loại PowerUp.
    /// </summary>
    public UpgradeConfigSO GetConfig(PowerUpType type)
    {
        if (Configs == null) return null;
        foreach (var c in Configs)
        {
            if (c != null && c.Type == type) return c;
        }
        return null;
    }

    /// <summary>
    /// Trả về thời gian Max Duration dựa trên level đã nâng cấp của người chơi.
    /// </summary>
    public float GetDuration(PowerUpType type)
    {
        int level = GetLevel(type);
        var config = GetConfig(type);
        if (config == null) return 10f; // dự phòng
        
        return config.GetTier(level).Duration;
    }

    /// <summary>
    /// Trả về chỉ số phụ (ví dụ Bán kính Magnet).
    /// </summary>
    public float GetSecondaryValue(PowerUpType type)
    {
        int level = GetLevel(type);
        var config = GetConfig(type);
        if (config == null) return 10f; 

        return config.GetTier(level).SecondaryValue;
    }

    /// <summary>
    /// Thực hiện mua nâng cấp bằng TotalGold. 
    /// Trả về true nếu thành công, false nếu không đủ tiền/MAX cấp.
    /// </summary>
    public bool TryUpgrade(PowerUpType type)
    {
        int level = GetLevel(type);
        if (level >= 5) return false; // Max Level

        var config = GetConfig(type);
        if (config == null) return false;

        // Muốn lên cấp tiếp theo, cần trả phí của cấp đó (Index là level hiện tại vì Index chạy từ 0)
        int cost = config.Tiers[level].Cost;
        
        // Đọc tổng vàng (Lấy source truth từ PlayerPrefs)
        int totalGold = PlayerPrefs.GetInt("TotalGold", 0);

        if (totalGold >= cost)
        {
            // Trừ vàng và Lưu
            totalGold -= cost;
            PlayerPrefs.SetInt("TotalGold", totalGold);
            
            // Tăng level và Lưu
            level++;
            PlayerPrefs.SetInt("Upgrade_" + type.ToString(), level);
            
            // Ép đĩa ngay lập tức để không mất tiền oan nếu crash
            PlayerPrefs.Save();

            // Notify các nơi cần biết
            OnUpgradeChanged?.Invoke(type, level);
            return true;
        }

        return false;
    }
}
