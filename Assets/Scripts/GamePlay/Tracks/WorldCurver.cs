using UnityEngine;

/// <summary>
/// Quản lý thông số độ cong của thế giới (Curved World).
/// Tự động truyền giá trị dao động vào Global Shader.
/// Hãy gắn Script này vào GameManager hoặc một Object trống trong scene.
/// </summary>
public class WorldCurver : MonoBehaviour
{
    [Header("Curvature Y (Cong Lên/Xuống)")]
    [Tooltip("Số dương = đường uốn cong xuống phía dưới (giống quả đất).\nSố âm = đường uốn cong vòng lên trên (kiểu lòng chảo).")]
    [Range(-0.01f, 0.01f)]
    public float CurveY = 0.0015f; 

    [Header("Curvature X (Cong Trái/Phải)")]
    [Tooltip("Bật tính năng cho đường tự động uốn lượn trái phải theo thời gian.")]
    public bool EnableDynamicX = true;
    
    [Tooltip("Độ cong gốc. Nếu không bật Dynamic thì sẽ giữ cứng mốc này.")]
    [Range(-0.01f, 0.01f)]
    public float BaseCurveX = 0f;
    
    [Tooltip("Biên độ cong tối đa sang 2 bên.")]
    public float OscillationAmplitude = 0.002f;
    
    [Tooltip("Tốc độ uốn lượn.")]
    public float OscillationSpeed = 0.2f;

    [Header("Tùy chọn khác")]
    [Tooltip("Đoạn thẳng tính từ Camera mà đường NHÀ PHẲNG trước khi bị bẻ cong (để tránh lỗi hình xung quanh Player).")]
    public float DistanceOffset = 15f;

    private int _curveParamsID;

    private void Awake()
    {
        _curveParamsID = Shader.PropertyToID("_CurveParams");
    }

    private void Update()
    {
        float currentX = BaseCurveX;

        // Tính toán độ uốn lượn mượt mà theo hàm Sine
        if (EnableDynamicX)
        {
            currentX += Mathf.Sin(Time.time * OscillationSpeed) * OscillationAmplitude;
        }

        // Truyền thông số vào Shader toàn cục của Unity
        // x: độ cong X
        // y: độ cong Y
        // z: khoảng an toàn không bẻ cong
        // w: không dùng
        Shader.SetGlobalVector(_curveParamsID, new Vector4(currentX, CurveY, DistanceOffset, 0f));
    }
    
    private void OnDisable()
    {
        // Reset về thẳng tắp khi tắt script để tránh lỗi
        Shader.SetGlobalVector(_curveParamsID, Vector4.zero);
    }
}
