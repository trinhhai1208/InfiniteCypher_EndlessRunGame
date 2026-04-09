using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Công cụ tạo Icon nhân vật tự động từ Prefab (Editor Window).
/// </summary>
public class CharacterIconGenerator : EditorWindow
{
    [Header("Settings")]
    public List<GameObject> prefabsToCapture = new List<GameObject>();
    public int resolution = 512;
    public string savePath = "Assets/UI/Icons/";
    public Vector3 cameraOffset = new Vector3(0, 1.2f, 2.5f);
    public Vector3 cameraRotation = new Vector3(10, 180, 0);

    private Vector2 _scrollPos;

    [MenuItem("Tools/Character Icon Generator")]
    public static void ShowWindow()
    {
        GetWindow<CharacterIconGenerator>("Icon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Character Icon Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        resolution = EditorGUILayout.IntField("Resolution", resolution);
        cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);
        cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        GUILayout.Label("Prefabs to Capture:", EditorStyles.boldLabel);

        // Hiển thị danh sách Prefabs
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
        
        // Nút thêm ô trống
        if (GUILayout.Button("Add New Slot"))
        {
            prefabsToCapture.Add(null);
        }

        for (int i = 0; i < prefabsToCapture.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabsToCapture[i] = (GameObject)EditorGUILayout.ObjectField($"Slot {i}", prefabsToCapture[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                prefabsToCapture.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("GENERATE ALL ICONS", GUILayout.Height(40)))
        {
            GenerateIcons();
        }
    }

    private void GenerateIcons()
    {
        if (prefabsToCapture.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Chưa kéo Prefab nào vào danh sách!", "OK");
            return;
        }

        // Tạo thư mục nếu chưa có
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 1. Tạo Stage tạm thời
        GameObject stage = new GameObject("IconGenStage");
        Camera cam = new GameObject("IconGenCam").AddComponent<Camera>();
        
        cam.transform.SetParent(stage.transform);
        cam.transform.localPosition = cameraOffset;
        cam.transform.localRotation = Quaternion.Euler(cameraRotation);

        // Thiết lập Camera để chụp nền trong suốt
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0, 0, 0, 0); // Alpha = 0
        cam.farClipPlane = 10f;

        // 2. Render Texture
        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        cam.targetTexture = rt;

        foreach (GameObject prefab in prefabsToCapture)
        {
            if (prefab == null) continue;

            // Sinh nhân vật
            GameObject instance = Instantiate(prefab, stage.transform.position, Quaternion.identity, stage.transform);
            instance.transform.localPosition = Vector3.zero;

            // Đảm bảo nhân vật ở tư thế idle (nếu có animator)
            Animator anim = instance.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Update(0); // Force update tư thế đầu tiên
            }

            // Chụp
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();

            // Lưu file
            byte[] bytes = tex.EncodeToPNG();
            string fileName = Path.Combine(savePath, prefab.name + "_Icon.png");
            File.WriteAllBytes(fileName, bytes);

            // Dọn dẹp nhân vật vừa sinh
            DestroyImmediate(instance);
            
            Debug.Log($"[Icon Generator] Đã lưu: {fileName}");
        }

        // 3. Giải phóng tài nguyên
        RenderTexture.active = null;
        cam.targetTexture = null;
        DestroyImmediate(stage);
        rt.Release();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Đã tạo xong {prefabsToCapture.Count} Icons!", "Tuyệt");
    }
}
