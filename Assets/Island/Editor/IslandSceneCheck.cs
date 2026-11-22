using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs against the authored scene in a real Play Mode session. World edits last only for that session.
[InitializeOnLoad]
public static class IslandSceneCheck
{
    const string ScenePath = "Assets/Island/Scenes/NagiIsland.unity";
    const string Prefix = "Island.SceneCheck.";
    static string SavePath => SessionState.GetString(Prefix + "path", "");
    static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    static IslandGame game;
    static IslandSpot[] spots;
    static AsyncOperation loading;
    static Func<Vector2, bool> canWalk;
    static int phase, expectedWood, expectedStone;
    static string treeId;
    static Vector3 savedPosition;
    static float savedYaw;
    static readonly Dictionary<string, string> placedIds = new Dictionary<string, string>();
    static string playException;
    static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    static IslandSceneCheck()
    {
        // SessionState survives the domain reload on entering Play Mode.
        if (SavePath.Length > 0) IslandGame.EditorTestSavePath = SavePath;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnModeChanged;
    }

    [MenuItem("Island/Tools/Check Playable Scene")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SavePath.Length != 0)
            throw new InvalidOperationException("Run the scene check from Edit Mode.");
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Check(scene != null, "island scene exists");
        SessionState.SetString(Prefix + "previousScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        string path = Path.Combine(Path.GetTempPath(), "IslandSceneCheck-" + Guid.NewGuid().ToString("N") + ".json");
        SessionState.SetString(Prefix + "path", path);
        SessionState.SetString(Prefix + "deadline", (Now + 120).ToString());
        SessionState.SetInt(Prefix + "result", -1);
        IslandGame.EditorTestSavePath = path;
        EditorSceneManager.playModeStartScene = scene;
        phase = 0;
        placedIds.Clear();
        playException = null;
        EditorApplication.EnterPlaymode();
    }

    static void OnModeChanged(PlayModeStateChange mode)
    {
        if (SavePath.Length == 0) return;
        if (mode == PlayModeStateChange.EnteredPlayMode)
            Application.logMessageReceived += CatchException;
        if (mode != PlayModeStateChange.EnteredEditMode) return;
        Application.logMessageReceived -= CatchException;
        int result = SessionState.GetInt(Prefix + "result", -1);
        string saved = SavePath;
        string previous = SessionState.GetString(Prefix + "previousScene", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        SessionState.EraseString(Prefix + "path");
        SessionState.EraseString(Prefix + "deadline");
        SessionState.EraseString(Prefix + "previousScene");
        SessionState.EraseInt(Prefix + "result");
        IslandGame.EditorTestSavePath = null;
        if (File.Exists(saved)) File.Delete(saved);
        if (File.Exists(saved + ".bak")) File.Delete(saved + ".bak");
        if (result == 0) Debug.Log("IslandSceneCheck passed. Island actions, walkable routes and two scene reloads verified.");
        if (Application.isBatchMode) EditorApplication.Exit(result == 0 ? 0 : 1);
    }

    static void CatchException(string message, string trace, LogType type)
    {
        // Keep unrelated Editor services (for example, the Search indexer) out of runtime results.
        if ((type == LogType.Exception || type == LogType.Assert) && trace.Contains("Assets/Island/"))
            playException = message + "\n" + trace;
    }

    static void Tick()
    {
        if (SavePath.Length == 0 || SessionState.GetInt(Prefix + "result", -1) != -1) return;
        try
        {
            if (Now > long.Parse(SessionState.GetString(Prefix + "deadline", "0")))
                throw new TimeoutException($"Island check stalled at stage {phase} (playing={Application.isPlaying}, paused={EditorApplication.isPaused}, loaded={loading?.isDone}, instance={game}, initialized={game && game.State != null})");
            Check(playException == null, playException);
            if (EditorApplication.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
            if (!EditorApplication.isPlaying || !Application.isPlaying || (loading != null && !loading.isDone)) return;
            if (phase == 0 || phase == 2 || phase == 4)
            {
                game = UnityEngine.Object.FindFirstObjectByType<IslandGame>();
                if (!game || game.State == null) return;
                Check((string)PrivateField("savePath") == SavePath, "isolated save path");
                spots = UnityEngine.Object.FindObjectsByType<IslandSpot>(FindObjectsSortMode.None);
                var method = typeof(IslandGame).GetMethod("CanWalk", BindingFlags.Instance | BindingFlags.NonPublic);
                canWalk = (Func<Vector2, bool>)Delegate.CreateDelegate(typeof(Func<Vector2, bool>), game, method);
            }
            switch (phase)
            {
                case 0: StartIsland(); phase = 1; Debug.Log("IslandSceneCheck: starter loop completed"); break;
                case 1:
                    if (game.State.ResourceWait(treeId, Now) != 0) break;
                    Gather(Find(treeId));
                    expectedWood = game.State.wood;
                    SaveAndReload();
                    phase = 2;
                    Debug.Log("IslandSceneCheck: reloading the planted island");
                    break;
                case 2:
                    VerifyWorld();
                    Check(!game.State.GoalReached && game.State.FarmWait(Now) >= 0, "planted crop restored");
                    Approach(One(IslandSpot.Kind.Farm));
                    phase = 3;
                    Debug.Log("IslandSceneCheck: crop and furniture restored");
                    break;
                case 3:
                    if (game.State.FarmWait(Now) != 0) break;
                    var farm = One(IslandSpot.Kind.Farm);
                    // Batch mode has no focused Game View. Advance the normal frame entry point if needed.
                    if (!farm.ripe.activeSelf) game.SendMessage("Update", SendMessageOptions.RequireReceiver);
                    Check(farm.ripe.activeSelf && !farm.alternate.activeSelf, "ripe crop visible");
                    game.Interact();
                    Check(game.State.harvests == 1 && game.State.GoalReached && game.Panel == "goal", "harvest completes goal");
                    game.ClosePanel();
                    game.Interact();
                    Check(game.State.FarmWait(Now) > 0, "replant after harvest");
                    var removed = Find(placedIds.Keys.First());
                    var recipe = game.Recipes.First(r => r.id == placedIds[removed.id]);
                    Approach(removed);
                    game.RetrieveFocusedFurniture();
                    Check(game.State.At(removed.id) == null && game.State.Count(recipe.id) == 1, "retrieve furniture");
                    Check(game.State.GoalReached, "attained goal survives rearrangement");
                    Place(removed, recipe);
                    SaveAndReload();
                    phase = 4;
                    break;
                case 4:
                    VerifyWorld();
                    Check(game.State.GoalReached && game.State.harvests == 1 && game.Panel == "", "completed island resumed");
                    Finish(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("IslandSceneCheck failed: " + ex);
            Finish(1);
        }
    }

    static void StartIsland()
    {
        Check(game.player && game.playerVisual && game.islandCamera && game.markerMaterial, "scene references");
        var hud = UnityEngine.Object.FindFirstObjectByType<IslandHud>();
        Check(hud && hud.game == game, "HUD references live game");
        Check(spots.All(s => !string.IsNullOrWhiteSpace(s.id)) && spots.Select(s => s.id).Distinct().Count() == spots.Length, "unique spot IDs");
        var recipes = game.Recipes.Take(3).ToArray();
        var slots = spots.Where(s => s.kind == IslandSpot.Kind.FurnitureSlot).OrderBy(s => s.id).Take(3).ToArray();
        Check(recipes.Length == 3 && slots.Length == 3 && recipes.All(r => r.prefab), "catalog and build plots");
        var workshop = One(IslandSpot.Kind.Workbench);
        Approach(workshop);
        game.Interact();
        Check(game.Panel == "craft", "open the workbench");
        game.Craft(recipes[0]);
        Check(game.State.Count(recipes[0].id) == 0, "reject craft with empty inventory");
        game.ClosePanel();
        Approach(One(IslandSpot.Kind.Cabin));
        game.Interact();
        Check(game.Panel == "help", "cabin instructions");
        game.ClosePanel();

        var tree = One(IslandSpot.Kind.Tree);
        treeId = tree.id;
        tree.regrowSeconds = 5;
        foreach (var node in spots.Where(s => s.kind == IslandSpot.Kind.Tree || s.kind == IslandSpot.Kind.Stone)) Gather(node);
        var farm = One(IslandSpot.Kind.Farm);
        farm.cropGrowSeconds = 10;
        Approach(farm);
        game.Interact();
        Check(game.State.FarmWait(Now) > 0 && farm.alternate.activeSelf && !farm.ripe.activeSelf, "seed and seedling");
        Approach(workshop);
        game.Interact();
        foreach (var recipe in recipes)
        {
            int wood = game.State.wood, stone = game.State.stone;
            game.Craft(recipe);
            Check(game.State.Count(recipe.id) == 1 && game.State.wood == wood - recipe.woodCost &&
                game.State.stone == stone - recipe.stoneCost, "craft costs for " + recipe.id);
        }
        game.ClosePanel();
        expectedStone = game.State.stone;
        game.Open("bag");
        game.SelectFurniture(recipes[0]);
        Check(game.IsPlacing && game.Panel == "", "bag starts preview");
        Approach(slots[0], preview: true, standNear: true);
        game.Interact();
        Check(!game.PlacementValid && game.State.At(slots[0].id) == null && game.IsPlacing, "cannot build over player");
        game.CancelPlacement();
        Check(!game.IsPlacing && game.State.Count(recipes[0].id) == 1, "cancel retains furniture");
        for (int i = 0; i < 3; i++)
        {
            if (i == 1)
            {
                game.SelectFurniture(recipes[i]);
                Approach(slots[0], preview: true);
                game.Interact();
                Check(!game.PlacementValid && game.IsPlacing && game.State.Count(recipes[i].id) == 1, "occupied plot");
                game.CancelPlacement();
            }
            Place(slots[i], recipes[i]);
            placedIds[slots[i].id] = recipes[i].id;
        }
        Check(game.PlacedKinds == 3 && game.State.stone == expectedStone, "three pieces placed");
    }

    static void Gather(IslandSpot node)
    {
        Approach(node);
        bool tree = node.kind == IslandSpot.Kind.Tree;
        int stock = tree ? game.State.wood : game.State.stone;
        game.Interact();
        game.Interact();
        Check((tree ? game.State.wood : game.State.stone) == stock + node.resourceYield, "gather once at " + node.id);
        Check(game.State.ResourceWait(node.id, Now) > 0 && !node.model.activeSelf, "depleted " + node.id);
    }

    static void Place(IslandSpot slot, FurnitureRecipe recipe)
    {
        int wood = game.State.wood, stone = game.State.stone;
        game.SelectFurniture(recipe);
        Approach(slot, preview: true);
        game.TurnPlacement();
        Check(game.PlacementValid, "valid preview at " + slot.id);
        game.Interact();
        var item = game.State.At(slot.id);
        Check(!game.IsPlacing && item != null && item.recipeId == recipe.id && item.turns == 1, "confirm rotated " + recipe.id);
        Check(game.State.Count(recipe.id) == 0 && game.State.wood == wood && game.State.stone == stone, "no second material charge");
    }

    static void SaveAndReload()
    {
        savedPosition = game.player.position;
        savedYaw = game.player.eulerAngles.y;
        game.SaveNow();
        Check(File.Exists(SavePath), "temporary save written");
        loading = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
    }

    static void VerifyWorld()
    {
        Check(Vector3.Distance(savedPosition, game.player.position) < .01f &&
            Mathf.Abs(Mathf.DeltaAngle(savedYaw, game.player.eulerAngles.y)) < .01f, "position and direction restored");
        Check(game.State.wood == expectedWood && game.State.stone == expectedStone, "materials restored");
        Check(game.PlacedKinds == 3 && game.State.placed.Count == 3, "placement count restored");
        foreach (var entry in placedIds)
        {
            var site = Find(entry.Key);
            var item = game.State.At(site.id);
            Check(item != null && item.recipeId == entry.Value && item.turns == 1, "restored plot " + site.id);
            string name = game.Recipes.First(r => r.id == item.recipeId).prefab.name + "(Clone)";
            var instances = site.GetComponentsInChildren<Transform>().Where(t => t.parent == site.transform && t.name == name).ToArray();
            Check(instances.Length == 1 && Vector3.Distance(instances[0].position, site.transform.position) < .01f &&
                Quaternion.Angle(instances[0].localRotation, Quaternion.Euler(0, 90, 0)) < .01f, "model restored once at " + site.id);
        }
    }

    // Find a reachable interaction position on the real walkable island, including placed furniture.
    // The test moves the character to the endpoint after confirming a connected path.
    static void Approach(IslandSpot site, bool preview = false, bool standNear = false)
    {
        game.ClosePanel();
        Vector3 start3 = game.player.position;
        Vector2 start = new Vector2(start3.x, start3.z);
        var frontier = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int> { Vector2Int.zero };
        frontier.Enqueue(Vector2Int.zero);
        while (frontier.Count > 0)
        {
            var index = frontier.Dequeue();
            var point = start + (Vector2)index * .36f;
            Vector2 dir = site.Center - point;
            float range = dir.magnitude;
            float minRange = preview ? (standNear ? .3f : 1.24f) : .05f;
            float maxRange = preview ? (standNear ? .9f : 1.8f) : site.reach;
            if (range >= minRange && range <= maxRange)
            {
                game.player.SetPositionAndRotation(new Vector3(point.x, 0, point.y),
                    Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y)));
                game.ClosePanel();
                if ((IslandSpot)PrivateField(preview ? "destination" : "focus") == site) return;
            }
            foreach (var step in Directions)
            {
                var next = index + step;
                if (seen.Contains(next)) continue;
                var offset = (Vector2)step * .36f;
                if (!canWalk(point + offset) || !canWalk(point + offset * .5f)) continue;
                seen.Add(next);
                frontier.Enqueue(next);
            }
        }
        throw new InvalidOperationException("No reachable interaction point for " + site.id);
    }

    static IslandSpot Find(string id) => spots.First(s => s.id == id);
    static IslandSpot One(IslandSpot.Kind kind) => spots.First(s => s.kind == kind);
    static object PrivateField(string name) =>
        typeof(IslandGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);

    static void Finish(int result)
    {
        SessionState.SetInt(Prefix + "result", result);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
        else OnModeChanged(PlayModeStateChange.EnteredEditMode);
    }

    static void Check(bool ok, string description)
    {
        if (!ok) throw new InvalidOperationException("Island scene check: " + description);
    }
}
