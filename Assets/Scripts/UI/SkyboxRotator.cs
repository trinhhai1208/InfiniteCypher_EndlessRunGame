using UnityEngine;

/// <summary>
/// Tự động xoay Skybox để tạo cảm giác không gian động.
/// </summary>
public class SkyboxRotator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 1.0f;

    private Material _skyboxMaterial;

    private void Start()
    {
        // Lấy Material hiện tại của Skybox trong dứ án
        _skyboxMaterial = RenderSettings.skybox;
        
        if (_skyboxMaterial == null)
        {
            // Debug.LogWarning("[SkyboxRotator] Không tìm thấy Skybox Material trong Lighting Settings!");
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (_skyboxMaterial != null)
        {
            // Tăng giá trị Rotation của Shader theo thời gian
            float currentRotation = _skyboxMaterial.GetFloat("_Rotation");
            _skyboxMaterial.SetFloat("_Rotation", currentRotation + _rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
