using UnityEngine;

/// <summary>
/// Định nghĩa kiểu hành vi của vật cản khi va chạm với Player.
/// Gắn script này vào ROOT của Obstacle Prefab.
///
/// Mapping đề xuất:
///   - Xe (Car, Bus, Truck)  -> VehicleStumble
///   - Rào chắn, Tường, Block -> Fatal
///   - Platform có thể đứng lên -> Fatal + AllowTopLanding = true
///     hoặc JumpableTop nếu chỉ cần bỏ qua va chạm từ trên
/// </summary>

public enum ObstacleCollisionType
{
    Fatal,          // Va chạm bất kỳ hướng nào -> Chết ngay
    VehicleStumble, // Va chạm ngang -> Vấp (Boss xuất hiện); đâm thẳng -> Chết
    JumpableTop     // Va chạm từ trên -> Bỏ qua (Player đứng được); còn lại -> Chết
}

public class ObstacleIdentity : MonoBehaviour
{
    [Header("Collision Type")]
    [SerializeField] private ObstacleCollisionType _collisionType = ObstacleCollisionType.Fatal;

    [Header("Options")]
    [Tooltip("Nếu bật, Player nhảy lên đứng được mà không chết. Áp dụng cho xe, platform, v.v.")]
    [SerializeField] private bool _allowTopLanding = false;

    public ObstacleCollisionType CollisionType => _collisionType;
    public bool AllowTopLanding => _allowTopLanding;
}
