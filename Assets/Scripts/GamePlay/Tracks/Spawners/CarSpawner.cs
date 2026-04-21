using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// P2 Refactor: Tách từ LevelGenerator.
/// Xử lý toàn bộ logic sinh Xe Con (Car) cùng xu hình Sin trên nóc.
/// Tuân theo nguyên tắc Single Responsibility.
/// </summary>
public class CarSpawner
{
    private readonly List<AssetReference> _carRefs;
    private readonly float _laneDistance;
    private readonly float _carRoofY;
    private readonly float _carLengthZ;
    private readonly SpawnBudget _budget;

    public CarSpawner(
        List<AssetReference> carRefs,
        float laneDistance,
        float carRoofY,
        float carLengthZ,
        SpawnBudget budget)
    {
        _carRefs = carRefs;
        _laneDistance = laneDistance;
        _carRoofY = carRoofY;
        _carLengthZ = carLengthZ;
        _budget = budget;
    }

    /// <summary>
    /// Spawn xe con + xu hình Sin trên nóc. Ghi kết quả vào group.
    /// </summary>
    public IEnumerator SpawnWithCoins(
        TrackSegment segment,
        float worldZ,
        int lane,
        LevelGenerator.SpawnedObjects group,
        ObjectSizeCache sizeCache,
        System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        GameObject carObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(
            carRef,
            new Vector3(x, 0f, worldZ),
            Quaternion.Euler(0f, 180f, 0f),
            segment.transform,
            res => carObj = res);

        _budget.Register();

        float spawnedLength = _carLengthZ;
        if (carObj == null) { onSpawned?.Invoke(spawnedLength); yield break; }

        group.Obstacles.Add(carObj);
        spawnedLength = sizeCache.AlignAndGetLength(carObj, worldZ, _carLengthZ);

        onSpawned?.Invoke(spawnedLength);
    }

    /// <summary>
    /// Spawn xe con không xu (xe bàn đạp lấy đà nhảy lên Bus).
    /// </summary>
    public IEnumerator SpawnOnly(
        TrackSegment segment,
        float worldZ,
        int lane,
        LevelGenerator.SpawnedObjects group,
        ObjectSizeCache sizeCache,
        System.Action<float> onSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference carRef = _carRefs[Random.Range(0, _carRefs.Count)];
        GameObject carObj = null;
        yield return AddressablePoolManager.Instance.GetRoutine(
            carRef,
            new Vector3(x, 0f, worldZ),
            Quaternion.Euler(0f, 180f, 0f),
            segment.transform,
            res => carObj = res);

        _budget.Register();

        float spawnedLength = _carLengthZ;
        if (carObj != null)
        {
            group.Obstacles.Add(carObj);
            spawnedLength = sizeCache.AlignAndGetLength(carObj, worldZ, _carLengthZ);
        }

        onSpawned?.Invoke(spawnedLength);
    }
}
