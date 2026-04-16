using UnityEngine;

/// <summary>
/// Đánh dấu nguồn gốc (AssetReference Key) của một GameObject được sinh ra 
/// từ AddressablePoolManager để biết đường thu hồi (Return) đúng kho.
/// </summary>
public class AddressablePoolItem : MonoBehaviour
{
    public string RuntimeKey { get; set; }
}
