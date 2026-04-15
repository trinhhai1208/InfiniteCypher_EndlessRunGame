using UnityEngine;

/// <summary>
/// Đại diện cho một đoạn đường (Track Segment).
/// Chỉ cần đặt StartPoint và EndPoint — LevelGenerator sẽ tự sinh obstacles/coins.
/// </summary>
public class TrackSegment : MonoBehaviour
{
    [Header("Connection Points")]
    [Tooltip("Điểm bắt đầu của đoạn đường (dùng để kết nối với đoạn trước)")]
    [SerializeField] private Transform _startPoint;

    [Tooltip("Điểm kết thúc của đoạn đường (TrackManager dùng để spawn đoạn tiếp theo)")]
    [SerializeField] private Transform _endPoint;

    // Public accessors
    public Transform StartPoint => _startPoint;
    public Transform EndPoint   => _endPoint;

    /// <summary>
    /// Chiều dài thực tế của segment theo trục Z
    /// </summary>
    public float Length
    {
        get
        {
            if (_startPoint == null || _endPoint == null) return 0f;
            return Mathf.Abs(_endPoint.position.z - _startPoint.position.z);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_startPoint.position, 0.3f);
            UnityEditor.Handles.Label(_startPoint.position + Vector3.up * 0.6f, "START");
        }

        if (_endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_endPoint.position, 0.3f);
            UnityEditor.Handles.Label(_endPoint.position + Vector3.up * 0.6f, "END");
        }

        if (_startPoint != null && _endPoint != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 center = (_startPoint.position + _endPoint.position) / 2f;
            Vector3 size   = new Vector3(8f, 0.1f, Length);
            Gizmos.DrawCube(center, size);
        }
    }
#endif
}
