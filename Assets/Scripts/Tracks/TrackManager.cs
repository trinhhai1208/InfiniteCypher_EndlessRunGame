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
    [SerializeField] private int _segmentsAhead = 3;

    [Tooltip("Khoảng cách phía sau nhân vật để xóa segment cũ")]
    [SerializeField] private float _despawnDistance = 20f;

    // Runtime state
    private readonly List<TrackSegment> _activeSegments = new();
    private Vector3 _nextSpawnPosition = Vector3.zero;
    private bool _isSpawning;
    private int _totalSpawned; // Đếm tổng số segment đã sinh — segment đầu tiên sẽ là Safe

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_levelGenerator == null)
            _levelGenerator = GetComponent<LevelGenerator>();

        // Đặt điểm spawn đầu tiên ngay tại vị trí Player
        _nextSpawnPosition = _player != null ? _player.position : Vector3.zero;
        _totalSpawned = 0;

        // Spawn các đoạn đường đầu tiên — Đoạn 1 sẽ tự động là Safe (không có xe)
        for (int i = 0; i < _segmentsAhead; i++)
            SpawnNextSegment();
    }

    private void Update()
    {
        if (_player == null) return;

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
            Debug.LogWarning("[TrackManager] Không có Segment Asset References. Hãy gán trong Inspector.");
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
                Debug.LogError($"[TrackManager] Prefab '{segmentGO.name}' thiếu component TrackSegment.cs!");
                Addressables.ReleaseInstance(segmentGO);
            }
        }
        else
        {
            Debug.LogError("[TrackManager] Không thể load segment từ Addressables.");
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

        _nextSpawnPosition = _player != null ? _player.position : Vector3.zero;
        _totalSpawned = 0;

        for (int i = 0; i < _segmentsAhead; i++)
            SpawnNextSegment();
    }
}
