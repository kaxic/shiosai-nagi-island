using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IslandExhibitionCheck
{
    [MenuItem("Island/Tools/Check Garden Exhibition")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Island/Scenes/NagiIsland.unity");
        try
        {
            var spots = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<IslandSpot>(true)).ToArray();
            RunWithPlots(spots);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    internal static void RunWithPlots(IslandSpot[] sceneSpots)
    {
        var plots = sceneSpots.Where(s => s.kind == IslandSpot.Kind.FurnitureSlot)
            .OrderByDescending(s => s.Center.y).ThenBy(s => s.Center.x).ToArray();
        Check(plots.Length >= 8, "starter island has eight plots");
        var state = new IslandState();
        var supply = ScriptableObject.CreateInstance<FurnitureRecipe>();
        string save = Path.Combine(Path.GetTempPath(), "NagiExhibit-" + Guid.NewGuid().ToString("N") + ".json");
        List<ExhibitPlot> Board() => IslandExhibitions.ReadPlan(state, sceneSpots);

        void Place(int cell, string id, int target = -1, bool reversed = false)
        {
            supply.id = id;
            if (state.Count(id) == 0) Check(state.TryCraft(supply), "make test furniture");
            int turns = 0;
            if (target >= 0)
            {
                Vector2 direction = (plots[target].Center - plots[cell].Center).normalized;
                float best = -2;
                for (int step = 0; step < 4; step++)
                {
                    var facing = plots[cell].transform.rotation * Quaternion.Euler(0, 90 * step, 0) * Vector3.back;
                    float score = Vector2.Dot(direction, new Vector2(facing.x, facing.z));
                    if (score > best) { best = score; turns = step; }
                }
            }
            Check(state.TryPlace(plots[cell].id, id, turns + (reversed ? 2 : 0)), "place " + id);
        }
        void Clear()
        {
            foreach (var item in state.placed.ToArray()) Check(state.TryRemove(item.slotId), "retrieve");
        }
        void Verify(int show)
        {
            var cells = Board();
            Check(cells.Count == plots.Length, "only furniture plots enter the plan");
            var checkedLines = IslandExhibitions.Evaluate(show, cells);
            Check(checkedLines.All(ok => ok), "solution for " + IslandExhibitions.Get(show).name);
            Check(state.TryExhibit(show, cells) && !state.TryExhibit(show, cells), "stamp only once");
            Check(state.placed.Count > 0 && state.HasExhibitionStamp(show), "no furniture consumed");
            Clear();
            Check(state.HasExhibitionStamp(show), "stamp survives redecorating");
        }
        try
        {
            // Check solutions in arbitrary entry order, including a late design first.
            Place(1, "table"); Place(5, "daybed"); Place(2, "bench", 3); Place(3, "bookshelf"); Place(7, "birdhouse");
            Verify(7);
            Place(0, "bench"); Place(5, "lantern");
            Check(!state.TryExhibit(0, Board()), "diagonal is not adjacent");
            Clear(); Place(0, "bench"); Place(1, "lantern"); Verify(0);
            Place(1, "bench"); Place(0, "planter");
            Check(!state.TryExhibit(1, Board()), "one planter cannot count twice");
            Place(2, "planter"); Verify(1);
            Place(1, "table"); Place(0, "bench", 1, reversed: true); Place(2, "bench", 1);
            Check(!state.TryExhibit(2, Board()), "both benches must face inward");
            Check(state.TryRemove(plots[0].id), "lift bench");
            Place(0, "bench", 1); Verify(2);
            Place(0, "bench", 2); Place(2, "bookshelf"); Place(4, "lantern"); Place(1, "arch");
            Check(!state.TryExhibit(3, Board()), "a filled middle plot is not a gap");
            Check(state.TryRemove(plots[1].id), "clear middle");
            var noMiddle = Board(); noMiddle.RemoveAll(p => p.position == plots[1].Center);
            Check(!state.TryExhibit(3, noMiddle), "a missing plot is not an empty buildable plot");
            Verify(3);
            Place(0, "planter"); Place(1, "birdhouse"); Place(4, "lantern"); Verify(4);
            Place(0, "brazier"); Place(1, "bench", 0); Place(4, "table"); Place(5, "lantern"); Verify(5);
            Place(1, "arch", 5); Place(0, "planter"); Place(2, "planter"); Place(5, "lantern"); Verify(6);
            Check(state.ExhibitionCount == IslandExhibitions.Count && state.CompletedChapters == 0, "independent optional collection");
            Check(IslandSave.Write(save, state) && IslandSave.Load(save).ExhibitionCount == 8, "restore all stamps");
            File.WriteAllText(save, "{\"version\":2,\"state\":{\"wood\":9,\"placed\":[],\"inventory\":[],\"gathered\":[]}}");
            var old = IslandSave.Load(save);
            Check(old.wood == 9 && old.ExhibitionCount == 0, "older save starts with empty stamp album");
            Debug.Log("IslandExhibitionCheck passed. All eight garden layouts, rotations, gaps, duplicate matches and stamp persistence verified.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(supply);
            if (File.Exists(save)) File.Delete(save);
            if (File.Exists(save + ".bak")) File.Delete(save + ".bak");
        }
    }

    static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("Island exhibition check failed: " + label);
    }
}
