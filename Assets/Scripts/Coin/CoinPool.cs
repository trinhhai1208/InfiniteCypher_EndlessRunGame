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
    private Mesh       _coinMesh;
    private Material   _coinMaterial;
    private bool       _isReady;

    private readonly List<Matrix4x4> _matrixCacheLeft  = new List<Matrix4x4>();
    private readonly List<Matrix4x4> _matrixCacheRight = new List<Matrix4x4>();

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
            // Debug.LogError("[CoinPool] Không load được Coin Prefab! Kiểm tra AssetReference.");
            return;
        }

        _loadedPrefab = op.Result;

        // Cache Mesh và Material để dùng cho GPU Instancing
        var mf = _loadedPrefab.GetComponentInChildren<MeshFilter>();
        var mr = _loadedPrefab.GetComponentInChildren<MeshRenderer>();
        if (mf != null) _coinMesh = mf.sharedMesh;
        if (mr != null) 
        {
            _coinMaterial = mr.sharedMaterial;
            // Tự động bật GPU Instancing để tránh lỗi InvalidOperationException
            if (_coinMaterial != null) _coinMaterial.enableInstancing = true;
        }

        // Pre-warm: tạo sẵn các instance và xếp vào pool
        for (int i = 0; i < _prewarmCount; i++)
        {
            var go = CreateNew();
            _inactive.Push(go);
        }

        _isReady = true;
        // Debug.Log($"[CoinPool] Sẵn sàng. Pre-warmed {_prewarmCount} coins.");
    }

    private void LateUpdate()
    {
        if (!_isReady || _active.Count == 0 || _coinMesh == null || _coinMaterial == null) return;

        bool isX2 = PowerUpManager.Instance != null && PowerUpManager.Instance.IsMultiplierActive();

        if (isX2)
        {
            _matrixCacheLeft.Clear();
            _matrixCacheRight.Clear();

            float spacing = 0.8f;
            foreach (var go in _active)
            {
                if (go == null || !go.activeInHierarchy) continue;

                // 1. Tắt Renderer thật (để vẽ 2 bản sao thay thế)
                var mr = go.GetComponentInChildren<MeshRenderer>();
                if (mr != null && mr.enabled) mr.enabled = false;

                // 2. Tính toán ma trận cho 2 bản sao
                Transform t = go.transform;
                Vector3 pos = t.position;
                Quaternion rot = t.rotation;
                Vector3 scale = t.lossyScale;

                Vector3 leftPos = pos - t.right * (spacing / 2f);
                Vector3 rightPos = pos + t.right * (spacing / 2f);

                _matrixCacheLeft.Add(Matrix4x4.TRS(leftPos, rot, scale));
                _matrixCacheRight.Add(Matrix4x4.TRS(rightPos, rot, scale));

                // Giới hạn 1023 bản thể cho mỗi lệnh DrawMeshInstanced
                if (_matrixCacheLeft.Count >= 1023) 
                {
                    Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheLeft);
                    Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheRight);
                    _matrixCacheLeft.Clear();
                    _matrixCacheRight.Clear();
                }
            }

            // Vẽ số còn lại
            if (_matrixCacheLeft.Count > 0)
            {
                Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheLeft);
                Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheRight);
            }
        }
        else
        {
            // Đảm bảo bật lại Renderer thật khi hết X2
            foreach (var go in _active)
            {
                if (go == null) continue;
                var mr = go.GetComponentInChildren<MeshRenderer>();
                if (mr != null && !mr.enabled) mr.enabled = true;
            }
        }
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
            // Debug.LogWarning("[CoinPool] Pool chưa sẵn sàng — prefab vẫn đang load.");
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
                // Debug.LogWarning($"[CoinPool] Vượt quá {_maxPoolSize} xu. Tự động nới rộng giới hạn thêm 50 xu để tránh lỗi mất đồ.");
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
