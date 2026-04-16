using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Tự động sinh xe con, xe Bus/Container và đồng xu vào mỗi TrackSegment.
/// - Xe con: xu hình Sin trên nóc
/// - Xe Bus/Container: xu đường thẳng trên nóc, có thể xuất hiện theo chuỗi
/// - Không cần đặt SpawnPoint thủ công
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Lane Config")]
    [Tooltip("Khoảng cách làn — phải khớp với _laneDistance trong PlayerController")]
    [SerializeField] private float _laneDistance = 3.8f;

    // ─────────────────────────────────────────
    [Header("Xe Con (Car)")]
    [SerializeField] private List<AssetReference> _carRefs = new();
    [Tooltip("Độ cao nóc xe con (chỉnh theo Prefab thực tế)")]
    [SerializeField] private float _carRoofY = 2f;
    [Tooltip("Chiều dài xe con theo trục Z")]
    [SerializeField] private float _carLengthZ = 4.6f;
    [Tooltip("Biên độ hình Sin của xu trên nóc xe con (cao nhất ở giữa xe)")]
    [SerializeField] private float _carSineAmplitude = 1f;
    [Tooltip("Số đồng xu trên nóc 1 xe con")]
    [SerializeField] private int _coinsOnCar = 3;

    // ─────────────────────────────────────────
    [Header("Xe Bus")]
    [SerializeField] private List<AssetReference> _busRefs = new();
    [Tooltip("Độ cao nóc xe Bus(chỉnh theo Prefab thực tế)")]
    [SerializeField] private float _busRoofY = 4.5f;
    [Tooltip("Chiều dài xe Bus theo trục Z")]
    [SerializeField] private float _busLengthZ = 9.3f;
    [Tooltip("Số xe tối đa trong 1 chuỗi liên tiếp")]
    [SerializeField] [Range(1, 5)] private int _maxBusChain = 3;
    [Tooltip("Khoảng gap giữa 2 xe trong chuỗi (vật lý khi nhảy giỪa 2 bus)")]
    [SerializeField] private float _busGapZ = 0f;
    [Tooltip("Tỉ lệ sinh xu trên nóc xe Bus (0 = không bao giờ, 1 = luôn luôn)")]
    [SerializeField] [Range(0f, 1f)] private float _busHasCoinChance = 0.5f;
    [Tooltip("Khoảng cách từ xe bàn đạp đến xe Bus (ngắn để giữ đà nhảy)")]
    [SerializeField] private float _carToBusGap = 2.5f;

    // ─────────────────────────────────────────
    [Header("Rào chắn (Barriers)")]
    [SerializeField] private List<AssetReference> _barrierRefs = new();
    [SerializeField] [Range(0f, 1f)] private float _barrierChance = 0.2f;
    [SerializeField] private float _barrierCoinZOffset = -3f;
    [Tooltip("Độ cao xu sát đất tại barrier (để slide)")]
    [SerializeField] private float _barrierLowCoinY = 0.8f;

    // ─────────────────────────────────────────
    [Header("Coin Settings")]
    [Tooltip("Khoảng cách giữa 2 xu trên nóc xe Bus")]
    [SerializeField] private float _coinSpacingOnBus = 1.2f;
    [Tooltip("Độ cao xu tự do (không trên xe)")]
    [SerializeField] private float _freeCoinHeightY = 1.2f;

    // ─────────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Khoảng Z đầu tiên không sinh vật cản (tính từ StartPoint của segment - Đã cộng bù 20m lùi)")]
    [SerializeField] private float _safeStartOffset = 50f;
    [Tooltip("Khoảng trống giữa 2 nhóm xe")]
    [SerializeField] private float _gapBetweenGroups = 5f;

    [Header("Tỉ Lệ Sinh (Probabilities)")]
    [SerializeField] [Range(0f, 1f)] private float _carGroupChance = 0.30f;
    [SerializeField] [Range(0f, 1f)] private float _busGroupChance = 0.25f;
    // Xác suất sinh xu tự do = 1 - carGroupChance - busGroupChance - barrierChance

    [Header("Free Coin Settings")]
    [Tooltip("Xác suất xu tự do xuất hiện dạng zigzag (thay vì đường thẳng)")]
    [SerializeField] [Range(0f, 1f)] private float _zigzagChance = 0.6f;
    [Tooltip("Số xu trong mỗi cụm xu tự do (Tăng lên để xu dầy hơn)")]
    [SerializeField] private Vector2Int _freeCoinCountRange = new(5, 12);
    [Tooltip("Khoảng cách giữa 2 xu tự do (nhỏ hơn = chuỗi xu ngắn hơn, nhiều cụm hơn)")]
    [SerializeField] private float _freeCoinSpacing = 1.0f;
    [Tooltip("Sau khi gặp xe/barrier, có bao nhiêu % cơ hội thêm xu tự do ở khoảng trống tiếp theo")]
    [SerializeField] [Range(0f, 1f)] private float _extraCoinAfterObstacleChance = 0.5f;
    [Tooltip("Số đồng xu tối đa cho phép trên 1 segment (tránh spam quá nhiều)")]
    [SerializeField] private int _maxCoinsPerSegment = 60;

    // ─────────────────────────────────────────
    [Header("Power-ups")]
    [SerializeField] private List<UnityEngine.AddressableAssets.AssetReference> _powerUpRefs = new();
    [SerializeField] [Range(0f, 1f)] private float _powerUpSpawnChance = 0.15f; // 15% cơ hội mỗi cụm xu xuất hiện Powerup

    private struct ObjectStats
    {
        public float LengthZ;
        public float OffsetToTail; // Khoảng cách từ Pivot đến mép sau (min.z) của vật thể
    }

    private struct SpawnedObjects
    {
        public List<GameObject> Coins;
        public List<GameObject> Obstacles;
    }

    // Lưu objects theo từng segment để cleanup đúng cách
    private readonly Dictionary<TrackSegment, SpawnedObjects> _spawnedMap = new();

    // Cache chiều dài (Z) và WaitForSeconds
    private readonly Dictionary<string, ObjectStats> _lengthCache = new();
    private readonly WaitForEndOfFrame _waitNextFrame = new();

    // ─────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sinh chướng ngại vật và đồng xu vào segment.
    /// </summary>
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
            // 1. Thu hồi xu
            if (group.Coins != null)
            {
                for (int i = 0; i < group.Coins.Count; i++)
                {
                    GameObject coin = group.Coins[i];
                    if (coin != null && coin.transform.parent == segment.transform)
                    {
                        CoinPool.Instance?.Return(coin);
                    }
                }
            }

            // 2. Thu hồi vật cản (về Pool)
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
    // Core Generation Loop
    // ─────────────────────────────────────────────────────────

    private IEnumerator PopulateRoutine(TrackSegment segment)
    {
        // ⏳ Chờ cho đến khi Pool sẵn sàng
        while (CoinPool.Instance == null || !CoinPool.Instance.IsReady)
        {
            yield return _waitNextFrame;
        }

        var group = new SpawnedObjects
        {
            Coins = new List<GameObject>(60),
            Obstacles = new List<GameObject>(10)
        };
        _spawnedMap[segment] = group;
        int coinCount = 0; // Đếm số xu đã sinh trong segment này

        if (segment.StartPoint == null || segment.EndPoint == null)
        {
            yield break;
        }

        float currentZ = segment.StartPoint.position.z + _safeStartOffset;
        float endZ     = segment.EndPoint.position.z - 5f;

        while (currentZ < endZ)
        {
            float roll       = Random.value;
            float advanceZ   = _gapBetweenGroups; // mặc định tiến thêm 1 khoảng trống

            if (roll < _carGroupChance && _carRefs.Count > 0)
            {
                // ── Xe Con ──────────────────────────────
                int lane = Random.Range(-1, 2);
                float actualCarLength = _carLengthZ;
                yield return SpawnCarWithSineCoins(segment, currentZ, lane, group, len => actualCarLength = len);
                advanceZ = actualCarLength + _gapBetweenGroups;
            }
            else if (roll < _carGroupChance + _busGroupChance && _busRefs.Count > 0)
            {
                // ── Xe Bus ─────────────────────────────────────────
                int lane = Random.Range(-1, 2);
                bool spawnBusCoins = Random.value < _busHasCoinChance;

                if (spawnBusCoins)
                {
                    // Có xu trên bus → cần 2-3 xe bàn đạp (không có xu) để lấy đà nhảy
                    int steppingCount = Random.Range(2, 4); // 2 hoặc 3 xe
                    float currentSteppingZ = currentZ;

                    for (int i = 0; i < steppingCount; i++)
                    {
                        // Sinh xe, chờ load xong roi đo độ dài thực tế để cộng dồn
                        float carLen = _carLengthZ;
                        yield return SpawnCarOnly(segment, currentSteppingZ, lane, group, len => carLen = len);
                        currentSteppingZ += carLen;
                    }

                    // Giảm gap đến bus để không mất đà
                    float busStartZ = currentSteppingZ + _carToBusGap;
                    int chainCount  = Random.Range(1, _maxBusChain + 1);
                    float currentBusZ = busStartZ;
                    float groupEndZ = busStartZ;

                    for (int i = 0; i < chainCount; i++)
                    {
                        if (currentBusZ > endZ) break;
                        float nextLen = _busLengthZ;
                        yield return SpawnBusWithStraightCoins(segment, currentBusZ, lane, group, len => nextLen = len);
                        currentBusZ += nextLen + _busGapZ;
                        groupEndZ = currentBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
                else
                {
                    // Không có xu trên bus → không cần xe bàn đạp, bus xuất hiện đơn độc
                    int chainCount  = Random.Range(1, _maxBusChain + 1);
                    float currentBusZ = currentZ;
                    float groupEndZ = currentZ;

                    for (int i = 0; i < chainCount; i++)
                    {
                        if (currentBusZ > endZ) break;
                        float nextLen = _busLengthZ;
                        yield return SpawnBusOnly(segment, currentBusZ, lane, group, len => nextLen = len);
                        currentBusZ += nextLen + _busGapZ;
                        groupEndZ = currentBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
            }
            else if (roll < _carGroupChance + _busGroupChance + _barrierChance && _barrierRefs.Count > 0)
            {
                // ── Rào chắn (Barriers) ─────────────────
                int lane = Random.Range(-1, 2);
                float x  = lane * _laneDistance;

                // Spawn rào chắn (Dùng Pool)
                AssetReference barrierRef = _barrierRefs[Random.Range(0, _barrierRefs.Count)];
                GameObject barrierObj = null;
                yield return AddressablePoolManager.Instance.GetRoutine(barrierRef, new Vector3(x, 0f, currentZ), Quaternion.identity, segment.transform, res => barrierObj = res);

                if (barrierObj != null)
                    group.Obstacles.Add(barrierObj);

                if (CoinPool.Instance != null)
                {
                    bool isSinPattern = Random.value < 0.5f;

                    if (isSinPattern)
                    {
                        // 🌊 Hình Sin — Xu bay vòm cao qua barrier → gợi ý JUMP
                        int barrierArcCount = 7;
                        float arcStartZ = currentZ + _barrierCoinZOffset;
                        float arcLength = 7f;

                        for (int c = 0; c < barrierArcCount; c++)
                        {
                            float t     = (float)c / (barrierArcCount - 1);
                            float coinZ = arcStartZ + t * arcLength;
                            float coinY = _freeCoinHeightY + Mathf.Sin(t * Mathf.PI) * 2.5f;

                            if (coinCount >= _maxCoinsPerSegment) break;
                            var coin = CoinPool.Instance.Get(
                                new Vector3(x, coinY, coinZ),
                                Quaternion.identity,
                                segment.transform);
                            if (coin != null) { group.Coins.Add(coin); coinCount++; }
                        }
                    }
                    else
                    {
                        // ⬇️ Xu sát đất — Xu thấp Y trước barrier → gợi ý SLIDE
                        int barrierSlideCount = 5;
                        float coinStartZ = currentZ + _barrierCoinZOffset;

                        for (int c = 0; c < barrierSlideCount; c++)
                        {
                            float coinZ = coinStartZ + c * 1.2f;
                            if (coinCount >= _maxCoinsPerSegment) break;
                            var coin = CoinPool.Instance.Get(
                                new Vector3(x, _barrierLowCoinY, coinZ),
                                Quaternion.identity,
                                segment.transform);
                            if (coin != null) { group.Coins.Add(coin); coinCount++; }
                        }
                    }
                }

                advanceZ = _gapBetweenGroups;
            }
            else if (CoinPool.Instance != null)
            {
                // ── Xu Tự Do ────────────────────────────
                int lane = Random.Range(-1, 2);
                int remaining = _maxCoinsPerSegment - coinCount;
                if (remaining <= 0) break;
                int freeCoinCount = Mathf.Min(Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1), remaining);
                yield return SpawnFreeCoinLine(segment, currentZ, lane, group, freeCoinCount);
                coinCount += freeCoinCount;
                // advanceZ đủ dài để không bị chồng lấp vật cản tiếp theo
                advanceZ = freeCoinCount * _freeCoinSpacing + _gapBetweenGroups;
            }

            currentZ += advanceZ;

            // Sau khi sinh XE, có thể thêm cụm xu tự do ở khoảng trống
            if (roll < _carGroupChance + _busGroupChance
                && CoinPool.Instance != null
                && Random.value < _extraCoinAfterObstacleChance)
            {
                int bonusRemaining = _maxCoinsPerSegment - coinCount;
                if (bonusRemaining <= 0) continue;
                int bonusLane  = Random.Range(-1, 2);
                int bonusCount = Mathf.Min(Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1), bonusRemaining);
                yield return SpawnFreeCoinLine(segment, currentZ, bonusLane, group, bonusCount);
                coinCount += bonusCount;
                // Tiến currentZ qua hết chuỗi xu bonus trước khi vòng lặp tiếp theo bắt đầu
                currentZ += bonusCount * _freeCoinSpacing + _gapBetweenGroups;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // Helper đo chiều dài thực tế (Z) của đối tượng bằng Collider
    // ─────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────
    // Helper: Đo chiều dài vật lý, Căn chỉnh đuôi xe khớp mốc Z
    // ─────────────────────────────────────────────────────────
    private float AlignAndGetLengthZ(GameObject obj, float targetBackZ, float fallbackLength)
    {
        if (obj == null) return fallbackLength;

        string prefabName = obj.name.Replace("(Clone)", "").Trim();

        // 1. Kiểm tra Cache để đạt hiệu năng O(1)
        if (_lengthCache.TryGetValue(prefabName, out ObjectStats stats))
        {
            // Tính toán shiftZ dựa trên OffsetToTail đã cache
            // transform.position.z là vị trí Pivot hiện tại
            // Mép sau thực tế = Pivot.z + OffsetToTail
            // Cần: Pivot.z + OffsetToTail = targetBackZ  =>  Pivot.z = targetBackZ - OffsetToTail
            float newPivotZ = targetBackZ - stats.OffsetToTail;
            obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, newPivotZ);
            
            return stats.LengthZ;
        }

        // 2. Nếu chưa có trong cache, thực hiện đo đạc tốn kém (chỉ chạy 1 lần cho mỗi loại prefab)
        var renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds? totalBounds = null;
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            if (totalBounds == null) totalBounds = r.bounds;
            else
            {
                var b = totalBounds.Value;
                b.Encapsulate(r.bounds);
                totalBounds = b;
            }
        }

        if (totalBounds.HasValue)
        {
            float lengthZ = totalBounds.Value.size.z;
            // OffsetToTail = min.z - Pivot.z (giá trị tương đối)
            float offsetToTail = totalBounds.Value.min.z - obj.transform.position.z;

            // Lưu vào cache
            _lengthCache[prefabName] = new ObjectStats { LengthZ = lengthZ, OffsetToTail = offsetToTail };

            // Căn chỉnh
            float shiftZ = targetBackZ - totalBounds.Value.min.z;
            obj.transform.position += new Vector3(0, 0, shiftZ);
            
            return lengthZ;
        }

        return fallbackLength;
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Car + Xu Hình Sin
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnCarWithSineCoins(TrackSegment segment, float worldZ, int lane, SpawnedObjects group, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;

        // Spawn xe con (Dùng Pool)
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        GameObject carObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(carRef, new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform, res => carObj = res);

        float spawnedLength = _carLengthZ;

        if (carObj == null)
        {
            onSpawned?.Invoke(spawnedLength);
            yield break;
        }

        group.Obstacles.Add(carObj);
        spawnedLength = AlignAndGetLengthZ(carObj, worldZ, _carLengthZ);

        // Spawn xu hình Sin trên nóc xe con
        if (CoinPool.Instance == null)
        {
            onSpawned?.Invoke(spawnedLength);
            yield break;
        }

        for (int i = 0; i < _coinsOnCar; i++)
        {
            float t = (float)i / Mathf.Max(_coinsOnCar - 1, 1);
            float startZ = worldZ + spawnedLength * 0.15f;
            float endZ = worldZ + spawnedLength * 0.85f;
            float coinZ = Mathf.Lerp(startZ, endZ, t);
            
            float sinY  = Mathf.Sin(t * Mathf.PI) * _carSineAmplitude;
            float coinY = _carRoofY + sinY;

            var coin = CoinPool.Instance.Get(new Vector3(x, coinY, coinZ), Quaternion.identity, segment.transform);
            if (coin != null) group.Coins.Add(coin);
        }

        onSpawned?.Invoke(spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Bus + Xu Đường Thẳng
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBusWithStraightCoins(TrackSegment segment, float worldZ, int lane, SpawnedObjects group, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;

        // Spawn xe bus (Dùng Pool)
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        GameObject busObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(busRef, new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform, res => busObj = res);

        float spawnedLength = _busLengthZ;

        if (busObj == null)
        {
            onSpawned?.Invoke(spawnedLength);
            yield break;
        }

        group.Obstacles.Add(busObj);
        spawnedLength = AlignAndGetLengthZ(busObj, worldZ, _busLengthZ);

        // Spawn xu đường thẳng trên nóc xe bus
        if (CoinPool.Instance == null)
        {
            onSpawned?.Invoke(spawnedLength);
            yield break;
        }

        float coinStartZ = worldZ + spawnedLength * 0.15f;
        float coinEndZ   = worldZ + spawnedLength * 0.85f;
        float coinZ      = coinStartZ;

        while (coinZ <= coinEndZ)
        {
            var coin = CoinPool.Instance.Get(new Vector3(x, _busRoofY, coinZ), Quaternion.identity, segment.transform);
            if (coin != null) group.Coins.Add(coin);
            coinZ += _coinSpacingOnBus;
        }

        onSpawned?.Invoke(spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Xu Tự Do
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnFreeCoinLine(TrackSegment segment, float worldZ, int primaryLane, SpawnedObjects group, int coinCount = -1)
    {
        if (CoinPool.Instance == null) yield break;

        // Nếu không truyền count, sinh ngẫu nhiên
        if (coinCount < 0)
            coinCount = Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1);

        bool isZigzag = Random.value < _zigzagChance;
        int secondaryLane;
        if (primaryLane == 0)
            secondaryLane = Random.value < 0.5f ? -1 : 1;
        else
            secondaryLane = 0;

        for (int i = 0; i < coinCount; i++)
        {
            int useLane = isZigzag ? (i % 2 == 0 ? primaryLane : secondaryLane) : primaryLane;
            float coinX = useLane * _laneDistance;
            float coinZ = worldZ + i * _freeCoinSpacing;

            // Cơ hội thay thế đồng xu ở giữa dãy bằng một Power-up
            if (i == coinCount / 2 && _powerUpRefs.Count > 0 && Random.value < _powerUpSpawnChance)
            {
                var pRef = _powerUpRefs[Random.Range(0, _powerUpRefs.Count)];
                GameObject powerupObj = null;
                yield return AddressablePoolManager.Instance.GetRoutine(pRef, new Vector3(coinX, _freeCoinHeightY, coinZ), Quaternion.identity, segment.transform, res => powerupObj = res);
                if (powerupObj != null)
                {
                    group.Obstacles.Add(powerupObj);
                }
                continue; // Bỏ qua sinh xu tại vị trí này
            }

            var coin = CoinPool.Instance.Get(
                new Vector3(coinX, _freeCoinHeightY, coinZ),
                Quaternion.identity,
                segment.transform);
            if (coin != null) group.Coins.Add(coin);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Car Không Xu (Xe bàn đạp)
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnCarOnly(TrackSegment segment, float worldZ, int lane, SpawnedObjects group, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        GameObject carObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(carRef, new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform, res => carObj = res);

        float spawnedLength = _carLengthZ;
        if (carObj != null)
        {
            group.Obstacles.Add(carObj);
            spawnedLength = AlignAndGetLengthZ(carObj, worldZ, _carLengthZ);
        }
        
        onSpawned?.Invoke(spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Bus Không Xu (Bus cản đường)
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBusOnly(TrackSegment segment, float worldZ, int lane, SpawnedObjects group, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        GameObject busObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(busRef, new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform, res => busObj = res);

        float spawnedLength = _busLengthZ;
        if (busObj != null)
        {
            group.Obstacles.Add(busObj);
            spawnedLength = AlignAndGetLengthZ(busObj, worldZ, _busLengthZ);
        }
        
        onSpawned?.Invoke(spawnedLength);
    }
}
