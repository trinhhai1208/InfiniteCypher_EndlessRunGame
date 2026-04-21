using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// P2 Refactor: Tách từ LevelGenerator.
/// Xử lý toàn bộ logic sinh Xe Bus + chuỗi bus + xu thẳng trên nóc.
/// </summary>
public class BusSpawner
{
    private readonly List<AssetReference> _busRefs;
    private readonly float _laneDistance;
    private readonly float _busRoofY;
    private readonly float _busLengthZ;
    private readonly float _busGapZ;
    private readonly float _coinSpacingOnBus;
    private readonly int _maxBusChain;
    private readonly SpawnBudget _budget;
    private readonly float _bridgeSineAmplitude;

    public BusSpawner(
        List<AssetReference> busRefs,
        float laneDistance,
        float busRoofY,
        float busLengthZ,
        float busGapZ,
        float coinSpacingOnBus,
        int maxBusChain,
        SpawnBudget budget,
        float bridgeSineAmplitude = 0.8f)
    {
        _busRefs = busRefs;
        _laneDistance = laneDistance;
        _busRoofY = busRoofY;
        _busLengthZ = busLengthZ;
        _busGapZ = busGapZ;
        _coinSpacingOnBus = coinSpacingOnBus;
        _maxBusChain = maxBusChain;
        _budget = budget;
        _bridgeSineAmplitude = bridgeSineAmplitude;
    }

    /// <summary>
    /// Spawn xe bus + xu đường thẳng trên nóc.
    /// </summary>
    public IEnumerator SpawnWithCoins(
        TrackSegment segment,
        float worldZ,
        int lane,
        LevelGenerator.SpawnedObjects group,
        ObjectSizeCache sizeCache,
        bool makeMoving = false,
        float speed = 0f,
        System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        GameObject busObj = null;

        yield return AddressablePoolManager.Instance.GetRoutine(
            busRef,
            new Vector3(x, 0f, worldZ),
            Quaternion.Euler(0f, 180f, 0f),
            segment.transform,
            res => busObj = res);

        _budget.Register();

        float spawnedLength = _busLengthZ;
        if (busObj == null) { onSpawned?.Invoke(0f); yield break; }

        group.Obstacles.Add(busObj);
        spawnedLength = sizeCache.AlignAndGetLength(busObj, worldZ, _busLengthZ);

        if (makeMoving)
        {
            var mover = busObj.GetComponent<MovingObstacle>();
            if (mover == null) mover = busObj.AddComponent<MovingObstacle>();
            mover.Activate(speed);
        }

        if (CoinPool.Instance == null) { onSpawned?.Invoke(spawnedLength); yield break; }

        float coinStartZ = worldZ + spawnedLength * 0.15f;
        float coinEndZ   = worldZ + spawnedLength * 0.85f;
        float coinZ      = coinStartZ;
        while (coinZ <= coinEndZ)
        {
            Transform parent = makeMoving ? busObj.transform : segment.transform;
            var coin = CoinPool.Instance.Get(new Vector3(x, _busRoofY, coinZ), Quaternion.identity, parent);
            if (coin != null) group.Coins.Add(coin);
            coinZ += _coinSpacingOnBus;
        }

        onSpawned?.Invoke(spawnedLength);
    }

    /// <summary>
    /// Spawn xe bus không xu (bus cản đường đơn độc).
    /// </summary>
    public IEnumerator SpawnOnly(
        TrackSegment segment,
        float worldZ,
        int lane,
        LevelGenerator.SpawnedObjects group,
        ObjectSizeCache sizeCache,
        bool makeMoving = false,
        float speed = 0f,
        System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference busRef = _busRefs[Random.Range(0, _busRefs.Count)];
        GameObject busObj = null;

        yield return AddressablePoolManager.Instance.GetRoutine(
            busRef,
            new Vector3(x, 0f, worldZ),
            Quaternion.Euler(0, 180f, 0),
            segment.transform,
            res => busObj = res);

        _budget.Register();

        float spawnedLength = _busLengthZ;
        if (busObj == null) { onSpawned?.Invoke(0f); yield break; }

        if (busObj != null)
        {
            group.Obstacles.Add(busObj);
            spawnedLength = sizeCache.AlignAndGetLength(busObj, worldZ, _busLengthZ);
            
            if (makeMoving)
            {
                var mover = busObj.GetComponent<MovingObstacle>();
                if (mover == null) mover = busObj.AddComponent<MovingObstacle>();
                mover.Activate(speed);
            }
        }

        onSpawned?.Invoke(spawnedLength);
    }

    public int GetMaxChain() => _maxBusChain;
    public float GetGapZ()   => _busGapZ;

    public IEnumerator SpawnSinusoidalBridge(
        TrackSegment segment,
        float fromZ,
        float toZ,
        float laneX,
        float roofY,
        int coinCount,
        LevelGenerator.SpawnedObjects group)
    {
        if (CoinPool.Instance == null) yield break;

        float gapLength = toZ - fromZ;
        if (gapLength <= 0f) yield break;

        for (int i = 0; i < coinCount; i++)
        {
            float t = (float)i / Mathf.Max(coinCount - 1, 1);
            float z = Mathf.Lerp(fromZ, toZ, t);
            float sinY = Mathf.Sin(t * Mathf.PI) * _bridgeSineAmplitude;
            float y = roofY + sinY;

            var coin = CoinPool.Instance.Get(
                new Vector3(laneX, y, z),
                Quaternion.identity,
                segment.transform);
            if (coin != null) group.Coins.Add(coin);
        }

        yield return null;
    }
}
