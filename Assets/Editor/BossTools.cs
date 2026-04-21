using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class BossTools
{
    [MenuItem("Tools/Boss/Focus In Scene")]
    public static void FocusBossMenu()
    {
        var boss = Object.FindObjectOfType<BossController>();
        if (boss == null)
        {
            EditorUtility.DisplayDialog("Focus Boss", "No BossController found in the open scene.", "OK");
            return;
        }

        Selection.activeGameObject = boss.gameObject;
        SceneView.FrameLastActiveSceneView();
    }

    [MenuItem("CONTEXT/BossController/Focus In Scene")]
    private static void ContextFocus(MenuCommand cmd)
    {
        var boss = cmd.context as BossController;
        if (boss == null) return;
        Selection.activeGameObject = boss.gameObject;
        SceneView.FrameLastActiveSceneView();
    }

    [MenuItem("CONTEXT/BossController/Appear (Inspector)")]
    private static void ContextAppear(MenuCommand cmd)
    {
        var boss = cmd.context as BossController;
        if (boss == null) return;
        boss.Appear();
        Selection.activeGameObject = boss.gameObject;
        SceneView.FrameLastActiveSceneView();
    }

    [MenuItem("CONTEXT/BossController/Force Hide (Inspector)")]
    private static void ContextHide(MenuCommand cmd)
    {
        var boss = cmd.context as BossController;
        if (boss == null) return;
        boss.ForceHide();
    }
}
#endif
