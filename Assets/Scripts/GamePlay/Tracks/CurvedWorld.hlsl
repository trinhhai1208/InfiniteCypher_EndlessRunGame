#ifndef CURVED_WORLD_INCLUDED
#define CURVED_WORLD_INCLUDED

// File dùng để chèn vào Shader Graph (Custom Function Node)
// Giao tiếp với WorldCurver.cs qua biến toàn cục

float4 _CurveParams; // x: curveX, y: curveY, z: distanceOffset

// Trả về Position ở dạng Object Space (để nối vào Vertex Position trong Shader Graph)
void ApplyCurvedWorld_float(float3 positionOS, out float3 OutPositionOS)
{
#if SHADERGRAPH_PREVIEW
    // Bỏ qua nếu đang xem trước trong bảng Shader Graph để tránh lỗi hiển thị
    OutPositionOS = positionOS;
#else
    // 1. Lấy vị trí thật ngoài đời (World Space)
    float3 worldPos = TransformObjectToWorld(positionOS);
    
    // 2. Tính khoảng cách từ Camera theo trục Z
    float dist = worldPos.z - _WorldSpaceCameraPos.z;
    
    // Chỉ tính toán uốn cong nếu vật thể nằm xa hơn mốc khoảng cách an toàn an toàn
    dist = max(0, dist - _CurveParams.z); 
    
    // 3. Công thức uốn cong bậc 2 (Parabol)
    // Càng xa (dist càng lớn) thì độ lệch Y và X càng mạnh
    worldPos.y -= dist * dist * _CurveParams.y;
    worldPos.x -= dist * dist * _CurveParams.x;
    
    // 4. Chuyển ngược lại về Object Space cho Unity render
    OutPositionOS = TransformWorldToObject(worldPos);
#endif
}

#endif
