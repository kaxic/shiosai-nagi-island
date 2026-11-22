using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class IslandRulesCheck
{
    [MenuItem("Island/Tools/Check Gameplay Rules")]
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IslandRules-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "island.json");
        var bench = MakeRecipe("bench", 2, 1);
        var lamp = MakeRecipe("lamp", 1, 1);
        var planter = MakeRecipe("planter", 1, 1);
        try
        {
            var state = IslandSave.Load(path);
            Check(state.playerZ == -0.4f && state.FarmWait(100) == -1, "fresh island");
            Check(!state.TryCraft(bench), "insufficient materials");
            Check(state.TryGather("oak", true, 8, 20, 100), "gather timber");
            Check(!state.TryGather("oak", true, 8, 20, 101) && state.wood == 8, "resource cooldown");
            Check(state.ResourceWait("oak", 115) == 5, "remaining cooldown");
            Check(state.TryGather("oak", true, 8, 20, 120), "regrowth at deadline");
            Check(state.TryGather("outcrop", false, 6, 35, 120), "gather stone");

            Check(state.TryCraft(bench) && state.TryCraft(bench), "craft two benches");
            int wood = state.wood, stone = state.stone;
            // Preview, turn and cancel are world/UI operations. No item leaves inventory until TryPlace.
            Check(state.At("p1") == null && state.Count("bench") == 2, "preview/cancel retains stock");
            Check(state.TryPlace("p1", "bench", -1) && state.At("p1").turns == 3, "rotated placement");
            Check(!state.TryPlace("p1", "bench", 0) && state.Count("bench") == 1, "cannot stack");
            Check(state.wood == wood && state.stone == stone, "placement does not charge materials again");
            Check(state.TryPlace("p2", "bench", 0) && !state.TryPlace("p3", "bench", 0), "stock limit");
            Check(state.PlacedTypeCount == 1 && state.TryRemove("p2"), "retrieve duplicate");
            Check(!state.TryRemove("p2") && state.Count("bench") == 1, "retrieval returns one item");

            Check(state.TryPlant(18, 125) && !state.TryPlant(18, 126), "plant once");
            Check(state.FarmWait(130) == 13 && !state.TryHarvest(130), "unripe crop");
            state.playerX = 1.5f;
            state.playerZ = 2;
            state.playerYaw = 90;
            Check(IslandSave.Write(path, state), "initial save");
            state = IslandSave.Load(path);
            Check(state.wood == wood && state.stone == stone && state.Count("bench") == 1, "saved inventory");
            Check(state.playerX == 1.5f && state.playerZ == 2 && state.playerYaw == 90, "saved position");
            Check(state.placed.Count == 1 && state.At("p1").turns == 3, "saved placement");
            Check(state.ResourceWait("outcrop", 130) == 25 && state.FarmWait(130) == 13, "saved timers");
            Check(state.TryHarvest(143) && state.harvests == 1 && !state.TryHarvest(144), "single harvest");
            Check(state.TryPlant(18, 150) && state.FarmWait(155) == 13, "replant");
            Check(state.TryCraft(lamp) && state.TryPlace("p2", "lamp", 1), "second furniture type");
            Check(!state.GoalReached && state.TryCraft(planter) && state.TryPlace("p3", "planter", 0), "third type");
            Check(state.GoalReached && state.TryRemove("p1") && state.GoalReached, "persistent goal");
            Check(IslandSave.Write(path, state), "save milestone");
            state = IslandSave.Load(path);
            Check(state.GoalReached && state.harvests == 1 && state.PlacedTypeCount == 2, "saved goal");
            Check(IslandSave.Write(path, state), "rotate milestone into backup");
            File.WriteAllText(path, "{broken");
            state = IslandSave.Load(path);
            Check(state.GoalReached && state.Count("bench") == 2, "restore backup");
            Check(IslandSave.Write(path, state), "repair damaged primary");
            Check(IslandSave.Load(path + ".bak").GoalReached, "repair preserves usable backup");
            string future = "{\"version\":999}";
            File.WriteAllText(path, future);
            Check(IslandSave.Load(path).IsSaveReadOnly && !IslandSave.Write(path, state), "protect future save");
            Check(File.ReadAllText(path) == future, "future save retained");
            Debug.Log("IslandRulesCheck passed. Gathering, crafting, placement, farm, goals and save recovery verified.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bench);
            UnityEngine.Object.DestroyImmediate(lamp);
            UnityEngine.Object.DestroyImmediate(planter);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static FurnitureRecipe MakeRecipe(string id, int wood, int stone)
    {
        var recipe = ScriptableObject.CreateInstance<FurnitureRecipe>();
        recipe.id = id;
        recipe.woodCost = wood;
        recipe.stoneCost = stone;
        return recipe;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Island rule check failed: " + description);
    }
}
