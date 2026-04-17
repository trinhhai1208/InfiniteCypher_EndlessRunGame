public interface IEvent { }
public struct PlayerStumbleEvent : IEvent { }
public struct PlayerJumpEvent : IEvent { }
public struct CoinCollectedEvent : IEvent { public int Count; }
public struct GameOverEvent : IEvent { }
public struct GameStartEvent : IEvent { }
public struct DistanceChangedEvent : IEvent { public int Distance; }

// Mission Events
public struct PlayerBarrierRollEvent : IEvent { } // Lướt thành công qua rào (bên dưới)
public struct PlayerObstacleJumpEvent : IEvent { } // Nhảy thành công qua vật cản
public struct PlayerVehicleRunEvent : IEvent { } // Người chơi chạy trên xe