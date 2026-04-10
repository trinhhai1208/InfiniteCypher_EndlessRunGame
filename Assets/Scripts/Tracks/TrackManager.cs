using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Quản lý việc sinh (spawn) và xóa (release) các đoạn đường bằng Addressables.
/// Luôn giữ _segmentsAhead đoạn đường phía trước nhân vật.
/// Kết hợp với LevelGenerator để tự động sinh obstacles và coins.
/// </summary>
public class TrackManager : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform _player;

    [Header("Level Generator")]
    [SerializeField] private LevelGenerator _levelGenerator;

    [Header("Track Segment Addressable References")]
    [Tooltip("Danh sách các TrackSegment prefab. Script sẽ chọn ngẫu nhiên từ đây.")]
    [SerializeField] private List<AssetReference> _segmentAssetRefs = new();

    [Header("Track Settings")]
    [Tooltip("Số đoạn đường luôn tồn tại phía trước nhân vật")]
    [SerializeField] private int _segmentsAhead = 4;

    [Tooltip("Khoảng cách phía sau nhân vật để xóa segment cũ")]
    [SerializeField] private float _despawnDistance = 70f;

    // Runtime state
    private readonly List<TrackSegment> _activeSegments = new();
    private Vector3 _nextSpawnPosition = Vector3.zero;
    private bool _isSpawning;
    private int _totalSpawned; 
    
    public bool IsReady { get; private set; } = false; // Báo hiệu đã load xong đoạn đường đầu tiên

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_levelGenerator == null)
            _levelGenerator = GetComponent<LevelGenerator>();

        // Đặt điểm spawn đầu tiên lùi lại 20m để che hụt chân camera
        _nextSpawnPosition = _player != null ? _player.position - Vector3.forward * 20f : new Vector3(0, 0, -20f);
        _totalSpawned = 0;

        // Bắt đầu quy trình sinh map ban đầu
        StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        IsReady = false;
        
        // Sinh đoạn đường đầu tiên và đợi nó xong
        if (_segmentAssetRefs.Count > 0)
        {
            yield return StartCoroutine(SpawnSegmentAsync(_segmentAssetRefs[Random.Range(0, _segmentAssetRefs.Count)]));
        }
        
        // Sinh thêm ít nhất 1 đoạn nữa cho chắc chắn
        for (int i = 1; i < _segmentsAhead && i < 2; i++)
        {
            yield return StartCoroutine(SpawnSegmentAsync(_segmentAssetRefs[Random.Range(0, _segmentAssetRefs.Count)]));
        }

        IsReady = true;
        // Debug.Log("<color=green>[TrackManager] Map đã được tải xong và sẵn sàng!</color>");
    }

    private void Update()
    {
        if (_player == null) return;

        // Tối ưu WebGL: Chỉ kiểm tra mỗi 10 frame một lần
        if (Time.frameCount % 10 != 0) return;

        CheckDespawn();
        CheckSpawn();
    }

    // ─────────────────────────────────────────────────────────────
    // Spawn Logic
    // ─────────────────────────────────────────────────────────────

    private void CheckSpawn()
    {
        if (_isSpawning) return;
        if (_activeSegments.Count < _segmentsAhead)
            SpawnNextSegment();
    }

    private void SpawnNextSegment()
    {
        if (_segmentAssetRefs == null || _segmentAssetRefs.Count == 0)
        {
            // Debug.LogWarning("[TrackManager] Không có Segment Asset References. Hãy gán trong Inspector.");
            return;
        }

        int randomIndex = Random.Range(0, _segmentAssetRefs.Count);
        StartCoroutine(SpawnSegmentAsync(_segmentAssetRefs[randomIndex]));
    }

    private IEnumerator SpawnSegmentAsync(AssetReference assetRef)
    {
        _isSpawning = true;

        AsyncOperationHandle<GameObject> handle = assetRef.InstantiateAsync(_nextSpawnPosition, Quaternion.identity);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject segmentGO = handle.Result;
            TrackSegment segment = segmentGO.GetComponent<TrackSegment>();

            if (segment != null)
            {
                // Căn chỉnh segment khớp với điểm nối
                if (segment.StartPoint != null)
                {
                    Vector3 offset = segmentGO.transform.position - segment.StartPoint.position;
                    segmentGO.transform.position = _nextSpawnPosition + offset;
                }

                // Cập nhật điểm spawn tiếp theo
                if (segment.EndPoint != null)
                    _nextSpawnPosition = segment.EndPoint.position;

                _activeSegments.Add(segment);

                // 🎲 Tất cả các đoạn đều sinh xe/xu ngay từ đầu
                _totalSpawned++;
                _levelGenerator?.PopulateSegment(segment);
            }
            else
            {
                // Debug.LogError($"[TrackManager] Prefab '{segmentGO.name}' thiếu component TrackSegment.cs!");
                Addressables.ReleaseInstance(segmentGO);
            }
        }
        else
        {
            // Debug.LogError("[TrackManager] Không thể load segment từ Addressables.");
        }

        _isSpawning = false;
    }

    // ─────────────────────────────────────────────────────────────
    // Despawn Logic
    // ─────────────────────────────────────────────────────────────

    private void CheckDespawn()
    {
        if (_activeSegments.Count == 0) return;

        TrackSegment oldest = _activeSegments[0];
        if (oldest == null)
        {
            _activeSegments.RemoveAt(0);
            return;
        }

        if (oldest.EndPoint != null &&
            oldest.EndPoint.position.z < _player.position.z - _despawnDistance)
        {
            _activeSegments.RemoveAt(0);

            // 🧹 Dọn dẹp obstacles và coins trước khi release
            _levelGenerator?.CleanupSegment(oldest);
            Addressables.ReleaseInstance(oldest.gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    public void ResetTrack()
    {
        foreach (var seg in _activeSegments)
        {
            if (seg == null) continue;
            _levelGenerator?.CleanupSegment(seg);
            Addressables.ReleaseInstance(seg.gameObject);
        }
        _activeSegments.Clear();

        _nextSpawnPosition = _player != null ? _player.position - Vector3.forward * 20f : new Vector3(0, 0, -20f);
        _totalSpawned = 0;

        for (int i = 0; i < _segmentsAhead; i++)
            SpawnNextSegment();
    }
}
