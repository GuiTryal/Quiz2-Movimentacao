using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Salva imagens da cena em Logs/ (Tools > Salvar Preview da Cena).</summary>
public static class ScenePreview
{
    [MenuItem("Tools/Salvar Preview da Cena")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Movimentacao.unity");
        Shot("preview_cena.png", new Vector3(-10f, 14f, -12f), new Vector3(5f, 2f, 12f));
        Shot("preview_jogador.png", new Vector3(2.5f, 2.5f, 3.5f), new Vector3(0f, 1.2f, 0f));
        Shot("preview_camera_jogo.png", new Vector3(0f, 5f, -7f), new Vector3(0f, 1.5f, 2f));
    }

    private static void Shot(string file, Vector3 position, Vector3 lookAt)
    {
        var go = new GameObject("PreviewCam");
        var cam = go.AddComponent<Camera>();
        cam.CopyFrom(Camera.main);
        go.transform.position = position;
        go.transform.LookAt(lookAt);

        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs", file));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        Debug.Log("Preview salvo em " + path);
    }
}
