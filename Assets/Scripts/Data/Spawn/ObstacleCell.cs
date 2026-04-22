using System;
using UnityEngine;

namespace FutureCityEndlessRun.SpawnSystem
{
    /// <summary>
    /// Đơn vị cấu thành nhỏ nhất của một Pattern. Đại diện cho 1 vật cản tại 1 vị trí cụ thể.
    /// </summary>
    [Serializable]
    public class ObstacleCell
    {
        [Tooltip("Làn đường. -1: Trái, 0: Giữa, 1: Phải")]
        [Range(-1, 1)]
        public int Lane;

        [Tooltip("Khoảng cách Z tương đối so với điểm bắt đầu của Pattern.")]
        public float ZOffset;

        [Tooltip("Loại vật cản sẽ được spawn.")]
        public ObstacleType Type;

        [Tooltip("Có sinh xu phía trên/trước vật cản để dẫn đường không?")]
        public bool HasCoins;

        [Tooltip("Có sinh PowerUp (nếu có thể) không?")]
        public bool AllowPowerUp;
    }
}
