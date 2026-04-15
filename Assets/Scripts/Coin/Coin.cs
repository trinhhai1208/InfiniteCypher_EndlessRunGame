using UnityEngine;

/// <summary>
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

        // Nếu xu vẫn còn được parent bởi segment, tách ra khỏi segment ngay khi bắt đầu bị hút
        // để tránh trường hợp segment bị despawn và vô tình trả lại xu dù nó đang bay về Player.
        if (_cachedTransform.parent != null)
        {
            _cachedTransform.SetParent(null);
        }
        
        // Tốc độ hút mặc định 25f trên Inspector là QUÁ CHẬM nếu Player max đang chạy 28f.
        // Cần ép tốc độ tối thiểu phải lớn hơn tốc độ player để đuổi kịp.
        float actualSpeed = Mathf.Max(_magnetSpeed, 80f);

        // NẾU coin bị rớt ra phía sau (hoặc ngang hàng chậm nhịp), tăng tốc độ gấp 3 lần 
        // để nó bay vụt bay vào lưng Player ngay lập tức (Không bị Miss, Không rồng rắn)
        if (_cachedTransform.position.z <= targetPosition.z)
        {
            actualSpeed *= 3f;
        }

        _cachedTransform.position = Vector3.MoveTowards(
            _cachedTransform.position,
            targetPosition + Vector3.up,
            actualSpeed * deltaTime);
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
