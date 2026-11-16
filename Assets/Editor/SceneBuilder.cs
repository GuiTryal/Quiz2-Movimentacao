using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta a cena de teste com as plataformas do KayKit Platformer Pack.
/// Menu: Tools > Montar Cena de Movimentação
/// </summary>
public static class SceneBuilder
{
    private const string ModelsPath = "Assets/KayKit_Platformer_Pack/Models/";
    private const string ScenePath = "Assets/Scenes/Movimentacao.unity";

    [MenuItem("Tools/Montar Cena de Movimentação")]
    public static void Build()
    {
        Scene();
    }

    private static void Scene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Luz
        var light = Object.FindObjectOfType<Light>();
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.shadows = LightShadows.Soft;

        // Cenário: (modelo, posição do topo da plataforma)
        var level = new GameObject("Level").transform;
        Place("blue/platform_6x6x1_blue", new Vector3(0f, 0f, 0f), level, "Chao_Inicial");
        Place("blue/platform_4x4x1_blue", new Vector3(0f, 0f, 6f), level);
        Place("blue/platform_2x2x1_blue", new Vector3(0f, 1f, 10f), level);
        Place("green/platform_4x4x1_green", new Vector3(3f, 2f, 13.5f), level);
        Place("green/platform_2x2x1_green", new Vector3(7f, 3f, 14f), level);
        Place("red/platform_4x4x2_red", new Vector3(11f, 4f, 14f), level);
        Place("red/platform_2x2x1_red", new Vector3(11f, 5f, 18f), level);
        Place("yellow/platform_6x6x1_yellow", new Vector3(11f, 6f, 23f), level, "Chegada");
        Place("yellow/flag_A_yellow", new Vector3(12.5f, 6f, 24.5f), level, null, false);
        Place("neutral/signage_finish", new Vector3(11f, 6f, 25.5f), level, null, false);
        Place("green/platform_4x4x1_green", new Vector3(-6f, 0.5f, 0f), level);
        Place("yellow/diamond_yellow", new Vector3(-6f, 1f, 0f), level, null, false);

        // Jogador (cápsula: o pack não traz personagem)
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.5f, 0f);
        var rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        var physMat = new PhysicMaterial("SemAtrito")
        {
            dynamicFriction = 0f, staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum
        };
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(physMat, "Assets/Materials/SemAtrito.physicMaterial");
        player.GetComponent<CapsuleCollider>().sharedMaterial = physMat;

        var playerMat = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.55f, 0.1f) };
        AssetDatabase.CreateAsset(playerMat, "Assets/Materials/Player.mat");
        player.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

        // "Nariz" para mostrar a direção do personagem
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Frente";
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
        nose.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);

        player.AddComponent<PlayerController>();
        player.AddComponent<RespawnOnFall>();

        // Câmera
        var cam = Camera.main;
        cam.transform.position = new Vector3(0f, 5.5f, -8f);
        cam.backgroundColor = new Color(0.55f, 0.8f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Cena criada em " + ScenePath);
    }

    /// <summary>Instancia um modelo do pack com o topo (bounds.max.y) em topCenter.</summary>
    private static GameObject Place(string model, Vector3 topCenter, Transform parent,
                                    string name = null, bool collider = true)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsPath + model + ".fbx");
        if (prefab == null)
        {
            Debug.LogError("Modelo não encontrado: " + model);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (name != null) go.name = name;
        go.isStatic = true;

        Bounds b = GetBounds(go);
        Vector3 anchor = collider
            ? new Vector3(b.center.x, b.max.y, b.center.z)
            : new Vector3(b.center.x, b.min.y, b.center.z);
        go.transform.position += topCenter - anchor;

        if (collider)
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;

        return go;
    }

    private static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
