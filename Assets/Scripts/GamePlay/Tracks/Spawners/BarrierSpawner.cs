using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// P2 Refactor: Tách từ LevelGenerator.
/// Xử lý spawn rào chắn (Barrier) và xu sát đất gợi ý slide.
/// </summary>
public class BarrierSpawner
{
    private readonly List<AssetReference> _barrierRefs;
    private readonly float _laneDistance;
    private readonly float _barrierCoinZOffset;
    private readonly float _barrierLowCoinY;
    private readonly SpawnBudget _budget;

    public BarrierSpawner(
        List<AssetReference> barrierRefs,
        float laneDistance,
        float barrierCoinZOffset,
        float barrierLowCoinY,
        SpawnBudget budget)
    {
        _barrierRefs = barrierRefs;
        _laneDistance = laneDistance;
        _barrierCoinZOffset = barrierCoinZOffset;
        _barrierLowCoinY = barrierLowCoinY;
        _budget = budget;
    }

    /// <summary>
    /// Spawn rào chắn tại lane chỉ định và chuỗi xu sát đất để gợi ý slide.
    /// Trả về số xu đã sinh thêm.
    /// </summary>
    public IEnumerator Spawn(
        TrackSegment segment,
        float worldZ,
        int lane,
        LevelGenerator.SpawnedObjects group,
        int maxCoinsPerSegment,
        int currentCoinCount,
        System.Action<int> onCoinSpawned = null)
    {
        float x = lane * _laneDistance;
        AssetReference barrierRef = _barrierRefs[Random.Range(0, _barrierRefs.Count)];
        GameObject barrierObj = null;

        yield return AddressablePoolManager.Instance.GetRoutine(
            barrierRef,
            new Vector3(x, 0f, worldZ),
            Quaternion.identity,
            segment.transform,
            res => barrierObj = res);

        _budget.Register();

        if (barrierObj != null)
            group.Obstacles.Add(barrierObj);

        if (CoinPool.Instance == null) yield break;

        int barrierSlideCount = 5;
        float coinStartZ = worldZ + _barrierCoinZOffset;
        int spawned = 0;

        for (int c = 0; c < barrierSlideCount; c++)
        {
            if (currentCoinCount + spawned >= maxCoinsPerSegment) break;
            float coinZ = coinStartZ + c * 1.2f;
            var coin = CoinPool.Instance.Get(new Vector3(x, _barrierLowCoinY, coinZ), Quaternion.identity, segment.transform);
            if (coin != null) { group.Coins.Add(coin); spawned++; }
        }

        onCoinSpawned?.Invoke(spawned);
    }
}
