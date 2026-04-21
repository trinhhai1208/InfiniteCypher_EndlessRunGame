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
    [Header("Parkour Pattern Settings")]
    [SerializeField] [Range(0f, 1f)] private float _parkourPatternChance = 0.30f;
    [Tooltip("Số xe con làm bệ phóng trước khi nhảy lên Bus.")]
    [SerializeField] private int _steppingStoneCount = 3;
    [Tooltip("Khoảng cách từ xe con cuối cùng đến bus (lấy đà nhảy).")]
    [SerializeField] private float _carToBusJumpGap = 3.0f;
    [Tooltip("Số lượng bus trong chuỗi nhảy (1-3, sau đó 1).")]
    [SerializeField] private int _maxJumpSequenceBuses = 3;
    [Tooltip("Tốc độ xe bus di động (m/s).")]
    [SerializeField] private float _movingBusSpeed = 8f;
    [Tooltip("Khoảng cách nhảy giữa 2 bus trong chuỗi liên hoàn.")]
    [SerializeField] private float _jumpGapBetweenBuses = 4.5f;
    [Tooltip("Độ cao cung mây xu hình Sin nối giữa 2 xe bus.")]
    [SerializeField] private float _bridgeSineAmplitude = 2.5f;

    // Lựa chọn Pattern
    private enum ParkourType
    {
        StaticJump,  // Tất cả bus đứng yên, có bridge coin hình sin
        MovingJump   // Tất cả bus di động, coin gắn vào bus
    }

    // ─────────────────────────────────────────
    [Header("Tỉ Lệ Sinh (Probabilities)")]
    [SerializeField] [Range(0f, 1f)] private float _carGroupChance = 0.30f;
    [SerializeField] [Range(0f, 1f)] private float _busGroupChance = 0.25f;

    // ─────────────────────────────────────────
    [Header("Free Coin Settings")]
    [SerializeField] [Range(0f, 1f)] private float _zigzagChance = 0.6f;
    [SerializeField] private Vector2Int _freeCoinCountRange = new(5, 12);
    [SerializeField] private float _freeCoinSpacing = 1.0f;

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
            _carRefs, _laneDistance, _carRoofY, _carLengthZ, _budget);

        _busSpawner = new BusSpawner(
            _busRefs, _laneDistance, _busRoofY, _busLengthZ,
            _busGapZ, _coinSpacingOnBus, _maxBusChain, _budget, _bridgeSineAmplitude);

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
                    if (coin != null)
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

            // ─── Parkour Pattern ────────────────────────────
            if (roll < _parkourPatternChance)
            {
                float patternLen = 0f;
                yield return SpawnParkourPattern(segment, currentZ, group, len => patternLen = len);
                advanceZ = patternLen + _gapBetweenGroups;
            }
            // ─── Xe Con (Single Car) ───────────────────────────
            else if (roll < _parkourPatternChance + _carGroupChance && _carRefs.Count > 0)
            {
                int lane = Random.Range(-1, 2);
                float actualCarLength = _carLengthZ;
                // Chỉ sinh 1 xe con đơn lẻ
                yield return _carSpawner.SpawnOnly(segment, currentZ, lane, group, _sizeCache, len => actualCarLength = len);
                advanceZ = actualCarLength + _gapBetweenGroups;
            }
            // ─── Xe Bus ─────────────────────────────────────
            else if (roll < _parkourPatternChance + _carGroupChance + _busGroupChance && _busRefs.Count > 0)
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
                        yield return _busSpawner.SpawnWithCoins(segment, currentBusZ, lane, group, _sizeCache, onSpawned: len => nextLen = len);
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
                        yield return _busSpawner.SpawnOnly(segment, currentBusZ, lane, group, _sizeCache, onSpawned: len => nextLen = len);
                        currentBusZ += nextLen + _busSpawner.GetGapZ();
                        groupEndZ = currentBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
            }
            // ─── Rào Chắn ───────────────────────────────────
            else if (roll < _parkourPatternChance + _carGroupChance + _busGroupChance + _barrierChance && _barrierRefs.Count > 0)
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


        }
    }

    // ─────────────────────────────────────────────────────────
    // Parkour Pattern Logic
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnParkourPattern(TrackSegment segment, float startZ, SpawnedObjects group, System.Action<float> onComplete)
    {
        ParkourType type = Random.value < 0.5f ? ParkourType.StaticJump : ParkourType.MovingJump;
        int parkourLane = Random.Range(-1, 2);
        int[] allLanes = { -1, 0, 1 };

        // 1. Sinh Bệ Phóng (Car Stepping Stones)
        float steppingEndZ = startZ;
        for (int i = 0; i < _steppingStoneCount; i++)
        {
            if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
            float carLen = _carLengthZ;
            yield return _carSpawner.SpawnOnly(segment, steppingEndZ, parkourLane, group, _sizeCache, len => carLen = len);
            steppingEndZ += carLen;
        }

        float blockStartZ = steppingEndZ + _carToBusJumpGap;
        int chainBusCount = PickChainLength();
        float totalLengthZ = 0f;

        if (type == ParkourType.StaticJump)
        {
            // StaticJump: Làn parkourLane có Static Bus nhảy qua nhảy lại. Các làn kia trống hoặc rào chắn.
            foreach (int lane in allLanes)
            {
                if (lane == parkourLane) continue;
                float sideRoll = Random.value;
                if (sideRoll < 0.6f) { /* Trống */ }
                else if (_barrierRefs.Count > 0)
                {
                    if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
                    yield return _barrierSpawner.Spawn(segment, blockStartZ, lane, group, _maxCoinsPerSegment, 0, _ => { });
                }
            }

            // Sinh Chuỗi Static Nhảy trên parkourLane
            float currentBusZ = blockStartZ;
            float prevBusEndZ = blockStartZ;
            
            for (int i = 0; i < chainBusCount; i++)
            {
                if (_budget.IsExhausted()) { _budget.ResetFrame(); yield return null; }
                float busLen = _busLengthZ;
                bool isSuccess = false;
                yield return _busSpawner.SpawnWithCoins(segment, currentBusZ, parkourLane, group, _sizeCache, onSpawned: len => { busLen = len; isSuccess = (len > 0); });
                
                if (isSuccess && i > 0)
                {
                    yield return _busSpawner.SpawnSinusoidalBridge(segment, prevBusEndZ, currentBusZ, parkourLane * _laneDistance, _busRoofY, coinCount: 5, group);
                }
                
                prevBusEndZ = currentBusZ + busLen;
                currentBusZ = prevBusEndZ + _jumpGapBetweenBuses;
            }
            totalLengthZ = currentBusZ - startZ;
        }
        else // MovingJump
        {
            // MovingJump: Làn parkourLane dừng lại ở 1 Static Bus làm đài. Các làn kia phun Moving Bus.
            float safePlatformLen = _busLengthZ;
            yield return _busSpawner.SpawnWithCoins(segment, blockStartZ, parkourLane, group, _sizeCache, onSpawned: len => safePlatformLen = len);
            
            int movingLaneCount = 0;
            float maxMovingZ = blockStartZ;
            
            foreach (int lane in allLanes)
            {
                if (lane == parkourLane) continue;

                float sideRoll = Random.value;
                if (sideRoll < 0.5f || movingLaneCount >= 2) 
                {
                    if (sideRoll >= 0.5f && _barrierRefs.Count > 0)
                        yield return _barrierSpawner.Spawn(segment, blockStartZ, lane, group, _maxCoinsPerSegment, 0, _ => { });
                    continue; 
                }

                movingLaneCount++;
                float movingZ = blockStartZ;
                for (int i = 0; i < chainBusCount; i++)
                {
                    float busLen = _busLengthZ;
                    yield return _busSpawner.SpawnWithCoins(segment, movingZ, lane, group, _sizeCache, makeMoving: true, speed: _movingBusSpeed, onSpawned: len => busLen = len);
                    movingZ += busLen + _jumpGapBetweenBuses;
                }
                maxMovingZ = Mathf.Max(maxMovingZ, movingZ);
            }
            
            totalLengthZ = maxMovingZ - startZ;
        }

        onComplete?.Invoke(totalLengthZ);
    }

    private int PickChainLength()
    {
        float r = Random.value;
        if (r < 0.50f) return 1;
        if (r < 0.80f) return 2;
        return _maxJumpSequenceBuses;
    }
}
