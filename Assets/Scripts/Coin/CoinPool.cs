using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Singleton Pool tái sử dụng GameObject xu (Coin).
/// Load prefab qua Addressables 1 lần duy nhất khi khởi động.
/// Dùng Stack<GameObject> thủ công để tương thích mọi phiên bản Unity.
///
/// P1 Optimization: Cache Transform và MeshRenderer ngay khi Coin được tạo,
/// tránh gọi GetComponentInChildren tốn kém mỗi frame trong LateUpdate.
/// </summary>
public class CoinPool : MonoBehaviour
{
    public static CoinPool Instance { get; private set; }

    // ─────────────────────────────────────────
    [Header("Settings")]
    [SerializeField] private AssetReference _coinAssetRef;
    [SerializeField] private int _prewarmCount = 30;
    [SerializeField] private int _maxPoolSize  = 200;
    [Tooltip("Số xu active tối đa. 0 = không giới hạn. Đặt 80 cho mobile WebGL để giảm draw call.")]
    [SerializeField] private int _activeCap = 0; // 0 = disabled

    // ─────────────────────────────────────────
    // P1: Struct cache — tránh GetComponent mỗi frame
    private struct CoinInstance
    {
        public GameObject Go;
        public Transform Tr;
        public MeshRenderer Mr;
        public Coin CoinScript; // P2: Cache Coin component — Zero-GC magnet path
    }

    private readonly Stack<CoinInstance>   _inactive = new();
    private readonly List<CoinInstance>    _active   = new(); 
    private readonly Dictionary<GameObject, int> _activeMap = new(); // O(1) lookup cho Return logic

    private GameObject _loadedPrefab;
    private Mesh       _coinMesh;
    private Material   _coinMaterial;
    private bool       _isReady;

    private readonly List<Matrix4x4> _matrixCacheLeft  = new();
    private readonly List<Matrix4x4> _matrixCacheRight = new();

    public bool IsReady => _isReady;

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
            if (_coinMaterial != null) _coinMaterial.enableInstancing = true;
        }

        // Pre-warm: tạo sẵn các instance và xếp vào pool
        for (int i = 0; i < _prewarmCount; i++)
        {
            _inactive.Push(CreateNew());
        }

        _isReady = true;
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

            // P1: Dùng index loop thay vì foreach để tránh boxing
            for (int i = 0; i < _active.Count; i++)
            {
                var ci = _active[i];
                if (ci.Go == null || !ci.Go.activeInHierarchy) continue;

                // P1: Dùng cached MeshRenderer thay vì GetComponentInChildren mỗi frame
                if (ci.Mr != null && ci.Mr.enabled) ci.Mr.enabled = false;

                // P1: Dùng cached Transform thay vì go.transform
                Vector3 pos    = ci.Tr.position;
                Quaternion rot = ci.Tr.rotation;
                Vector3 scale  = ci.Tr.lossyScale;

                Vector3 leftPos  = pos - ci.Tr.right * (spacing / 2f);
                Vector3 rightPos = pos + ci.Tr.right * (spacing / 2f);

                _matrixCacheLeft.Add(Matrix4x4.TRS(leftPos, rot, scale));
                _matrixCacheRight.Add(Matrix4x4.TRS(rightPos, rot, scale));

                if (_matrixCacheLeft.Count >= 1023)
                {
                    Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheLeft);
                    Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheRight);
                    _matrixCacheLeft.Clear();
                    _matrixCacheRight.Clear();
                }
            }

            if (_matrixCacheLeft.Count > 0)
            {
                Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheLeft);
                Graphics.DrawMeshInstanced(_coinMesh, 0, _coinMaterial, _matrixCacheRight);
            }
        }
        else
        {
            // Đảm bảo bật lại Renderer thật khi hết X2
            for (int i = 0; i < _active.Count; i++)
            {
                var ci = _active[i];
                if (ci.Go == null) continue;
                // P1: Dùng cached MeshRenderer
                if (ci.Mr != null && !ci.Mr.enabled) ci.Mr.enabled = true;
            }
        }
    }

    // ─────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────

    private CoinInstance CreateNew()
    {
        var go = Instantiate(_loadedPrefab);
        go.SetActive(false);

        // P1+P2: Cache ngay khi tạo — chỉ gọi GetComponent 1 lần duy nhất cho mỗi instance
        return new CoinInstance
        {
            Go = go,
            Tr = go.transform,
            Mr = go.GetComponentInChildren<MeshRenderer>(),
            CoinScript = go.GetComponent<Coin>() // P2: Cache Coin
        };
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
        if (!_isReady) return null;

        CoinInstance ci;
        if (_inactive.Count > 0)
            ci = _inactive.Pop();
        else
        {
            if (_active.Count >= _maxPoolSize)
                _maxPoolSize += 50;
            ci = CreateNew();
        }

        // P2: Active Cap — giới hạn số xu active trên mobile. Inspector: _activeCap > 0 = bật.
        if (_activeCap > 0 && _active.Count >= _activeCap) return null;

        ci.Tr.SetParent(parent);
        ci.Tr.SetPositionAndRotation(position, rotation);
        ci.Go.SetActive(true);
        
        // Luôn trả về index nơi vật thể vừa được Add vào List
        _activeMap[ci.Go] = _active.Count;
        _active.Add(ci);
        
        return ci.Go;
    }

    /// <summary>
    /// Trả 1 xu về pool khi nhân vật ăn xu hoặc segment cleanup.
    /// </summary>
    public void Return(GameObject go)
    {
        if (go == null) return;
        if (!_activeMap.TryGetValue(go, out int index)) return; // O(1) check

        CoinInstance ci = _active[index];

        // 🔄 Swap-and-Pop pattern: Hoán đổi phần tử hiện tại với phần tử cuối cùng của list
        // Điều này giúp việc RemoveAt(last) đạt O(1) và giữ cho List luôn dầy đặc.
        int lastIndex = _active.Count - 1;
        if (index != lastIndex)
        {
            CoinInstance lastCI = _active[lastIndex];
            _active[index] = lastCI;
            _activeMap[lastCI.Go] = index; // Cập nhật map cho phần tử vừa được di dời
        }

        _active.RemoveAt(lastIndex);
        _activeMap.Remove(go);

        // Reset trạng thái
        go.SetActive(false);
        ci.Tr.SetParent(null);
        if (ci.Mr != null && !ci.Mr.enabled) ci.Mr.enabled = true;

        _inactive.Push(ci);
    }

    /// <summary>
    /// Trả toàn bộ xu đang active về pool (dùng khi restart).
    /// </summary>
    public void ReturnAll()
    {
        // Snapshot để tránh modify list trong loop
        var snapshot = new List<CoinInstance>(_active);
        foreach (var ci in snapshot)
            Return(ci.Go);
    }

    /// <summary>
    /// P2: Hút tất cả xu trong bán kính về phía Player.
    /// Duyệt trực tiếp _active list — không cần OverlapSphere hay GetComponent.
    /// radiusSq: Bình phương bán kính hút (tránh sqrt tốn CPU).
    /// </summary>
    public void AttractNearbyCoins(Vector3 playerPos, float radiusSq, float deltaTime)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            var ci = _active[i];
            if (ci.Go == null || !ci.Go.activeInHierarchy) continue;

            float distSq = (ci.Tr.position - playerPos).sqrMagnitude;
            if (distSq <= radiusSq)
            {
                // P2: Dùng cached CoinScript — Zero-GC, không cần GetComponent
                ci.CoinScript?.AttractTo(playerPos, deltaTime);
            }
        }
    }


    private void OnDestroy()
    {
        if (_loadedPrefab != null)
            Addressables.Release(_loadedPrefab);
    }
}
