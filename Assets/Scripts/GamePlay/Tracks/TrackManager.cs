using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Quản lý việc sinh (spawn) và xóa (release) các đoạn đường bằng Addressables.
/// Luôn giữ _segmentsAhead đoạn đường phía trước nhân vật.
/// Kết hợp với LevelGenerator để tự động sinh obstacles và coins.
/// P2 Optimization: Chuyển sang Queue<TrackSegment> để CheckDespawn đạt O(1).
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

    // P2: Đổi từ List sang Queue — Dequeue() = O(1) thay vì RemoveAt(0) = O(n)
    private readonly Queue<TrackSegment> _activeSegments = new();
    private Vector3 _nextSpawnPosition = Vector3.zero;
    private bool _isSpawning;
    private int _totalSpawned;

    public bool IsReady { get; private set; } = false;

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ServiceLocator.Register<TrackManager>(this);

        if (_levelGenerator == null)
            _levelGenerator = GetComponent<LevelGenerator>();

        _nextSpawnPosition = _player != null ? _player.position - Vector3.forward * 20f : new Vector3(0, 0, -20f);
        _totalSpawned = 0;

        StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        IsReady = false;

        while (_levelGenerator == null)
        {
            _levelGenerator = GetComponent<LevelGenerator>();
            yield return null;
        }

        for (int i = 0; i < _segmentsAhead; i++)
        {
            if (_segmentAssetRefs.Count > 0)
            {
                yield return StartCoroutine(SpawnSegmentAsync(_segmentAssetRefs[Random.Range(0, _segmentAssetRefs.Count)]));
            }
        }

        IsReady = true;
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
        if (_segmentAssetRefs == null || _segmentAssetRefs.Count == 0) return;

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
                if (segment.StartPoint != null)
                {
                    Vector3 offset = segmentGO.transform.position - segment.StartPoint.position;
                    segmentGO.transform.position = _nextSpawnPosition + offset;
                }

                if (segment.EndPoint != null)
                    _nextSpawnPosition = segment.EndPoint.position;

                // P2: Enqueue thay vì Add (Queue API)
                _activeSegments.Enqueue(segment);

                _totalSpawned++;
                _levelGenerator?.PopulateSegment(segment);
            }
            else
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(segmentGO);
            }
        }

        _isSpawning = false;
    }

    // ─────────────────────────────────────────────────────────────
    // Despawn Logic
    // ─────────────────────────────────────────────────────────────

    private void CheckDespawn()
    {
        if (_activeSegments.Count == 0) return;

        // P2: Peek() = O(1) thay vì _activeSegments[0] trên List
        TrackSegment oldest = _activeSegments.Peek();

        if (oldest == null)
        {
            _activeSegments.Dequeue();
            return;
        }

        if (oldest.EndPoint != null &&
            oldest.EndPoint.position.z < _player.position.z - _despawnDistance)
        {
            // P2: Dequeue() = O(1) thay vì RemoveAt(0) = O(n)
            _activeSegments.Dequeue();

            _levelGenerator?.CleanupSegment(oldest);

            if (oldest.gameObject != null)
                Addressables.ReleaseInstance(oldest.gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    public void ResetTrack()
    {
        StartCoroutine(ResetTrackRoutine());
    }

    private IEnumerator ResetTrackRoutine()
    {
        IsReady = false;

        // Snapshot để làm sạch queue trong khi xử lý
        var snapshot = new List<TrackSegment>(_activeSegments);
        _activeSegments.Clear();

        foreach (var seg in snapshot)
        {
            if (seg == null) continue;

            _levelGenerator?.CleanupSegment(seg);
            if (seg.gameObject != null)
                Addressables.ReleaseInstance(seg.gameObject);

            // Dọn dẹp từng segment qua từng frame để tránh CPU Spike
            yield return null;
        }

        _nextSpawnPosition = _player != null ? _player.position - Vector3.forward * 20f : new Vector3(0, 0, -20f);
        _totalSpawned = 0;

        for (int i = 0; i < _segmentsAhead; i++)
        {
            yield return StartCoroutine(SpawnSegmentAsync(_segmentAssetRefs[Random.Range(0, _segmentAssetRefs.Count)]));
        }

        IsReady = true;
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TrackManager>();
    }
}
