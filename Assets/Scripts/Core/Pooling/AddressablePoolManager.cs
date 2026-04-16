using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Quản lý tái sử dụng (Pooling) cho hệ thống Addressables.
/// Dùng để tối ưu Instantiate/Destroy cho Xe cộ, Rào chắn.
/// Giảm GC Alloc và tăng độ mượt khi chơi trên WebGL.
/// </summary>
public class AddressablePoolManager : MonoBehaviour
{
    private static AddressablePoolManager _instance;
    public static AddressablePoolManager Instance 
    { 
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[AddressablePoolManager]");
                _instance = go.AddComponent<AddressablePoolManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        } 
    }

    // Kho hàng: Key là mã tài sản, Value là ngăn xếp các GameObject đang nghỉ ngơi
    private readonly Dictionary<string, Stack<GameObject>> _pools = new();

    private void Awake()
    {
        if (_instance != null && _instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lấy ra tài sản. Nếu kho còn hàng -> xuất kho. Nếu hết hàng -> tự sinh mới.
    /// </summary>
    public IEnumerator GetRoutine(AssetReference assetRef, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> onComplete)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            onComplete?.Invoke(null);
            yield break;
        }

        string key = assetRef.RuntimeKey.ToString();

        if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            GameObject obj = null;
            // Pop an toàn: Đề phòng ai đó vô tình Destroy tay làm hỏng stack
            while (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj != null) break;
            }

            if (obj != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                onComplete?.Invoke(obj);
                yield break;
            }
        }

        // Hết hàng -> Instantiate mới
        var op = assetRef.InstantiateAsync(position, rotation, parent);
        yield return op;

        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject newObj = op.Result;
            
            // Gắn mã nguồn gốc vào vật thể
            var poolItem = newObj.AddComponent<AddressablePoolItem>();
            poolItem.RuntimeKey = key;

            onComplete?.Invoke(newObj);
        }
        else
        {
            onComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// Cất tài sản vào kho.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        var poolItem = obj.GetComponent<AddressablePoolItem>();
        if (poolItem == null || string.IsNullOrEmpty(poolItem.RuntimeKey))
        {
            // Vật thể này không đăng ký pool -> hủy theo cách thông thường
            Addressables.ReleaseInstance(obj);
            return;
        }

        string key = poolItem.RuntimeKey;
        if (!_pools.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            _pools[key] = stack;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform); // Cất về Manager
        stack.Push(obj);
    }
}
