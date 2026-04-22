namespace FutureCityEndlessRun.SpawnSystem
{
    /// <summary>
    /// Các nhóm Pattern để phân loại và kiểm soát luồng.
    /// </summary>
    public enum SpawnPatternCategory
    {
        Safe,               // Không có vật cản nguy hiểm, thường chứa xu dẫn đường
        LaneChange,         // Buộc người chơi đổi làn (Zigzag)
        Jump,               // Buộc nhảy qua vật cản thấp (Car, Barrier thấp)
        Roll,               // Buộc lăn qua vật cản cao (Barrier cao)
        TopRun,             // Nhảy lên và chạy trên nóc xe
        Parkour,            // Chuỗi thao tác phức tạp (xe bàn đạp, cầu xu)
        MovingThreat,       // Xe di chuyển ngược chiều
        Combo,              // Kết hợp nhiều thao tác (Nhảy + Lăn)
        Recovery            // Pattern hồi phục sau đoạn khó (Thường là Safe + Xu)
    }

    /// <summary>
    /// Hành động yêu cầu từ người chơi để vượt qua Pattern.
    /// </summary>
    public enum RequiredActionType
    {
        None,
        MoveLeft,
        MoveRight,
        Jump,
        Roll,
        JumpThenRoll,
        JumpChain,
        TopLanding
    }

    /// <summary>
    /// Loại vật cản cơ bản (Mapping với Spawner).
    /// </summary>
    public enum ObstacleType
    {
        None,
        BarrierLow,         // Nhảy
        BarrierHigh,        // Lăn
        StaticCar,          // Nhảy
        StaticBus,          // Chặn hẳn (hoặc nhảy lên nóc)
        MovingCar,          // Xe con di chuyển
        MovingBus,          // Xe bus di chuyển
        SteppingCar         // Xe tĩnh nằm san sát nhau để làm bàn đạp parkour
    }
}
