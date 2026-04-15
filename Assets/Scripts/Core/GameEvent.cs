public interface IEvent { }
public struct PlayerStumbleEvent : IEvent { }
public struct PlayerJumpEvent : IEvent { }
public struct CoinCollectedEvent : IEvent { public int Count; }
public struct GameOverEvent : IEvent { }
public struct DistanceChangedEvent : IEvent { public int Distance; }