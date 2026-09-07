#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RPGGameUIEditor : EditorWindow
{
    private static void PurgeOldUI()
    {
        string[] uiNames = new string[] { "Canvas_GameUI", "Canvas", "TopBar", "Panel_HUD", "Panel_Inventory", "Panel_Store", "Card_PlayerStats", "Card_WaveInfo", "Card_AmmoPanel" };
        foreach (var name in uiNames)
        {
            GameObject obj = GameObject.Find(name);
            while (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
                obj = GameObject.Find(name);
            }
        }

        // Si existe Ground y además Ground_Plane duplicado, eliminar el duplicado
        GameObject ground = GameObject.Find("Ground");
        GameObject groundPlane = GameObject.Find("Ground_Plane");
        if (ground != null && groundPlane != null && ground != groundPlane)
        {
            Undo.DestroyObjectImmediate(groundPlane);
        }
    }

    [MenuItem("RPG Survival/🛠️ Setup UI Completa (1 Clic)", false, 10)]
    public static void SetupCompleteUI()
    {
        PurgeOldUI();
        RuntimeGameSetup.EnsureSceneSetup();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Setup Completo", "¡La UI completa (Barra de vida dinámica, Rondas, Munición, Hotbar táctica, Tienda e Inventario con sprites) se ha configurado y vinculado exitosamente con 1 clic!", "¡Excelente!");
    }

    [MenuItem("RPG Survival/🧹 Limpiar y Regenerar UI", false, 20)]
    public static void CleanAndRegenerateUI()
    {
        PurgeOldUI();

        GameObject oldEventSystem = GameObject.Find("EventSystem");
        if (oldEventSystem != null)
        {
            Undo.DestroyObjectImmediate(oldEventSystem);
        }

        RuntimeGameSetup.EnsureSceneSetup();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("UI Regenerada", "Se ha limpiado y reconstruido toda la UI desde cero.", "Aceptar");
    }
}
#endif
