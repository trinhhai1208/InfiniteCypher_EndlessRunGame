using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// P2 Refactor: LevelGenerator giờ chỉ đóng vai trò "Orchestrator".
/// - Quyết định xác suất nhóm sinh (Car / Bus / Barrier / FreeCoin).
/// - Gọi các module Spawner tương ứng (CarSpawner, BusSpawner, BarrierSpawner, FreeCoinSpawner).
/// - Quản lý SpawnBudget để trải đều spike CPU qua nhiều frame.
/// - Quản lý cleanup theo segment.
/// 
/// Tất cả logic spawn chi tiết đã được chuyển sang thư mục Spawners/.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    // ─── Public struct cho các Spawner bên trong ────────────────
    public struct SpawnedObjects
    {
        public List<GameObject> Coins;
        public List<GameObject> Obstacles;
    }

    // ─────────────────────────────────────────
    [Header("Lane Config")]
    [Tooltip("Khoảng cách làn — phải khớp với _laneDistance trong PlayerController")]
    [SerializeField] private float _laneDistance = 3.8f;

    // ─────────────────────────────────────────
    [Header("Xe Con (Car)")]
    [SerializeField] private List<AssetReference> _carRefs = new();
    [SerializeField] private float _carRoofY = 2f;
    [SerializeField] private float _carLengthZ = 4.6f;
    [SerializeField] private float _carSineAmplitude = 1f;
    [SerializeField] private int _coinsOnCar = 3;

    // ─────────────────────────────────────────
    [Header("Xe Bus")]
    [SerializeField] private List<AssetReference> _busRefs = new();
    [SerializeField] private float _busRoofY = 4.5f;
    [SerializeField] private float _busLengthZ = 9.3f;
    [SerializeField] [Range(1, 5)] private int _maxBusChain = 3;
    [SerializeField] private float _busGapZ = 0f;
    [SerializeField] [Range(0f, 1f)] private float _busHasCoinChance = 0.5f;
    [SerializeField] private float _carToBusGap = 2.5f;

    // ─────────────────────────────────────────
    [Header("Rào chắn (Barriers)")]
    [SerializeField] private List<AssetReference> _barrierRefs = new();
    [SerializeField] [Range(0f, 1f)] private float _barrierChance = 0.2f;
    [SerializeField] private float _barrierCoinZOffset = -3f;
    [SerializeField] private float _barrierLowCoinY = 0.8f;

    // ─────────────────────────────────────────
    [Header("Coin Settings")]
    [SerializeField] private float _coinSpacingOnBus = 1.2f;
    [SerializeField] private float _freeCoinHeightY = 1.2f;

    // ─────────────────────────────────────────
    [Header("Spawn Settings")]
    [SerializeField] private float _safeStartOffset = 50f;
    [SerializeField] private float _gapBetweenGroups = 5f;

    // ─────────────────────────────────────────
    [Header("Tỉ Lệ Sinh (Probabilities)")]
    [SerializeField] [Range(0f, 1f)] private float _carGroupChance = 0.30f;
    [SerializeField] [Range(0f, 1f)] private float _busGroupChance = 0.25f;

    // ─────────────────────────────────────────
    [Header("Free Coin Settings")]
    [SerializeField] [Range(0f, 1f)] private float _zigzagChance = 0.6f;
    [SerializeField] private Vector2Int _freeCoinCountRange = new(5, 12);
    [SerializeField] private float _freeCoinSpacing = 1.0f;
    [SerializeField] [Range(0f, 1f)] private float _extraCoinAfterObstacleChance = 0.5f;
    [SerializeField] private int _maxCoinsPerSegment = 60;

    // ─────────────────────────────────────────
    [Header("Power-ups")]
    [SerializeField] private List<AssetReference> _powerUpRefs = new();
    [SerializeField] [Range(0f, 1f)] private float _powerUpSpawnChance = 0.15f;

    // ─────────────────────────────────────────
    [Header("P2: Spawn Budget")]
    [Tooltip("Số lượng vật thể (xe/barrier/powerup) tối đa sinh ra mỗi frame. Tăng = ít frame giật hơn nhưng spike cao hơn.")]
    [SerializeField] [Range(1, 10)] private int _spawnBudgetPerFrame = 3;

    // ─────────────────────────────────────────
    // Internal State
    // ─────────────────────────────────────────
    private readonly Dictionary<TrackSegment, SpawnedObjects> _spawnedMap = new();
    private readonly WaitForEndOfFrame _waitNextFrame = new();

    // P2: Shared instances — tạo 1 lần, tái dùng mãi
    private ObjectSizeCache _sizeCache;
    private SpawnBudget _budget;
    private CarSpawner _carSpawner;
    private BusSpawner _busSpawner;
    private BarrierSpawner _barrierSpawner;
    private FreeCoinSpawner _freeCoinSpawner;

    // ─────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        _sizeCache = new ObjectSizeCache();
        _budget    = new SpawnBudget(_spawnBudgetPerFrame);

        _carSpawner = new CarSpawner(
            _carRefs, _laneDistance, _carRoofY, _carLengthZ,
            _carSineAmplitude, _coinsOnCar, _budget);

        _busSpawner = new BusSpawner(
            _busRefs, _laneDistance, _busRoofY, _busLengthZ,
            _busGapZ, _coinSpacingOnBus, _maxBusChain, _budget);

        _barrierSpawner = new BarrierSpawner(
            _barrierRefs, _laneDistance,
            _barrierCoinZOffset, _barrierLowCoinY, _budget);

        _freeCoinSpawner = new FreeCoinSpawner(
            _powerUpRefs, _laneDistance, _freeCoinHeightY, _freeCoinSpacing,
            _zigzagChance, _powerUpSpawnChance, _freeCoinCountRange, _budget);
    }

    // ─────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────

    public void PopulateSegment(TrackSegment segment)
    {
        if (segment == null) return;
        StartCoroutine(PopulateRoutine(segment));
    }

    public void CleanupSegment(TrackSegment segment)
    {
        if (segment == null) return;

        if (_spawnedMap.TryGetValue(segment, out SpawnedObjects group))
        {
            if (group.Coins != null)
            {
                for (int i = 0; i < group.Coins.Count; i++)
                {
                    GameObject coin = group.Coins[i];
                    if (coin != null && coin.transform.parent == segment.transform)
                        CoinPool.Instance?.Return(coin);
                }
            }

            if (group.Obstacles != null)
            {
                for (int i = 0; i < group.Obstacles.Count; i++)
                {
                    if (group.Obstacles[i] != null)
                        AddressablePoolManager.Instance.Return(group.Obstacles[i]);
                }
            }

            _spawnedMap.Remove(segment);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Core Orchestration Loop
    // ─────────────────────────────────────────────────────────

    private IEnumerator PopulateRoutine(TrackSegment segment)
    {
        while (CoinPool.Instance == null || !CoinPool.Instance.IsReady)
            yield return _waitNextFrame;

        var group = new SpawnedObjects
        {
            Coins     = new List<GameObject>(60),
            Obstacles = new List<GameObject>(10)
        };
        _spawnedMap[segment] = group;
        int coinCount = 0;

        if (segment.StartPoint == null || segment.EndPoint == null) yield break;

        float currentZ = segment.StartPoint.position.z + _safeStartOffset;
        float endZ     = segment.EndPoint.position.z - 5f;

        while (currentZ < endZ)
        {
            float roll     = Random.value;
            float advanceZ = _gapBetweenGroups;

            // P2: Reset budget mỗi lần qua threshold
            if (_budget.IsExhausted())
            {
                _budget.ResetFrame();
                yield return null; // Nhường frame
            }

            // ─── Xe Con ─────────────────────────────────────
            if (roll < _carGroupChance && _carRefs.Count > 0)
            {
                int lane = Random.Range(-1, 2);
                float actualCarLength = _carLengthZ;
                yield return _carSpawner.SpawnWithCoins(segment, currentZ, lane, group, _sizeCache, len => actualCarLength = len);
                advanceZ = actualCarLength + _gapBetweenGroups;
            }
            // ─── Xe Bus ─────────────────────────────────────
            else if (roll < _carGroupChance + _busGroupChance && _busRefs.Count > 0)
            {
                int lane = Random.Range(-1, 2);
                bool spawnBusCoins = Random.value < _busHasCoinChance;

                if (spawnBusCoins)
                {
                    int steppingCount = Random.Range(2, 4);
                    float steppingZ   = currentZ;

                    for (int i = 0; i < steppingCount; i++)
                    {
                        if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
                        float carLen = _carLengthZ;
                        yield return _carSpawner.SpawnOnly(segment, steppingZ, lane, group, _sizeCache, len => carLen = len);
                        steppingZ += carLen;
                    }

                    float busStartZ   = steppingZ + _carToBusGap;
                    int chainCount    = Random.Range(1, _busSpawner.GetMaxChain() + 1);
                    float currentBusZ = busStartZ;
                    float groupEndZ   = busStartZ;

                    for (int i = 0; i < chainCount; i++)
                    {
                        if (currentBusZ > endZ) break;
                        if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
                        float nextLen = _busLengthZ;
                        yield return _busSpawner.SpawnWithCoins(segment, currentBusZ, lane, group, _sizeCache, len => nextLen = len);
                        currentBusZ += nextLen + _busSpawner.GetGapZ();
                        groupEndZ = currentBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
                else
                {
                    int chainCount    = Random.Range(1, _busSpawner.GetMaxChain() + 1);
                    float currentBusZ = currentZ;
                    float groupEndZ   = currentZ;

                    for (int i = 0; i < chainCount; i++)
                    {
                        if (currentBusZ > endZ) break;
                        if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
                        float nextLen = _busLengthZ;
                        yield return _busSpawner.SpawnOnly(segment, currentBusZ, lane, group, _sizeCache, len => nextLen = len);
                        currentBusZ += nextLen + _busSpawner.GetGapZ();
                        groupEndZ = currentBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
            }
            // ─── Rào Chắn ───────────────────────────────────
            else if (roll < _carGroupChance + _busGroupChance + _barrierChance && _barrierRefs.Count > 0)
            {
                int lane = Random.Range(-1, 2);
                yield return _barrierSpawner.Spawn(segment, currentZ, lane, group,
                    _maxCoinsPerSegment, coinCount,
                    spawned => coinCount += spawned);

                advanceZ = _gapBetweenGroups;
            }
            // ─── Xu Tự Do ───────────────────────────────────
            else if (CoinPool.Instance != null)
            {
                int remaining = _maxCoinsPerSegment - coinCount;
                if (remaining <= 0) break;

                int freeCoinCount = Mathf.Min(_freeCoinSpawner.GetRandomCount(), remaining);
                yield return _freeCoinSpawner.Spawn(segment, currentZ, Random.Range(-1, 2), group, freeCoinCount);
                coinCount += freeCoinCount;
                advanceZ = freeCoinCount * _freeCoinSpawner.GetSpacing() + _gapBetweenGroups;
            }

            currentZ += advanceZ;

            // Xu bonus sau xe/bus
            if (roll < _carGroupChance + _busGroupChance
                && CoinPool.Instance != null
                && Random.value < _extraCoinAfterObstacleChance)
            {
                int bonusRemaining = _maxCoinsPerSegment - coinCount;
                if (bonusRemaining > 0)
                {
                    int bonusCount = Mathf.Min(_freeCoinSpawner.GetRandomCount(), bonusRemaining);
                    yield return _freeCoinSpawner.Spawn(segment, currentZ, Random.Range(-1, 2), group, bonusCount);
                    coinCount += bonusCount;
                    currentZ  += bonusCount * _freeCoinSpawner.GetSpacing() + _gapBetweenGroups;
                }
            }
        }
    }
}
