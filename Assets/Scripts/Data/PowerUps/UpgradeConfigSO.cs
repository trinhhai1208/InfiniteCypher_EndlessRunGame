using System;
using UnityEngine;

[Serializable]
public class UpgradeTier
{
    [Tooltip("Tiền nâng cấp (0 là đã mở khóa sẵn ở Level 1)")]
    public int Cost;
    
    [Tooltip("Thời gian tác dụng chính")]
    public float Duration;

    [Tooltip("Chỉ số phụ (Vd: Bán kính hút của Magnet)")]
    public float SecondaryValue;
}

[CreateAssetMenu(fileName = "NewUpgradeConfig", menuName = "FutureCity/Upgrade Config")]
public class UpgradeConfigSO : ScriptableObject
{
    [Tooltip("Tên hiển thị trong Shop")]
    public string DisplayName;

    [Tooltip("Loại PowerUp dùng cho config này")]
    public PowerUpType Type;

    [Tooltip("Danh sách 5 mốc nâng cấp (Index 0 = Lvl 1, Index 4 = Lvl 5)")]
    public UpgradeTier[] Tiers = new UpgradeTier[5];

    [TextArea]
    public string Description;
    
    public Sprite Icon;

    /// <summary>
    /// Lấy cấu hình của một Level (Truyền vào Level = 1..5)
    /// </summary>
    public UpgradeTier GetTier(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, Tiers.Length - 1);
        return Tiers[index];
    }
}
