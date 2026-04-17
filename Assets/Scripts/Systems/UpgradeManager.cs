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
    /// Lấy Cấp độ hiện tại của một loại PowerUp (Mặc định 0 = chưa nâng cấp).
    /// </summary>
    public int GetLevel(PowerUpType type)
    {
        return PlayerPrefs.GetInt("Upgrade_" + type.ToString(), 0);
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
    /// Trả về thời gian dựa trên level nâng cấp. Trả về 10s mặc định nếu là Lv0.
    /// </summary>
    public float GetDuration(PowerUpType type)
    {
        int level = GetLevel(type);
        
        // Nếu chưa nâng cấp (Lv0), trả về thời gian cơ bản là 10 giây
        if (level <= 0) return 10f; 

        var config = GetConfig(type);
        if (config == null || level > config.Tiers.Length) return 10f;
        
        return config.Tiers[level - 1].Duration;
    }

    /// <summary>
    /// Trả về chỉ số phụ (Vd Bán kính). Trả về giá trị cơ bản nếu Lv0.
    /// </summary>
    public float GetSecondaryValue(PowerUpType type)
    {
        int level = GetLevel(type);

        // Giá trị cơ bản cho Lv0 (Vd bán kính Nam châm là 7m)
        if (level <= 0) return 7f;

        var config = GetConfig(type);
        if (config == null || level > config.Tiers.Length) return 7f; 

        return config.Tiers[level - 1].SecondaryValue;
    }

    /// <summary>
    /// Thực hiện mua nâng cấp bằng TotalGold. 
    /// Trả về true nếu thành công, false nếu không đủ tiền/MAX cấp.
    /// </summary>
    public bool TryUpgrade(PowerUpType type)
    {
        int level = GetLevel(type);
        var config = GetConfig(type);
        
        if (config == null) 
        {
            return false;
        }

        if(level >= config.Tiers.Length) 
        {
            return false;
        }

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
