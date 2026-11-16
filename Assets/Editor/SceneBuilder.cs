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
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.75f);

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

        // Jogador: objeto com física (cápsula invisível) + modelo do Knight (KayKit Adventurers)
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.5f, 0f);
        var rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        var col = player.AddComponent<CapsuleCollider>();
        col.height = PlayerHeight;
        col.radius = 0.45f;

        var physMat = new PhysicMaterial("SemAtrito")
        {
            dynamicFriction = 0f, staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum
        };
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.DeleteAsset("Assets/Materials/Player.mat");
        AssetDatabase.CreateAsset(physMat, "Assets/Materials/SemAtrito.physicMaterial");
        col.sharedMaterial = physMat;

        AddCharacterModel(player.transform);

        player.AddComponent<PlayerController>();
        player.AddComponent<RespawnOnFall>();
        player.AddComponent<PlayerAnimator>();

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

    private const float PlayerHeight = 1.8f;
    private const string KnightPath = "Assets/KayKit_Adventurers/Knight.fbx";
    private const string ControllerPath = "Assets/KayKit_Adventurers/Knight.controller";

    private static void AddCharacterModel(Transform player)
    {
        ConfigureKnightImport();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player);
        model.name = "Knight";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Deixa só a espada de uma mão e o escudo redondo
        string[] keep = { "1H_Sword", "Round_Shield" };
        string[] weapons = { "1H_Sword", "1H_Sword_Offhand", "2H_Sword", "Badge_Shield",
                             "Rectangle_Shield", "Round_Shield", "Spike_Shield" };
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (System.Array.IndexOf(weapons, t.name) >= 0 && System.Array.IndexOf(keep, t.name) < 0)
                t.gameObject.SetActive(false);

        // Escala o modelo para a altura da cápsula e coloca os pés na base dela
        Bounds b = GetBounds(model);
        float scale = PlayerHeight / b.size.y;
        model.transform.localScale = Vector3.one * scale;
        b = GetBounds(model);
        model.transform.position += new Vector3(0f, (player.position.y - PlayerHeight * 0.5f) - b.min.y, 0f);

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = CreateAnimatorController();
    }

    private static void ConfigureKnightImport()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(KnightPath);
        var clips = importer.defaultClipAnimations;
        foreach (var c in clips)
            c.loopTime = c.name.EndsWith("Idle") || c.name.StartsWith("Running") || c.name.StartsWith("Walking");
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip FindClip(string name)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(KnightPath))
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview") &&
                (clip.name == name || clip.name.EndsWith("|" + name)))
                return clip;
        Debug.LogError("Animação não encontrada: " + name);
        return null;
    }

    private static UnityEditor.Animations.AnimatorController CreateAnimatorController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter(new AnimatorControllerParameter
        {
            name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true
        });

        var sm = ac.layers[0].stateMachine;
        var idle = sm.AddState("Parado");
        idle.motion = FindClip("Idle");
        var run = sm.AddState("Correndo");
        run.motion = FindClip("Running_A");
        var jump = sm.AddState("NoAr");
        jump.motion = FindClip("Jump_Idle");
        sm.defaultState = idle;

        var t = idle.AddTransition(run);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "Speed");

        t = run.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.2f, "Speed");

        t = sm.AddAnyStateTransition(jump);
        t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Grounded");

        t = jump.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Grounded");

        return ac;
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
