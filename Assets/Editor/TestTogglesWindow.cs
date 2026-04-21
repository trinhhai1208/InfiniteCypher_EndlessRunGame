using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class TestTogglesWindow : EditorWindow
{
    private bool _obstaclesEnabled = true;
    private bool _bossEnabled = true;

    [MenuItem("Tools/Testing/Test Toggles Window")]
    private static void OpenWindow()
    {
        GetWindow<TestTogglesWindow>("Test Toggles");
    }

    [MenuItem("Tools/Testing/Toggle Obstacles %&o")] // Ctrl+Alt+O
    private static void MenuToggleObstacles()
    {
        var window = GetWindow<TestTogglesWindow>(false, "Test Toggles", true);
        window.ToggleAllObstacles();
    }

    [MenuItem("Tools/Testing/Toggle Boss %&b")] // Ctrl+Alt+B
    private static void MenuToggleBoss()
    {
        var window = GetWindow<TestTogglesWindow>(false, "Test Toggles", true);
        window.ToggleBoss();
    }

    private void OnGUI()
    {
        GUILayout.Label("Test Toggles (Scene)", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh State"))
            RefreshState();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        _obstaclesEnabled = EditorGUILayout.ToggleLeft("Obstacles Enabled", _obstaclesEnabled);
        _bossEnabled = EditorGUILayout.ToggleLeft("Boss Enabled", _bossEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            // Live apply when changing toggles in the window
            ApplyToggles();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Disable All Obstacles in Scene"))
            SetObstaclesActive(false);

        if (GUILayout.Button("Enable All Obstacles in Scene"))
            SetObstaclesActive(true);

        if (GUILayout.Button("Disable Boss"))
            SetBossActive(false);

        if (GUILayout.Button("Enable Boss"))
            SetBossActive(true);
    }

    private void RefreshState()
    {
        _obstaclesEnabled = AreAnyObstaclesActive();
        _bossEnabled = IsBossActive();
        Repaint();
    }

    private void ApplyToggles()
    {
        SetObstaclesActive(_obstaclesEnabled);
        SetBossActive(_bossEnabled);
    }

    private bool AreAnyObstaclesActive()
    {
        var obs = UnityEngine.Object.FindObjectsOfType<ObstacleIdentity>();
        foreach (var o in obs)
        {
            if (o.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    private bool IsBossActive()
    {
        var boss = UnityEngine.Object.FindObjectOfType<BossController>();
        return boss != null && boss.gameObject.activeInHierarchy;
    }

    private void SetObstaclesActive(bool active)
    {
        var obs = UnityEngine.Object.FindObjectsOfType<ObstacleIdentity>();
        var roots = new HashSet<GameObject>();
        foreach (var o in obs)
        {
            if (o == null) continue;
            var root = o.gameObject;
            if (!roots.Contains(root))
            {
                Undo.RecordObject(root, "Toggle Obstacle Active");
                root.SetActive(active);
                roots.Add(root);
            }
        }
        _obstaclesEnabled = active;
    }

    private void SetBossActive(bool active)
    {
        var boss = UnityEngine.Object.FindObjectOfType<BossController>();
        if (boss == null)
        {
            EditorUtility.DisplayDialog("Toggle Boss", "No BossController found in the open scene.", "OK");
            return;
        }
        Undo.RecordObject(boss.gameObject, "Toggle Boss Active");
        boss.gameObject.SetActive(active);
        _bossEnabled = active;
    }

    // Shortcut-invoked helpers
    private void ToggleAllObstacles()
    {
        bool any = AreAnyObstaclesActive();
        SetObstaclesActive(!any);
        RefreshState();
    }

    private void ToggleBoss()
    {
        bool active = IsBossActive();
        SetBossActive(!active);
        RefreshState();
    }
}
#endif
