using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class IslandCampaignCheck
{
    [MenuItem("Island/Tools/Check Island Campaign")]
    public static void Run()
    {
        var recipes = Resources.LoadAll<FurnitureRecipe>("Island/Furniture").ToDictionary(r => r.id);
        RunWithRecipes(recipes);
    }

    internal static void RunWithRecipes(Dictionary<string, FurnitureRecipe> recipes)
    {
        string file = Path.Combine(Path.GetTempPath(), "NagiCampaign-" + Guid.NewGuid().ToString("N") + ".json");
        var state = new IslandState();
        int visits = 0;
        long clock = 1000;

        void Gather(bool wood)
        {
            // Six trees and four outcrops, with the same yield and regrowth times as the starter island.
            int sources = wood ? 6 : 4;
            int cooldown = wood ? 22 : 18;
            for (;; clock++)
            {
                for (int i = 0; i < sources; i++)
                    if (state.TryGather((wood ? "tree-" : "rock-") + i, wood, 3, cooldown, clock))
                    {
                        visits++;
                        clock++;
                        return;
                    }
            }
        }
        void Supply(int wood, int stone)
        {
            while (state.wood < wood) Gather(true);
            while (state.stone < stone) Gather(false);
        }
        void Craft(string id)
        {
            var recipe = recipes[id];
            Check(state.CanMakeRecipe(recipe), "recipe unlocked in time: " + id);
            Supply(recipe.woodCost, recipe.stoneCost);
            Check(state.TryCraft(recipe), "craft " + id);
        }
        void Produce()
        {
            int before = state.Turnips;
            Check(state.TryPlant(35, clock), "plant");
            clock += state.GrowDuration(35);
            Check(state.TryHarvest(clock) && state.Turnips == before + state.HarvestYield, "harvest awards produce");
        }
        void Report()
        {
            int completed = state.CompletedChapters + state.RepeatOrders;
            int pay = state.OrderReward, purse = state.Shells;
            Check(state.OrderRequirements().All(r => r.have >= r.need), "requirements for " + state.CurrentOrder.title);
            Check(state.CanDeliverOrder && state.TryDeliverOrder(), "report " + completed);
            Check(state.CompletedChapters + state.RepeatOrders == completed + 1 && state.Shells == purse + pay,
                "one reward and one step");
        }
        try
        {
            Check(!state.CanUpgrade(IslandTool.Gathering) && !state.TryTrade(IslandGood.Wood), "empty wallet and inventory");
            Check(!state.TryDeliverOrder() && state.CompletedChapters == 0, "unmet order does not advance");
            Supply(3, 2);
            Check(!state.TryCraft(recipes["table"]) && !state.CanMakeRecipe(recipes["table"]), "recipe locked before chapter");
            int firstWood = state.wood, firstStone = state.stone;
            Report();
            Check(state.wood == firstWood - 3 && state.stone == firstStone - 2, "materials handed over");
            Check(state.TryUpgrade(IslandTool.Gathering) && state.Shells == 0 && state.GatherYield(3) == 4, "first gathering upgrade");

            Craft("bench");
            Report();
            Check(state.Count("bench") == 0 && state.CanMakeRecipe(recipes["table"]), "delivery and new design");
            Check(state.TryUpgrade(IslandTool.Gardening) && state.HarvestYield == 3 && state.GrowDuration(35) == 28, "first garden upgrade");
            Craft("table");
            Craft("bench");
            Check(state.TryPlace("A", "bench", 0) && state.TryPlace("B", "table", 0), "decorate before delivery");
            Produce();
            Check(!state.TryDeliverOrder() && state.Turnips == 3, "placed piece not deliverable; produce untouched");
            Check(state.TryRemove("B"), "retrieve the table");
            Report();
            Check(state.Turnips == 1 && state.PeakPlacedKinds == 2, "deduct produce and remember layout");

            Craft("bookshelf"); Report();
            Craft("birdhouse"); Produce(); Report();
            Craft("brazier");
            Craft("lantern"); Check(state.TryPlace("B", "lantern", 1), "place lamp");
            Craft("planter"); Check(state.TryPlace("C", "planter", 2), "place planter");
            Supply(0, 3); Report();
            Craft("daybed");
            Check(!state.TryDeliverOrder(), "arch must be installed");
            Craft("arch"); Check(state.TryPlace("D", "arch", 3), "decorate harbor entrance");
            Report();
            Check(state.At("D").recipeId == "arch" && state.PeakPlacedKinds == 4, "showpiece remains on island");
            Check(!state.TryDeliverOrder() && state.harvests == 2, "final needs three harvests");
            Produce(); Report();
            Check(state.GoalReached && state.CampaignComplete && state.Rank == 5 && state.RepeatOrders == 0, "opening festival");
            int openingVisits = visits;
            Debug.Log($"Island campaign balance: {openingVisits} gathering visits, {state.harvests} harvests and " +
                $"{state.DiscoveredKinds} crafted designs to the festival using the installed recipes.");

            // All three sales and both tiers of the merchant upgrade work after finishing the story.
            int ordinaryReward = state.OrderReward;
            Check(state.TryUpgrade(IslandTool.Trading) && state.TryUpgrade(IslandTool.Trading) &&
                !state.TryUpgrade(IslandTool.Trading) && state.UpgradeCost(IslandTool.Trading) == 0, "merchant tier limit");
            Check(state.OrderReward == ordinaryReward + 4, "order bonus");
            Supply(3, 3);
            foreach (IslandGood good in Enum.GetValues(typeof(IslandGood)))
            {
                int shells = state.Shells, quantity = IslandEconomy.TradeQuantity(good);
                int Stock() => good == IslandGood.Wood ? state.wood : good == IslandGood.Stone ? state.stone : state.Turnips;
                int before = Stock(), reward = state.TradeReward(good);
                Check(state.TryTrade(good) && state.Shells == shells + reward && Stock() == before - quantity, "sell " + good);
            }
            Check(!state.TryTrade((IslandGood)99) && !state.TryUpgrade((IslandTool)99), "reject unknown market options");
            Craft("lantern"); Supply(2, 0); Report();
            Produce(); Craft("planter"); Report();
            Craft("bookshelf"); Craft("table"); Report();
            Craft("daybed"); Supply(0, 2); Report();
            Craft("birdhouse"); Supply(3, 0); Report();
            Craft("brazier"); Supply(0, 3); Report();
            Check(state.RepeatOrders == 6 && state.CurrentOrder.title == "夜市の明かり", "all return trips cycle");
            Check(state.TryUpgrade(IslandTool.Gathering) && state.TryUpgrade(IslandTool.Gardening), "max out productive tools");
            Check(state.GatherYield(3) == 5 && state.GrowDuration(35) == 21 && state.HarvestYield == 4, "max tool benefits");
            Check(IslandSave.Write(file, state), "write campaign progress");
            var loaded = IslandSave.Load(file);
            Check(loaded.CampaignComplete && loaded.GoalReached && loaded.RepeatOrders == 6 && loaded.Rank == 5 &&
                loaded.Shells == state.Shells && loaded.Turnips == state.Turnips && loaded.PeakPlacedKinds == 4 &&
                loaded.DiscoveredKinds == state.DiscoveredKinds && loaded.ToolLevel(IslandTool.Trading) == 2, "restore campaign");

            // Literal format from the original release, with no fields from the new campaign.
            File.WriteAllText(file, "{\"version\":1,\"state\":{\"wood\":11,\"stone\":3,\"harvests\":4," +
                "\"playerX\":1.25,\"playerZ\":-3,\"playerYaw\":80,\"placed\":[{\"slotId\":\"old-plot\",\"recipeId\":\"bench\",\"turns\":2}]," +
                "\"inventory\":[{\"recipeId\":\"bench\",\"count\":2}],\"gathered\":[{\"nodeId\":\"oak\",\"readyAt\":2000}]," +
                "\"cropReadyAt\":1050,\"goalReached\":true}}");
            var original = IslandSave.Load(file);
            Check(original.GoalReached && original.PeakPlacedKinds == 3 && original.Count("bench") == 2 && original.At("old-plot").turns == 2 &&
                original.wood == 11 && original.harvests == 4 && original.Turnips == 8 && original.CompletedChapters == 0 &&
                original.FarmWait(1000) == 50 && original.ResourceWait("oak", 1000) == 1000, "import original island");
            Check(original.TryTrade(IslandGood.Turnip) && IslandSave.Write(file, original), "first save after migration");
            Check(IslandSave.Load(file).Turnips == 6 && IslandSave.Load(file).Shells == 3, "do not credit old harvests twice");
            Debug.Log("IslandCampaignCheck passed. Eight chapters, six recurring orders, trade, upgrades and original save migration verified.");
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
            if (File.Exists(file + ".bak")) File.Delete(file + ".bak");
        }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Island campaign check failed: " + message);
    }
}
