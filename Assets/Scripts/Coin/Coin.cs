using UnityEngine;

/// <summary>
/// Gắn lên Coin Prefab.
/// Yêu cầu:
///   - Collider với Is Trigger = true
///   - Tag = "Coin" (trên Prefab)
///   - Nhân vật có Tag = "Player"
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _despawnDistanceBack = 20f;
    [SerializeField] private float _magnetSpeed = 25f;
    [SerializeField] private float _magnetRadius = 10f;

    private bool _collected;
    private Transform _playerTransform;

    private void OnEnable()
    {
        _collected = false;
        if (PlayerController.Instance != null)
            _playerTransform = PlayerController.Instance.transform;
    }
    
    private void FixedUpdate()
    {
        if (_collected) return;
        if (_playerTransform == null) return;

        float playerZ = _playerTransform.position.z;

        // 1. Tối ưu: Tự trả về Pool nếu nhân vật đã đi qua 20m (Không cần đợi xóa Segment)
        if (transform.position.z < playerZ - _despawnDistanceBack)
        {
            if (CoinPool.Instance != null)
                CoinPool.Instance.Return(gameObject);
        }

        // 2. Xử lý Hút Nam Châm
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsMagnetActive())
        {
            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist < _magnetRadius)
            {
                // Hút về phía Player
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position + Vector3.up, _magnetSpeed * Time.fixedDeltaTime);
            }
        }
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
        }

        // Trả xu về pool thay vì Destroy
        if (CoinPool.Instance != null)
            CoinPool.Instance.Return(gameObject);
    }
}
