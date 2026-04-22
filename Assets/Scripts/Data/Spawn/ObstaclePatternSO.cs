using System.Collections.Generic;
using UnityEngine;

namespace FutureCityEndlessRun.SpawnSystem
{
    /// <summary>
    /// Định nghĩa một kịch bản (Pattern) hoàn chỉnh để hệ thống Spawn gọi ra.
    /// ScriptableObject này cho phép Level Designer dễ dàng kéo thả và cấu hình màn chơi.
    /// </summary>
    [CreateAssetMenu(fileName = "NewObstaclePattern", menuName = "FutureCity/Spawn/Obstacle Pattern")]
    public class ObstaclePatternSO : ScriptableObject
    {
        [Header("Metadata")]
        public string PatternId;
        public SpawnPatternCategory Category;
        
        [Tooltip("Độ khó: 0 = Easy, 1 = Medium, 2 = Hard, 3 = Expert")]
        [Range(0, 3)]
        public int DifficultyTier = 0;
        
        [Tooltip("Trọng số để bốc thăm ngẫu nhiên (Càng lớn càng dễ ra).")]
        public float Weight = 1f;

        [Tooltip("Khoảng cách tối thiểu phải chạy trước khi Pattern này được phép lặp lại.")]
        public float CooldownDistance = 50f;

        [Tooltip("Ước lượng chiều dài (Z) của toàn bộ Pattern này để cộng thêm vào khoảng cách Spawn.")]
        public float LengthEstimate = 20f;

        [Header("Grammar & Rules")]
        public RequiredActionType RequiredAction;
        
        [Tooltip("Các làn đầu vào trống (an toàn) để tiếp cận Pattern này. Ví dụ: Nếu chỉ có làn giữa trống, mảng này nên là {0}.")]
        public int[] EntryLanes = new int[] { -1, 0, 1 };

        [Tooltip("Các làn đầu ra trống (an toàn) sau khi vượt qua Pattern này.")]
        public int[] ExitLanes = new int[] { -1, 0, 1 };

        [Tooltip("Nếu mảng này KHÔNG RỖNG, Pattern này CHỈ ĐƯỢC PHÉP spawn nếu Pattern TRƯỚC ĐÓ thuộc các nhóm này.")]
        public List<SpawnPatternCategory> AllowedPreviousCategories = new List<SpawnPatternCategory>();

        [Header("Obstacles Configuration")]
        [Tooltip("Danh sách các vật thể có trong Pattern này.")]
        public List<ObstacleCell> Cells = new List<ObstacleCell>();
        
        [Header("Validation (Editor Only)")]
        [TextArea(2, 4)]
        public string DesignerNote = "Ghi chú của người thiết kế...";

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(PatternId))
            {
                PatternId = name;
            }
        }
    }
}
