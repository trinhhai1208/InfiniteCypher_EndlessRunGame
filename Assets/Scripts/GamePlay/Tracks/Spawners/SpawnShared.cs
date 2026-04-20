using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P2: Shared utility cho tất cả Spawner modules.
/// Cache chiều dài thực tế (Z) của prefab và căn chỉnh pivot.
/// Tránh đo Bounds tốn kém hơn 1 lần cho mỗi loại prefab.
/// </summary>
public class ObjectSizeCache
{
    private struct ObjectStats
    {
        public float LengthZ;
        public float OffsetToTail;
    }

    private readonly Dictionary<string, ObjectStats> _cache = new();

    /// <summary>
    /// Đo chiều dài Z của vật thể và căn chỉnh pivot.
    /// Kết quả được cache theo tên prefab (O(1) cho các lần sau).
    /// </summary>
    public float AlignAndGetLength(GameObject obj, float targetBackZ, float fallbackLength)
    {
        if (obj == null) return fallbackLength;

        string prefabName = obj.name.Replace("(Clone)", "").Trim();

        if (_cache.TryGetValue(prefabName, out ObjectStats stats))
        {
            float newPivotZ = targetBackZ - stats.OffsetToTail;
            obj.transform.position = new Vector3(
                obj.transform.position.x,
                obj.transform.position.y,
                newPivotZ);
            return stats.LengthZ;
        }

        // Đo lần đầu — tốn chi phí nhưng chỉ chạy 1 lần mỗi loại prefab
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
            float offsetToTail = totalBounds.Value.min.z - obj.transform.position.z;

            _cache[prefabName] = new ObjectStats { LengthZ = lengthZ, OffsetToTail = offsetToTail };

            float shiftZ = targetBackZ - totalBounds.Value.min.z;
            obj.transform.position += new Vector3(0, 0, shiftZ);
            return lengthZ;
        }

        return fallbackLength;
    }
}

/// <summary>
/// P2: Budget controller — giới hạn số vật thể sinh ra mỗi frame.
/// Khi đạt budget, LevelGenerator yield 1 frame trước khi tiếp tục.
/// </summary>
public class SpawnBudget
{
    private int _budget;
    private int _usedThisFrame;

    public SpawnBudget(int budgetPerFrame)
    {
        _budget = budgetPerFrame;
    }

    public void Register() => _usedThisFrame++;
    public bool IsExhausted() => _usedThisFrame >= _budget;
    public void ResetFrame() => _usedThisFrame = 0;
}
