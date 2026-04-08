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
    [Header("Magnet Settings")]
    [SerializeField] private float _magnetSpeed = 25f;
    [SerializeField] private float _magnetRadius = 10f;
    
    private bool _collected;
    private Transform _playerTransform;
    
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _meshFilter = GetComponentInChildren<MeshFilter>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;
    }

    private void OnEnable()
    {
        // Reset trạng thái mỗi lần được lấy từ pool
        _collected = false;
        if (_playerTransform == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) _playerTransform = pObj.transform;
        }
    }

    private void Update()
    {
        if (_collected) return;

        // Xử lý Hút Nam Châm
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsMagnetActive() && _playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist < _magnetRadius)
            {
                // Hút về phía Player
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position + Vector3.up, _magnetSpeed * Time.deltaTime);
            }
        }

        // --- Xử lý Hình Ảnh Nhân Đôi xu ---
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsMultiplierActive())
        {
            // 1. Ẩn Renderer thật để tránh bị trùng lặp ở tâm
            if (_meshRenderer != null && _meshRenderer.enabled) _meshRenderer.enabled = false;

            // 2. Vẽ 2 đồng xu ảo đối xứng qua tâm
            if (_meshFilter != null && _meshRenderer != null)
            {
                float spacing = 0.8f; // Khoảng cách giữa 2 xu
                Vector3 leftPos = _meshFilter.transform.position - transform.right * (spacing / 2f);
                Vector3 rightPos = _meshFilter.transform.position + transform.right * (spacing / 2f);

                Matrix4x4 leftMatrix = Matrix4x4.TRS(leftPos, _meshFilter.transform.rotation, _meshFilter.transform.lossyScale);
                Matrix4x4 rightMatrix = Matrix4x4.TRS(rightPos, _meshFilter.transform.rotation, _meshFilter.transform.lossyScale);

                Graphics.DrawMesh(_meshFilter.sharedMesh, leftMatrix, _meshRenderer.sharedMaterial, 0);
                Graphics.DrawMesh(_meshFilter.sharedMesh, rightMatrix, _meshRenderer.sharedMaterial, 0);
            }
        }
        else
        {
            // Nếu không có X2, đảm bảo hiện lại Renderer thật
            if (_meshRenderer != null && !_meshRenderer.enabled) _meshRenderer.enabled = true;
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
