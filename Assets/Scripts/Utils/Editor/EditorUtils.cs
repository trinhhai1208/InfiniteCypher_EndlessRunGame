#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorUtils
{
    [MenuItem("FutureCity/Clear All Save Data")]
    public static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=green>Đã xóa sạch toàn bộ dữ liệu (Vàng, Cấp nâng cấp, Nhân vật...)!</color>");
    }
}
#endif
