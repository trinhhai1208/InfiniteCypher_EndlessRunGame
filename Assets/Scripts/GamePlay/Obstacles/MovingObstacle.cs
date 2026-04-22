using UnityEngine;

/// <summary>
/// Gắn vào bất kỳ Obstacle nào (đặc biệt là xe bus) để khiến nó di chuyển ngược chiều player.
/// Khi xe ra khỏi tầm nhìn phía sau người chơi, tự động trả về AddressablePoolManager.
/// </summary>
public class MovingObstacle : MonoBehaviour
{
    [Tooltip("Tốc độ di chuyển ngược chiều Z (m/s). Dương = về phía người chơi.")]
    [SerializeField] private float _speed = 10f;

    [Tooltip("Khi khoảng cách Bus < player Z - giá trị này thì trả về Pool.")]
    [SerializeField] private float _despawnBehindDistance = 20f;

    private Transform _player;
    private bool _isActive = false;

    // Cache components hoặc dữ liệu cần thiết nếu sau này muốn mở rộng
    private void Awake()
    {
        // Có thể cache Rigidbody nếu muốn dùng physics, nhưng tạm thời dùng Transform
    }

    /// <summary>
    /// Khởi động xe di động sau khi spawn. Cần gọi hàm này trực tiếp nếu spawn thủ công,
    /// hoặc có thể tự tìm player trong OnEnable.
    /// </summary>
    public void Activate(float speed)
    {
        _speed = speed;
        _isActive = true;
        
        // Tìm player (Sử dụng PlayerController.Instance nếu có).
        // Cần đảm bảo PlayerController có một Instance.
        if (PlayerController.Instance != null)
        {
            _player = PlayerController.Instance.transform;
        }
    }

    private void OnEnable()
    {
        // Nếu muốn tự động tìm player, có thể thực hiện ở đây, 
        // nhưng tốt nhất là đợi Activate() được gọi để thiết lập speed đúng.
    }

    private void OnDisable()
    {
        _isActive = false;
        _player = null;
    }

    private void Update()
    {
        if (!_isActive || _player == null) return;

        // Di chuyển ngược chiều Z (về phía người chơi) mỗi frame
        transform.position += Vector3.back * (_speed * Time.deltaTime);

        // Despawn khi xe đi qua người chơi một khoảng _despawnBehindDistance
        if (_player.position.z - transform.position.z > _despawnBehindDistance)
        {
            _isActive = false;
            // Trả về pool. Địa chỉ AssetReference của nó có thể không có ở đây
            // Giải pháp: Có thể dùng EventBus hoặc cơ chế nào đó.
            // Nhưng đơn giản hơn: Trả về pool theo GameObject nếu AddressablePoolManager hỗ trợ.
            if (AddressablePoolManager.Instance != null)
            {
               ReturnChildCoins();
               AddressablePoolManager.Instance.Return(gameObject);
            }
            else
            {
                ReturnChildCoins();
                gameObject.SetActive(false);
            }
        }
    }

    private void ReturnChildCoins()
    {
        if (CoinPool.Instance == null) return;
        
        // Trả lại các xu đang là con (được gắn bởi BusSpawner.SpawnWithCoins)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<Coin>() != null)
            {
                CoinPool.Instance.Return(child.gameObject);
            }
        }
    }
}
