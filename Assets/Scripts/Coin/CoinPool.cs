using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Singleton Pool tái sử dụng GameObject xu (Coin).
/// Load prefab qua Addressables 1 lần duy nhất khi khởi động.
/// Dùng Stack<GameObject> thủ công để tương thích mọi phiên bản Unity.
/// </summary>
public class CoinPool : MonoBehaviour
{
    public static CoinPool Instance { get; private set; }

    // ─────────────────────────────────────────
    [Header("Settings")]
    [Tooltip("AssetReference của Coin Prefab")]
    [SerializeField] private AssetReference _coinAssetRef;
    [Tooltip("Số xu tạo sẵn khi khởi động (tránh lag frame đầu)")]
    [SerializeField] private int _prewarmCount = 30;
    [Tooltip("Số xu tối đa pool giữ trong bộ nhớ")]
    [SerializeField] private int _maxPoolSize  = 100;

    // ─────────────────────────────────────────
    private readonly Stack<GameObject>   _inactive = new Stack<GameObject>(); // đang chờ trong pool
    private readonly HashSet<GameObject> _active   = new HashSet<GameObject>(); // đang được dùng

    private GameObject _loadedPrefab;
    private bool       _isReady;

    public bool IsReady => _isReady; // Expose ra ngoài để LevelGenerator kiểm tra

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var op = Addressables.LoadAssetAsync<GameObject>(_coinAssetRef);
        op.Completed += OnPrefabLoaded;
    }

    private void OnPrefabLoaded(AsyncOperationHandle<GameObject> op)
    {
        if (op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[CoinPool] Không load được Coin Prefab! Kiểm tra AssetReference.");
            return;
        }

        _loadedPrefab = op.Result;

        // Pre-warm: tạo sẵn các instance và xếp vào pool
        for (int i = 0; i < _prewarmCount; i++)
        {
            var go = CreateNew();
            _inactive.Push(go);
        }

        _isReady = true;
        Debug.Log($"[CoinPool] Sẵn sàng. Pre-warmed {_prewarmCount} coins.");
    }

    // ─────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────

    private GameObject CreateNew()
    {
        var go = Instantiate(_loadedPrefab);
        go.SetActive(false);
        return go;
    }

    // ─────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────

    /// <summary>
    /// Lấy 1 xu từ pool, đặt tại vị trí/rotation/parent chỉ định.
    /// Trả về null nếu pool chưa sẵn sàng.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!_isReady)
        {
            Debug.LogWarning("[CoinPool] Pool chưa sẵn sàng — prefab vẫn đang load.");
            return null;
        }

        // Lấy từ stack, tạo mới nếu hết
        GameObject go;
        if (_inactive.Count > 0)
            go = _inactive.Pop();
        else
        {
            // Tự động nới rộng hồ bơi (Pool) thay vì chặn lại làm mất xu
            if (_active.Count >= _maxPoolSize)
            {
                Debug.LogWarning($"[CoinPool] Vượt quá {_maxPoolSize} xu. Tự động nới rộng giới hạn thêm 50 xu để tránh lỗi mất đồ.");
                _maxPoolSize += 50; 
            }
            go = CreateNew();
        }

        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        _active.Add(go);
        return go;
    }

    /// <summary>
    /// Trả 1 xu về pool khi nhân vật ăn xu hoặc segment cleanup.
    /// </summary>
    public void Return(GameObject go)
    {
        if (go == null) return;
        if (!_active.Contains(go)) return; // tránh return trùng

        _active.Remove(go);
        go.SetActive(false);
        go.transform.SetParent(null); // tách khỏi segment
        _inactive.Push(go);
    }

    /// <summary>
    /// Trả toàn bộ xu đang active về pool (dùng khi restart).
    /// </summary>
    public void ReturnAll()
    {
        var snapshot = new List<GameObject>(_active);
        foreach (var go in snapshot)
            Return(go);
    }

    private void OnDestroy()
    {
        if (_loadedPrefab != null)
            Addressables.Release(_loadedPrefab);
    }
}
