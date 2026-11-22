using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates the starter content once. Later builds use the scene and assets edited by hand.</summary>
public static class IslandProject
{
    public const string ScenePath = "Assets/Island/Scenes/NagiIsland.unity";
    const string PrefabDir = "Assets/Island/Art/Prefabs";
    const string RecipeDir = "Assets/Island/Resources/Island/Furniture";

    public static void Prepare()
    {
        if (File.Exists(ScenePath)) return;
        foreach (string path in new[] {"Assets/Island/Scenes", PrefabDir, RecipeDir})
            Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var landscape = new GameObject("島の風景");
        IslandArt.CreateLandscape(landscape.transform);
        var modelBank = new Dictionary<string, GameObject>();
        foreach (string kind in new[] {"tree", "rock", "stump", "pebbles", "cabin", "workbench",
            "farm", "sprout", "ripe", "bench", "lantern", "planter", "player"})
        {
            string path = $"{PrefabDir}/{kind}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab)
            {
                var model = IslandArt.CreateModel(kind, null);
                model.name = kind;
                prefab = PrefabUtility.SaveAsPrefabAsset(model, path);
                UnityEngine.Object.DestroyImmediate(model);
            }
            modelBank.Add(kind, prefab);
        }

        void Recipe(string file, string id, string label, int wood, int stone)
        {
            string path = $"{RecipeDir}/{file}.asset";
            if (File.Exists(path)) return;
            var recipe = ScriptableObject.CreateInstance<FurnitureRecipe>();
            recipe.id = id; recipe.displayName = label;
            recipe.woodCost = wood; recipe.stoneCost = stone;
            recipe.prefab = modelBank[id];
            AssetDatabase.CreateAsset(recipe, path);
        }
        Recipe("01_Bench", "bench", "海辺のベンチ", 3, 1);
        Recipe("02_Lantern", "lantern", "浜のランタン", 1, 3);
        Recipe("03_Planter", "planter", "花のプランター", 2, 2);

        GameObject Model(string kind, Transform root)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelBank[kind], root);
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return model;
        }
        Transform Group(string name) => new GameObject(name).transform;
        var buildings = Group("家・作業台・畑");
        var resources = Group("採集ポイント");
        var slots = Group("家具を置ける区画");
        IslandSpot Spot(Transform parent, IslandSpot.Kind kind, string id, string label, float x,
            float z, float block, float reach, string model = null)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(x, 0, z);
            var spot = obj.AddComponent<IslandSpot>();
            spot.kind = kind; spot.id = id; spot.blockingRadius = block; spot.reach = reach;
            if (model != null) spot.model = Model(model, obj.transform);
            return spot;
        }
        Spot(buildings, IslandSpot.Kind.Cabin, "cabin", "海辺の小屋", -4, 4.3f, 1.72f, 2.4f, "cabin");
        var workbench = Spot(buildings, IslandSpot.Kind.Workbench, "workbench", "作業台", -4.25f, 1f, 0.77f, 2.15f, "workbench");
        workbench.transform.rotation = Quaternion.Euler(0, -10, 0);
        var farm = Spot(buildings, IslandSpot.Kind.Farm, "farm", "かぶ畑", 3.25f, 3.2f, 1.08f, 2.25f, "farm");
        farm.alternate = Model("sprout", farm.transform); farm.ripe = Model("ripe", farm.transform);
        farm.alternate.SetActive(false); farm.ripe.SetActive(false);
        int treeNumber = 0, rockNumber = 0;
        void Node(bool wood, float x, float z, float angle, float scale = 1)
        {
            int n = wood ? ++treeNumber : ++rockNumber;
            string id = (wood ? "tree-" : "rock-") + n.ToString("00");
            var node = Spot(resources, wood ? IslandSpot.Kind.Tree : IslandSpot.Kind.Stone, id,
                (wood ? "広葉樹 " : "岩場 ") + n, x, z, wood ? .55f : .71f, 2.15f, wood ? "tree" : "rock");
            node.alternate = Model(wood ? "stump" : "pebbles", node.transform);
            node.alternate.SetActive(false);
            node.transform.rotation = Quaternion.Euler(0, angle, 0);
            node.model.transform.localScale = node.alternate.transform.localScale = Vector3.one * scale;
            node.resourceYield = 3; node.regrowSeconds = wood ? 22 : 18;
        }
        Node(true, -7.1f, .1f, 32); Node(true, -8.7f, 3.9f, 96, 1.16f);
        Node(true, -.6f, 6.65f, -30, 1.04f); Node(true, 6.45f, 5.2f, 165, 1.16f);
        Node(true, 8.65f, 1.9f, 85, .85f); Node(true, -6.8f, 6.3f, 150, .77f);
        Node(false, 4.9f, .15f, 16); Node(false, -5.9f, -3.55f, -65, .85f);
        Node(false, 8.1f, 3.5f, 55); Node(false, 1.65f, 6.45f, 120, .78f);
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 4; col++)
                Spot(slots, IslandSpot.Kind.FurnitureSlot, $"plot-{row+1}-{col+1}",
                    $"広場 {row+1}-{col+1}", -3 + col * 3, -2.4f - row * 2.8f, 0, 2.05f);

        var player = new GameObject("開拓者");
        player.transform.position = new Vector3(0, 0, -.4f);
        var visual = Model("player", player.transform);
        var camObject = new GameObject("島のカメラ", typeof(Camera), typeof(AudioListener));
        camObject.tag = "MainCamera";
        var camera = camObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9.1f;
        camera.transform.position = new Vector3(4, 20, -23);
        camera.transform.LookAt(new Vector3(0, .35f, 0));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(70, 174, 178, 255);
        camera.farClipPlane = 130;
        camera.allowHDR = false;

        var game = new GameObject("凪島だより").AddComponent<IslandGame>();
        game.player = player.transform; game.playerVisual = visual.transform;
        game.islandCamera = camera;
        const string guidePath = "Assets/Island/Art/InteractionGuide.mat";
        game.markerMaterial = AssetDatabase.LoadAssetAtPath<Material>(guidePath);
        if (!game.markerMaterial)
        {
            game.markerMaterial = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(game.markerMaterial, guidePath);
        }
        game.gameObject.AddComponent<IslandHud>().game = game;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath, true)};
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Island/Build Windows Player")]
    public static void BuildWindows()
    {
        Prepare();
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        IslandRulesCheck.Run();
        PlayerSettings.companyName = "NagiIsland";
        PlayerSettings.productName = "Nagi Island";
        PlayerSettings.bundleVersion = "2.0";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.forceSingleInstance = true;
        AssetDatabase.SaveAssets();
        string output = "Builds/Windows/NagiIsland.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] {ScenePath}, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
        Debug.Log("NAGI_WINDOWS_BUILD_OK " + Path.GetFullPath(output));
    }
}
