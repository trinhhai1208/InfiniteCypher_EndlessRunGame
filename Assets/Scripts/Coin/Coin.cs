using UnityEngine;

/// <summary>
/// Gắn lên Coin Prefab.
/// Yêu cầu:
///   - Collider với Is Trigger = true
///   - Tag = "Coin" (trên Prefab)
///   - Nhân vật có Tag = "Player"
///
/// P1 Optimization: Đã xóa FixedUpdate.
/// Logic Magnet giờ được điều khiển hoàn toàn bởi PowerUpManager (Player-driven).
/// Logic tự Despawn cũng được chuyển sang PowerUpManager để tránh mỗi xu gọi 1 Update.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _magnetSpeed = 25f;

    private bool _collected;
    private Transform _cachedTransform;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    private void OnEnable()
    {
        _collected = false;
    }

    /// <summary>
    /// Được gọi bởi PowerUpManager mỗi FixedUpdate khi Magnet đang active.
    /// Di chuyển xu về phía Player.
    /// </summary>
    public void AttractTo(Vector3 targetPosition, float deltaTime)
    {
        if (_collected) return;
        _cachedTransform.position = Vector3.MoveTowards(
            _cachedTransform.position,
            targetPosition + Vector3.up,
            _magnetSpeed * deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;

        // Chỉ nhân vật Player mới có thể thu thập xu
        if (!other.CompareTag("Player")) return;

        _collected = true;

        // Cộng điểm / xu vào GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin();
            // Phát âm thanh thu thập vàng
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        }

        // Trả xu về pool thay vì Destroy
        if (CoinPool.Instance != null)
            CoinPool.Instance.Return(gameObject);
    }
}
