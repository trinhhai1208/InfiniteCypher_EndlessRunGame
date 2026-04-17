using UnityEngine;

public enum MissionType
{
    Distance,      // Chạy quãng đường X
    Coins,         // Gom X vàng
    BarrierRoll,   // Lướt qua rào chắn
    ObstacleJump,  // Nhảy qua vật cản
    VehicleRun     // Đi trên nóc xe
}

public enum MissionScope
{
    SingleRun,     // Yêu cầu làm trong 1 ván chơi duy nhất
    Total          // Cộng dồn qua nhiều ván chơi
}
