using System.Collections.Generic;
using UnityEngine;

namespace FutureCityEndlessRun.SpawnSystem
{
    /// <summary>
    /// Thư viện chứa tất cả các Pattern của dự án, được phân nhóm theo độ khó.
    /// LevelGenerator sẽ tham chiếu đến SO này để lấy Pattern ra spawn.
    /// </summary>
    [CreateAssetMenu(fileName = "PatternLibrary", menuName = "FutureCity/Spawn/Pattern Library")]
    public class PatternLibrarySO : ScriptableObject
    {
        [Header("Tier 0 - Easy (Tốc độ chậm)")]
        [Tooltip("Các pattern dễ, đường thông thoáng, nhảy lăn đơn giản.")]
        public List<ObstaclePatternSO> Tier0Patterns = new List<ObstaclePatternSO>();

        [Header("Tier 1 - Medium (Tốc độ vừa)")]
        [Tooltip("Chuyển làn nhanh, bắt đầu có combo.")]
        public List<ObstaclePatternSO> Tier1Patterns = new List<ObstaclePatternSO>();

        [Header("Tier 2 - Hard (Tốc độ cao)")]
        [Tooltip("Parkour, xe di chuyển.")]
        public List<ObstaclePatternSO> Tier2Patterns = new List<ObstaclePatternSO>();

        [Header("Tier 3 - Expert (Siêu khó)")]
        [Tooltip("Xe di chuyển ngược chiều chiếm nhiều làn, kết hợp parkour.")]
        public List<ObstaclePatternSO> Tier3Patterns = new List<ObstaclePatternSO>();

        [Header("Recovery Patterns (Bắt buộc chèn sau lúc khó)")]
        [Tooltip("Thường là làn rỗng hoặc chỉ có xu, giúp người chơi lấy lại nhịp.")]
        public List<ObstaclePatternSO> RecoveryPatterns = new List<ObstaclePatternSO>();

        /// <summary>
        /// API tiện ích lấy danh sách Pattern theo độ khó hiện tại
        /// </summary>
        public List<ObstaclePatternSO> GetPatternsByTier(int tier)
        {
            switch (tier)
            {
                case 0: return Tier0Patterns;
                case 1: return Tier1Patterns;
                case 2: return Tier2Patterns;
                case 3: return Tier3Patterns;
                default: return Tier3Patterns;
            }
        }
    }
}
