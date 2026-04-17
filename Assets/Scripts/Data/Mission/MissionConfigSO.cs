using UnityEngine;

[System.Serializable]
public class MissionScopeConfig
{
    public string Title;
    [Tooltip("Dùng {0} để đại diện cho mục tiêu cần đạt, {1} để đại diện cho phần thưởng. Vd: Thu thập {0} vàng.")]
    [TextArea] public string DescriptionTemplate;
    public int BaseGoal = 100;
    [Tooltip("Hệ số nhân độ khó mỗi khi lên cấp. Vd: 2.0 -> Cấp 1: 100, Cấp 2: 200, Cấp 3: 400")]
    public float GoalMultiplier = 2f;
    public Sprite Icon;
}

[CreateAssetMenu(fileName = "MissionConfig_", menuName = "FutureCity/Mission Config")]
public class MissionConfigSO : ScriptableObject
{
    public MissionType Type;
    
    [Header("Single Run (Trong 1 ván)")]
    public MissionScopeConfig SingleRun;

    [Header("Total (Tổng tích lũy)")]
    public MissionScopeConfig Total;

    /// <summary>
    /// Tính toán Mục tiêu (Goal) dựa trên Tier (Cấp độ). Tier bắt đầu từ 1.
    /// </summary>
    public int GetGoal(MissionScope scope, int tier)
    {
        var config = scope == MissionScope.SingleRun ? SingleRun : Total;
        // Công thức: BaseGoal * (Multiplier ^ (tier - 1))
        // Vd: Tier 1 -> Base * 1. Tier 2 -> Base * Multiplier
        return Mathf.RoundToInt(config.BaseGoal * Mathf.Pow(config.GoalMultiplier, tier - 1));
    }

    /// <summary>
    /// Tính toán Phần thưởng dựa trên Mục tiêu (Theo yêu cầu: Thưởng = Mục tiêu / 10)
    /// </summary>
    public int GetReward(int goal)
    {
        return goal / 10;
    }

    public string GetDescription(MissionScope scope, int goal, int reward)
    {
        var config = scope == MissionScope.SingleRun ? SingleRun : Total;
        if (string.IsNullOrEmpty(config.DescriptionTemplate)) return "";
        return string.Format(config.DescriptionTemplate, goal, reward);
    }
}
