using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Abre a cena de movimentação automaticamente ao abrir o projeto,
/// assim é só apertar Play.
/// </summary>
[InitializeOnLoad]
public static class AutoStartScene
{
    private const string ScenePath = "Assets/Scenes/Movimentacao.unity";

    static AutoStartScene()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += OpenGameScene;
    }

    private static void OpenGameScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;

        // Só troca se o editor abriu numa cena vazia (sem arquivo e sem alterações)
        Scene active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path) && !active.isDirty)
            EditorSceneManager.OpenScene(ScenePath);
    }
}
