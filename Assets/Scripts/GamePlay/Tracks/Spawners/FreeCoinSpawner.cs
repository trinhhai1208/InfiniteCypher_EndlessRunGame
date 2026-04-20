using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// P2 Refactor: Tách từ LevelGenerator.
/// Xử lý spawn cụm xu tự do (đường thẳng / zigzag) và Power-up.
/// </summary>
public class FreeCoinSpawner
{
    private readonly List<AssetReference> _powerUpRefs;
    private readonly float _laneDistance;
    private readonly float _freeCoinHeightY;
    private readonly float _freeCoinSpacing;
    private readonly float _zigzagChance;
    private readonly float _powerUpSpawnChance;
    private readonly Vector2Int _freeCoinCountRange;
    private readonly SpawnBudget _budget;

    public FreeCoinSpawner(
        List<AssetReference> powerUpRefs,
        float laneDistance,
        float freeCoinHeightY,
        float freeCoinSpacing,
        float zigzagChance,
        float powerUpSpawnChance,
        Vector2Int freeCoinCountRange,
        SpawnBudget budget)
    {
        _powerUpRefs = powerUpRefs;
        _laneDistance = laneDistance;
        _freeCoinHeightY = freeCoinHeightY;
        _freeCoinSpacing = freeCoinSpacing;
        _zigzagChance = zigzagChance;
        _powerUpSpawnChance = powerUpSpawnChance;
        _freeCoinCountRange = freeCoinCountRange;
        _budget = budget;
    }

    /// <summary>
    /// Spawn một cụm xu tự do theo đường thẳng hoặc zigzag.
    /// Ở giữa cụm có cơ hội xuất hiện Power-up thay thế 1 xu.
    /// </summary>
    public IEnumerator Spawn(
        TrackSegment segment,
        float worldZ,
        int primaryLane,
        LevelGenerator.SpawnedObjects group,
        int coinCount = -1)
    {
        if (CoinPool.Instance == null) yield break;

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

            // Cơ hội thay thế xu ở giữa dãy bằng Power-up
            if (i == coinCount / 2 && _powerUpRefs.Count > 0 && Random.value < _powerUpSpawnChance)
            {
                var pRef = _powerUpRefs[Random.Range(0, _powerUpRefs.Count)];
                GameObject powerupObj = null;
                yield return AddressablePoolManager.Instance.GetRoutine(
                    pRef,
                    new Vector3(coinX, _freeCoinHeightY, coinZ),
                    Quaternion.identity,
                    segment.transform,
                    res => powerupObj = res);

                _budget.Register();

                if (powerupObj != null)
                    group.Obstacles.Add(powerupObj);

                continue; // Bỏ qua sinh xu tại vị trí này
            }

            var coin = CoinPool.Instance.Get(
                new Vector3(coinX, _freeCoinHeightY, coinZ),
                Quaternion.identity,
                segment.transform);
            if (coin != null) group.Coins.Add(coin);
        }
    }

    public int GetRandomCount()
        => Random.Range(_freeCoinCountRange.x, _freeCoinCountRange.y + 1);

    public float GetSpacing() => _freeCoinSpacing;
}
