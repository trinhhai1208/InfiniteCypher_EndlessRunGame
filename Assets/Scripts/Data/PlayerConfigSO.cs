using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ thông số cấu hình của Player.
/// Cho phép điều chỉnh gameplay mà không cần chạm vào code.
/// Tạo asset: Right-click → Create → FutureCity → Player Config
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "FutureCity/Player Config")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("Speed")]
    [Tooltip("Tốc độ khởi đầu khi bắt đầu ván chơi")]
    public float baseSpeed = 12f;

    [Tooltip("Tốc độ tăng mỗi giây (FixedUpdate)")]
    public float speedIncreaseRate = 0.1f;

    [Tooltip("Tốc độ tối đa - không thể vượt qua giá trị này")]
    public float maxSpeed = 28f;

    [Header("Lane")]
    [Tooltip("Khoảng cách giữa các lane (phải khớp với LevelGenerator._laneDistance)")]
    public float laneDistance = 3.8f;

    [Tooltip("Tốc độ trượt ngang khi chuyển lane")]
    public float laneChangeSpeed = 15f;

    [Header("Jump & Physics")]
    [Tooltip("Lực bật lên khi nhảy")]
    public float jumpForce = 11f;

    [Tooltip("Trọng lực tự định nghĩa (không dùng Physics của Unity)")]
    public float gravity = 28f;

    [Tooltip("Lực đẩy xuống khi Dive (nhấn S khi đang trên không)")]
    public float diveForce = 25f;

    [Header("Roll (Lăn)")]
    [Tooltip("Thời gian lăn (giây)")]
    public float rollDuration = 0.8f;

    [Tooltip("Chiều cao Capsule Collider khi đang lăn")]
    public float rollColliderHeight = 1.2f;

    [Tooltip("Offset Z của tâm Collider khi lăn")]
    public float rollColliderCenterZ = 0f;

    [Header("Mobile Input")]
    [Tooltip("Khoảng cách tối thiểu (pixels) để nhận diện là Swipe")]
    public float minSwipeDistance = 45f;

    [Header("Stumble (Vấp Ngã)")]
    [Tooltip("% tốc độ bị giảm khi vấp (0.4 = mất 40% tốc độ)")]
    [Range(0f, 1f)]
    public float stumbleSpeedPenalty = 0.4f;

    [Tooltip("Tổng thời gian bị stun sau vấp (giây)")]
    public float stumbleDuration = 0.5f;

    [Tooltip("Thời gian freeze di chuyển thẳng đầu của vấp")]
    public float stumbleForwardFreezeTime = 0.2f;

    [Tooltip("Khoảng đẩy lùi theo Z khi vấp")]
    public float stumbleBackwardPush = 0.35f;

    [Tooltip("Khoảng đẩy ngang khi vấp (tránh xa vật cản)")]
    public float stumbleBackwardSidePush = 0.45f;
}
