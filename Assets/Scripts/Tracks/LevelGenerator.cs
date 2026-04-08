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
    [SerializeField] private int _coinsOnCar = 5;

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
    [Tooltip("Khoảng Z đầu tiên không sinh vật cản (tính từ StartPoint của segment)")]
    [SerializeField] private float _safeStartOffset = 15f;
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
    [SerializeField] private Vector2Int _freeCoinCountRange = new(10, 20);
    [Tooltip("Khoảng cách giữa 2 xu tự do (nhỏ hơn = chuỗi xu ngắn hơn, nhiều cụm hơn)")]
    [SerializeField] private float _freeCoinSpacing = 1.0f;
    [Tooltip("Sau khi gặp xe/barrier, có bao nhiêu % cơ hội thêm xu tự do ở khoảng trống tiếp theo")]
    [SerializeField] [Range(0f, 1f)] private float _extraCoinAfterObstacleChance = 0.95f;

    // ─────────────────────────────────────────
    [Header("Power-ups")]
    [SerializeField] private List<UnityEngine.AddressableAssets.AssetReference> _powerUpRefs = new();
    [SerializeField] [Range(0f, 1f)] private float _powerUpSpawnChance = 0.15f; // 15% cơ hội mỗi cụm xu xuất hiện Powerup

    // Lưu objects theo từng segment để cleanup đúng cách
    private readonly Dictionary<TrackSegment, List<GameObject>> _spawnedMap = new();

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

        if (_spawnedMap.TryGetValue(segment, out List<GameObject> list))
        {
            foreach (var obj in list)
            {
                if (obj == null) continue;

                // Xu → trả về pool (tái sử dụng)
                if (obj.CompareTag("Coin"))
                    CoinPool.Instance?.Return(obj);
                else
                    // Vật cản → hủy Addressables instance
                    Addressables.ReleaseInstance(obj);
            }
            _spawnedMap.Remove(segment);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Core Generation Loop
    // ─────────────────────────────────────────────────────────

    private IEnumerator PopulateRoutine(TrackSegment segment)
    {
        // ⏳ Chờ cho đến khi Pool sẵn sàng (đề phòng Addressables load chậm)
        while (CoinPool.Instance == null || !CoinPool.Instance.IsReady)
        {
            yield return null;
        }

        var list = new List<GameObject>();
        _spawnedMap[segment] = list;

        if (segment.StartPoint == null || segment.EndPoint == null)
        {
            Debug.LogWarning($"[LevelGenerator] Segment '{segment.name}' thiếu StartPoint/EndPoint!");
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
                yield return SpawnCarWithSineCoins(segment, currentZ, lane, list, len => actualCarLength = len);
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
                        yield return SpawnCarOnly(segment, currentSteppingZ, lane, list, len => carLen = len);
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
                        float nextBusZ = currentBusZ + _busLengthZ;
                        yield return SpawnBusWithStraightCoins(segment, currentBusZ, lane, list, len => nextBusZ = len);
                        currentBusZ = nextBusZ + _busGapZ;
                        groupEndZ = nextBusZ;
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
                        float nextBusZ = currentBusZ + _busLengthZ;
                        yield return SpawnBusOnly(segment, currentBusZ, lane, list, len => nextBusZ = len);
                        currentBusZ = nextBusZ + _busGapZ;
                        groupEndZ = nextBusZ;
                    }

                    advanceZ = (groupEndZ - currentZ) + _gapBetweenGroups;
                }
            }
            else if (roll < _carGroupChance + _busGroupChance + _barrierChance && _barrierRefs.Count > 0)
            {
                // ── Rào chắn (Barriers) ─────────────────
                int lane = Random.Range(-1, 2);
                float x  = lane * _laneDistance;

                // Spawn rào chắn
                AssetReference barrierRef = _barrierRefs[Random.Range(0, _barrierRefs.Count)];
                var handle = barrierRef.InstantiateAsync(new Vector3(x, 0f, currentZ), Quaternion.identity, segment.transform);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                    list.Add(handle.Result);

                if (CoinPool.Instance != null)
                {
                    bool isSinPattern = Random.value < 0.5f;

                    if (isSinPattern)
                    {
                        // 🌊 Hình Sin — Xu bay vòm cao qua barrier → gợi ý JUMP
                        int coinCount = 7;
                        float arcStartZ = currentZ + _barrierCoinZOffset;
                        float arcLength = 7f;

                        for (int c = 0; c < coinCount; c++)
                        {
                            float t     = (float)c / (coinCount - 1);
                            float coinZ = arcStartZ + t * arcLength;
                            float coinY = _freeCoinHeightY + Mathf.Sin(t * Mathf.PI) * 2.5f;

                            var coin = CoinPool.Instance.Get(
                                new Vector3(x, coinY, coinZ),
                                Quaternion.identity,
                                segment.transform);
                            if (coin != null) list.Add(coin);
                        }
                    }
                    else
                    {
                        // ⬇️ Xu sát đất — Xu thấp Y trước barrier → gợi ý SLIDE
                        int coinCount = 5;
                        float coinStartZ = currentZ + _barrierCoinZOffset;

                        for (int c = 0; c < coinCount; c++)
                        {
                            float coinZ = coinStartZ + c * 1.2f;
                            var coin = CoinPool.Instance.Get(
                                new Vector3(x, _barrierLowCoinY, coinZ),
                                Quaternion.identity,
                                segment.transform);
                            if (coin != null) list.Add(coin);
                        }
                    }
                }

                advanceZ = _gapBetweenGroups;
            }
            else if (CoinPool.Instance != null)
            {
                // ── Xu Tự Do ────────────────────────────
                int lane = Random.Range(-1, 2);
                int freeCoinCount = Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1);
                yield return SpawnFreeCoinLine(segment, currentZ, lane, list, freeCoinCount);
                // advanceZ đủ dài để không bị chồng lấp vật cản tiếp theo
                advanceZ = freeCoinCount * _freeCoinSpacing + _gapBetweenGroups;
            }

            currentZ += advanceZ;

            // Sau khi sinh XE, có thể thêm cụm xu tự do ở khoảng trống
            if (roll < _carGroupChance + _busGroupChance
                && CoinPool.Instance != null
                && Random.value < _extraCoinAfterObstacleChance)
            {
                int bonusLane  = Random.Range(-1, 2);
                int bonusCount = Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1);
                yield return SpawnFreeCoinLine(segment, currentZ, bonusLane, list, bonusCount);
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
        
        // Dùng Renderer thay vì Collider vì Collider trong game thường được thu nhỏ lại để dễ né,
        // khiến việc dùng nó để lấy khoảng cách làm hình ảnh 3D bị đâm vào nhau.
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
            // Tính toán khoảng lệch: Mép đuôi thực tế của xe (min.z) so với gốc cần đặt (targetBackZ)
            float shiftZ = targetBackZ - totalBounds.Value.min.z;
            
            // Dịch chuyển xe tiến/lùi để đuôi xe sát rạt vào đúng điểm mốc
            obj.transform.position += new Vector3(0, 0, shiftZ);
            
            // Trả về độ dài kích thước toàn xe để tính khoảng cách cho xe tiếp theo
            return totalBounds.Value.size.z;
        }
        
        return fallbackLength;
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Car + Xu Hình Sin
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnCarWithSineCoins(TrackSegment segment, float worldZ, int lane, List<GameObject> list, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;

        // Spawn xe con
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        var carHandle = carRef.InstantiateAsync(new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform);
        yield return carHandle;

        float spawnedLength = _carLengthZ;

        if (carHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject carObj = carHandle.Result;
            list.Add(carObj);
            spawnedLength = AlignAndGetLengthZ(carObj, worldZ, _carLengthZ);
        }
        else
        {
            Debug.LogWarning($"[LevelGenerator] Không load được Car tại Z={worldZ}");
        }

        // Spawn xu hình Sin trên nóc xe con
        if (CoinPool.Instance == null)
        {
            onSpawned?.Invoke(spawnedLength);
            yield break;
        }

        for (int i = 0; i < _coinsOnCar; i++)
        {
            float t = (float)i / Mathf.Max(_coinsOnCar - 1, 1);
            
            // Bỏ qua biến Offset thủ công của user, tự chia đều quãng đường thân xe
            // Bắt đầu từ 10% chiều dài đuôi xe và kết thúc ở 90% đầu xe
            float startZ = worldZ + spawnedLength * 0.15f;
            float endZ = worldZ + spawnedLength * 0.85f;
            float coinZ = Mathf.Lerp(startZ, endZ, t);
            
            float sinY  = Mathf.Sin(t * Mathf.PI) * _carSineAmplitude;
            float coinY = _carRoofY + sinY;

            var coin = CoinPool.Instance.Get(
                new Vector3(x, coinY, coinZ),
                Quaternion.identity,
                segment.transform);
            if (coin != null) list.Add(coin);
        }

        onSpawned?.Invoke(spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Bus + Xu Đường Thẳng
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBusWithStraightCoins(TrackSegment segment, float worldZ, int lane, List<GameObject> list, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;

        // Spawn xe bus
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        var busHandle = busRef.InstantiateAsync(new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform);
        yield return busHandle;

        float spawnedLength = _busLengthZ;

        if (busHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject busObj = busHandle.Result;
            list.Add(busObj);
            spawnedLength = AlignAndGetLengthZ(busObj, worldZ, _busLengthZ);
        }
        else
        {
            Debug.LogWarning($"[LevelGenerator] Không load được Bus tại Z={worldZ}");
        }

        // Spawn xu đường thẳng trên nóc xe bus
        if (CoinPool.Instance == null)
        {
            onSpawned?.Invoke(worldZ + spawnedLength);
            yield break;
        }

        // Tự động căn khoảng xu dựa trên kích thước thật của bus thay vì dùng Offset thủ công
        float coinStartZ = worldZ + spawnedLength * 0.15f;
        float coinEndZ   = worldZ + spawnedLength * 0.85f;
        float coinZ      = coinStartZ;

        while (coinZ <= coinEndZ)
        {
            var coin = CoinPool.Instance.Get(
                new Vector3(x, _busRoofY, coinZ),
                Quaternion.identity,
                segment.transform);
            if (coin != null) list.Add(coin);
            coinZ += _coinSpacingOnBus;
        }

        onSpawned?.Invoke(worldZ + spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Xu Tự Do
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnFreeCoinLine(TrackSegment segment, float worldZ, int primaryLane, List<GameObject> list, int coinCount = -1)
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
                var handle = pRef.InstantiateAsync(new Vector3(coinX, _freeCoinHeightY, coinZ), Quaternion.identity, segment.transform);
                yield return handle;
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    list.Add(handle.Result);
                }
                continue; // Bỏ qua sinh xu tại vị trí này
            }

            var coin = CoinPool.Instance.Get(
                new Vector3(coinX, _freeCoinHeightY, coinZ),
                Quaternion.identity,
                segment.transform);
            if (coin != null) list.Add(coin);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Car Không Xu (Xe bàn đạp)
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnCarOnly(TrackSegment segment, float worldZ, int lane, List<GameObject> list, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        var carHandle = carRef.InstantiateAsync(new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform);
        yield return carHandle;

        float spawnedLength = _carLengthZ;
        if (carHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = carHandle.Result;
            list.Add(obj);
            spawnedLength = AlignAndGetLengthZ(obj, worldZ, _carLengthZ);
        }
        
        onSpawned?.Invoke(spawnedLength);
    }

    // ─────────────────────────────────────────────────────────
    // Spawn Bus Không Xu (Bus cản đường)
    // ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBusOnly(TrackSegment segment, float worldZ, int lane, List<GameObject> list, System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        var busHandle = busRef.InstantiateAsync(new Vector3(x, 0f, worldZ), Quaternion.identity, segment.transform);
        yield return busHandle;

        float spawnedLength = _busLengthZ;
        if (busHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = busHandle.Result;
            list.Add(obj);
            spawnedLength = AlignAndGetLengthZ(obj, worldZ, _busLengthZ);
        }
        
        onSpawned?.Invoke(worldZ + spawnedLength);
    }
}
